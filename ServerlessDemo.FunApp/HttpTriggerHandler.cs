using AgileObjects.AgileMapper;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServerlessDemo.FunApp.Infrastructure;
using ServerlessDemo.FunApp.Models.DTOs;
using System.Text.Json;
using System.Web;

namespace ServerlessDemo.FunApp;

public class HttpTriggerHandler
{
    private readonly ILogger<HttpTriggerHandler> _logger;
    private readonly AppDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IHostEnvironment _env;

    public HttpTriggerHandler(ILogger<HttpTriggerHandler> logger
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
    public async Task<HttpResponseData> StreamProductsAsync([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData request)
    {
        if (_env.IsDevelopment())
        {
            _logger.LogInformation("Start streaming the products");
        }

        var query = HttpUtility.ParseQueryString(request.Url.Query);

        int.TryParse(query["productid"] ?? string.Empty, out int productid);

        var response = request.CreateResponse();
        // set the response type to be x-ndjson, the browser won't show the result on page but download it as a file
        response.Headers.Add("Content-Type", "application/x-ndjson");

        var products = _dbContext.Products.AsNoTracking()
            .Where(p => productid == 0 || p.Id == productid)
            .Select(p => _mapper.Map(p).ToANew<ProductSummaryDTO>())
            //.ProjectUsing(_mapper) // EF-friendly projection
            //.To<ProductSummaryDTO>()
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

    [Function("QueueProducts")]
    public async Task QueueProductsAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData request)
    {
        if (_env.IsDevelopment())
        {
            _logger.LogInformation("Start queuing the products");
        }

        using var reader = new StreamReader(request.Body);
        string rawJson = await reader.ReadToEndAsync();

        var queueName = Environment.GetEnvironmentVariable("QueueName");
        var connectionString = Environment.GetEnvironmentVariable("ServiceBusConnection");

        await using var client = new ServiceBusClient(connectionString);
        var sender = client.CreateSender(queueName);

        // Create the message with UTF-8 bytes
        var message = new ServiceBusMessage(System.Text.Encoding.UTF8.GetBytes(rawJson))
        {
            ContentType = "application/json" // set content type
        };

        await sender.SendMessageAsync(message);

        if (_env.IsDevelopment())
        {
            _logger.LogInformation($"Sent: {rawJson}");
            _logger.LogInformation("End queuing the products");
        }
    }
}