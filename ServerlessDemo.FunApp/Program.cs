using AgileObjects.AgileMapper;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServerlessDemo.FunApp.Models.Entities;
using ServerlessDemo.FunApp.Models.MappingConfigs;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = FunctionsApplication.CreateBuilder(args);

// Register CosmosClient as a singleton
builder.Services.AddSingleton(sp =>
{
    var connectionString = Environment.GetEnvironmentVariable("CosmosDBConnection")
        ?? throw new InvalidOperationException("CosmosDBConnection environment variable is not set.");

    return new CosmosClient(connectionString, new CosmosClientOptions { AllowBulkExecution = true });
});

builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.Converters.Add(new JsonStringEnumConverter());
});

builder.ConfigureFunctionsWebApplication();

IMapper mapper = Mapper.CreateNew();
// Find all IMappingConfig implementations in the current assembly
var mappingConfigs = typeof(Program).Assembly
    .GetTypes()
    .Where(t => typeof(IMappingConfig).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
    .Select(Activator.CreateInstance)
    .Cast<IMappingConfig>();

// Apply each config
foreach (var config in mappingConfigs)
{
    config.Configure(mapper);
}

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    .AddSingleton(mapper); // Add AgileMapper as a singleton

var host = builder.Build();

// Create a token that will be cancelled when the host shuts down
using var cts = CancellationTokenSource.CreateLinkedTokenSource(
    host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping
);

Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

if (builder.Environment.IsDevelopment())
{
    await SeedTestDataAsync(host.Services, cts.Token);
}

await host.RunAsync(cts.Token);

static async Task SeedTestDataAsync(IServiceProvider services, CancellationToken cancellationToken)
{
    var logger = services.GetRequiredService<ILogger<Program>>();

    var client = services.GetRequiredService<CosmosClient>();
    var container = client.GetContainer("ServerlessDemo", "Products");

    // Check if any records exist
    var iterator = container.GetItemQueryIterator<Product>("SELECT TOP 1 * FROM c");

    if (iterator.HasMoreResults)
    {
        var firstPage = await iterator.ReadNextAsync(cancellationToken);
        if (firstPage.Count > 0)
        {
            logger.LogInformation("Seed data already exists — skipping insert.");
            return;
        }
    }

    logger.LogInformation("No data found — inserting seed products...");

    var testProducts = Enumerable.Range(1, 100)
            .Select(i => new Product
            {
                Name = $"Product {i}",
                Price = Math.Round((decimal)(i * 0.5), 2),
                Stock = i % 100
            })
            .ToList();

    var parallelOptions = new ParallelOptions
    {
        MaxDegreeOfParallelism = 10,
        CancellationToken = cancellationToken
    };

    try
    {
        await Parallel.ForEachAsync(testProducts, parallelOptions, async (p, ct) =>
        {
            await container.UpsertItemAsync(p, new PartitionKey(p.Id), cancellationToken: ct);
        });

        logger.LogInformation("Seed data inserted: {Count} items", testProducts.Count);
    }
    catch (Exception ex)
    {
        logger.LogError(ex.ToString());
    }
}

public partial class Program { }
