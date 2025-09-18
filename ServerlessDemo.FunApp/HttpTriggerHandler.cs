using AgileObjects.AgileMapper;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServerlessDemo.FunApp.Models.DTOs;
using ServerlessDemo.FunApp.Models.Entities;
using System.Net;
using System.Text.Json;
using System.Web;

namespace ServerlessDemo.FunApp;

public class HttpTriggerHandler
{
    private readonly ILogger<HttpTriggerHandler> _logger;
    private readonly CosmosClient _cosmosClient;
    private readonly IMapper _mapper;
    private readonly IHostEnvironment _env;

    public HttpTriggerHandler(ILogger<HttpTriggerHandler> logger
        , CosmosClient cosmosClient
        , IHostEnvironment env
        , IMapper mapper)
    {
        _logger = logger;
        _cosmosClient = cosmosClient;
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

        var pid = query["id"];
        var response = request.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/x-ndjson");

        var container = _cosmosClient.GetContainer("ServerlessDemo", "Products");

        await using var writer = new StreamWriter(response.Body);
        using var feedIterator = container.GetItemQueryIterator<Product>(
            $"SELECT * FROM c {(string.IsNullOrEmpty(pid) 
                    ? string.Empty : $" where c.id='{pid}'")}");

        while (feedIterator.HasMoreResults)
        {
            var page = await feedIterator.ReadNextAsync();
            foreach (var product in page)
            {
                var dto = _mapper.Map(product).ToANew<ProductSummaryDTO>();
                // Serialize each product as JSON and write immediately
                var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await writer.WriteLineAsync(json);
                // push chunk to client
                await writer.FlushAsync();
            }
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