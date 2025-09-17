using AgileObjects.AgileMapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServerlessDemo.FunApp.Infrastructure;
using ServerlessDemo.FunApp.Models.Entities;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    .AddSingleton(Mapper.CreateNew()); // Add AgileMapper as a singleton

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseInMemoryDatabase("ServerlessDb");
});

//builder.Services.AddAutoMapper(cfg =>
//{
//    cfg.AddMaps(Assembly.GetExecutingAssembly());
//});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (!db.Products.Any())
    {
        var products = Enumerable.Range(1, 1000)
                .Select(i => new Product
                {
                    Name = $"Product {i}",
                    Price = Math.Round((decimal)(i * 0.5), 2),
                    Stock = i % 100
                })
                .ToArray();

        db.Products.AddRange(products);
    }

    db.SaveChanges();
}
app.Run();

public partial class Program { }
