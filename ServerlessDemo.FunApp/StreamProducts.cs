using AgileObjects.AgileMapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServerlessDemo.FunApp.Infrastructure;
using ServerlessDemo.FunApp.Models.DTOs;
using System.Text.Json;

namespace ServerlessDemo.FunApp;

public class StreamProducts
{
    private readonly ILogger<StreamProducts> _logger;
    private readonly AppDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IHostEnvironment _env;

    public StreamProducts(ILogger<StreamProducts> logger
        , AppDbContext context
        , IHostEnvironment env
        , IMapper mapper)
    {
        _logger = logger;
        _dbContext = context;
        _mapper = mapper;
        _env = env;
    }

    [Function("StreamProducts")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData request)
    {
        if (_env.IsDevelopment())
        {
            _logger.LogInformation("Start streaming the products");
        }

        var response = request.CreateResponse();
        // set the response type to be x-ndjson, the browser won't show the result on page but download it as a file
        response.Headers.Add("Content-Type", "application/x-ndjson");

        var products = _dbContext.Products.AsNoTracking()
            .Select(p => _mapper.Map(p).ToANew<ProductSummaryDTO>())
            .AsAsyncEnumerable();

        await foreach (var product in products)
        {
            var json = JsonSerializer.Serialize(product);
            await response.WriteStringAsync(json + "\n");
            await response.Body.FlushAsync();
        }

        if (_env.IsDevelopment())
        {
            _logger.LogInformation("End streaming the products");
        }

        return response;
    }
}