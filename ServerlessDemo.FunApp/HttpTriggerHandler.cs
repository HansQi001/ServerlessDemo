using AgileObjects.AgileMapper;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServerlessDemo.FunApp.Models.DTOs;
using ServerlessDemo.FunApp.Models.Entities;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Web;

namespace ServerlessDemo.FunApp;

public class HttpTriggerHandler
{
    private readonly ILogger<HttpTriggerHandler> _logger;
    private readonly CosmosClient _cosmosClient;
    private readonly IMapper _mapper;
    private readonly IHostEnvironment _env;
    private readonly JsonSerializerOptions _jsonOptions;

    public HttpTriggerHandler(ILogger<HttpTriggerHandler> logger
        , CosmosClient cosmosClient
        , IHostEnvironment env
        , IMapper mapper
        , IOptions<JsonSerializerOptions> jsonOptions)
    {
        _logger = logger;
        _cosmosClient = cosmosClient;
        _mapper = mapper;
        _env = env;
        _jsonOptions = jsonOptions.Value;
    }

    [Function("StreamProducts")]
    public async Task<HttpResponseData> StreamProductsAsync([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        var query = HttpUtility.ParseQueryString(request.Url.Query);

        var pid = query["id"];

        var container = _cosmosClient.GetContainer("ServerlessDemo", "Products");

        if (!string.IsNullOrEmpty(pid))
        {
            try
            {
                if (_env.IsDevelopment())
                    _logger.LogInformation("Fetching single product {Id}", pid);

                var item = await container.ReadItemAsync<Product>(
                    pid,
                    new PartitionKey(pid),
                    cancellationToken: cancellationToken);

                var dto = _mapper.Map(item.Resource).ToANew<ProductSummaryDTO>();

                var response = request.CreateResponse(HttpStatusCode.OK);
                // Return a normal JSON
                await response.WriteAsJsonAsync(dto, cancellationToken);

                return response;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return request.CreateResponse(HttpStatusCode.NotFound);
            }
        }
        else
        {
            if (_env.IsDevelopment())
            {
                _logger.LogInformation("Start streaming the products");
            }

            var response = request.CreateResponse(HttpStatusCode.OK);
            response.Headers.Remove("Content-Type");
            response.Headers.TryAddWithoutValidation("Content-Type", "application/x-ndjson");

            QueryDefinition queryDefinition = new QueryDefinition("SELECT * FROM c");

            await using var writer = new StreamWriter(response.Body, new UTF8Encoding(false));
            using var feedIterator = container.GetItemQueryIterator<Product>(queryDefinition);

            try
            {
                while (feedIterator.HasMoreResults && !cancellationToken.IsCancellationRequested)
                {
                    var page = await feedIterator.ReadNextAsync(cancellationToken);
                    // Log request charge and RU consumption
                    _logger.LogInformation("Query cost: {RU} RUs", page.RequestCharge);

                    foreach (var product in page)
                    {
                        var dto = _mapper.Map(product).ToANew<ProductSummaryDTO>();
                        // Serialize each product as JSON and write immediately
                        await JsonSerializer.SerializeAsync(writer.BaseStream, dto,
                            _jsonOptions,
                            cancellationToken);
                        // Add a new line
                        await writer.WriteLineAsync(ReadOnlyMemory<char>.Empty, cancellationToken);
                        // push chunk to client
                        await writer.FlushAsync(cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during streaming");
                throw;
            }

            if (_env.IsDevelopment())
            {
                _logger.LogInformation("End streaming the products");
            }

            return response;
        }
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