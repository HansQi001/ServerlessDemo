using AgileObjects.AgileMapper;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServerlessDemo.FunApp.Models.Entities;
using ServerlessDemo.FunApp.Models.MappingConfigs;

var builder = FunctionsApplication.CreateBuilder(args);

// Register CosmosClient as a singleton
builder.Services.AddSingleton(sp =>
{
    var connectionString = Environment.GetEnvironmentVariable("CosmosDBConnection");
    return new CosmosClient(connectionString, new CosmosClientOptions { AllowBulkExecution = true });
});

await SeedTestDataAsync(builder.Services.BuildServiceProvider());

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

//builder.Services.AddDbContext<AppDbContext>(options =>
//{
//    options.UseInMemoryDatabase("ServerlessDb");
//});


builder.Build().Run();

static async Task SeedTestDataAsync(IServiceProvider services)
{
    var client = services.GetRequiredService<CosmosClient>();
    var container = client.GetContainer("ServerlessDemo", "Products");

    // Check if any records exist
    var iterator = container.GetItemQueryIterator<Product>("SELECT TOP 1 * FROM c");

    if (iterator.HasMoreResults)
    {
        var firstPage = await iterator.ReadNextAsync();
        if (firstPage.Count > 0)
        {
            Console.WriteLine("Seed data already exists — skipping insert.");
            return;
        }
    }

    Console.WriteLine("No data found — inserting seed products...");

    var testProducts = Enumerable.Range(1, 100)
            .Select(i => new Product
            {
                Name = $"Product {i}",
                Price = Math.Round((decimal)(i * 0.5), 2),
                Stock = i % 100
            })
            .ToList();
    
    try
    {
        //await container.UpsertItemAsync(testProducts[0], new PartitionKey(testProducts[0].Id));

        // bulk Upsert
        var tasks = testProducts.Select(async p =>
                   await container.UpsertItemAsync(p, new PartitionKey(p.Id)));

        await Task.WhenAll(tasks);

        Console.WriteLine("Seed data inserted.");
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());
    }
}

public partial class Program { }
