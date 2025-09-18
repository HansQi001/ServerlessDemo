using AgileObjects.AgileMapper;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.Extensions.Logging;
using ServerlessDemo.FunApp.Infrastructure;
using ServerlessDemo.FunApp.Models.DTOs;
using ServerlessDemo.FunApp.Models.Entities;

namespace ServerlessDemo.FunApp;

public class QueueTiggerProcessor
{
    private readonly ILogger<QueueTiggerProcessor> _logger;
    private readonly AppDbContext _dbContext;

    public QueueTiggerProcessor(ILogger<QueueTiggerProcessor> logger
        , AppDbContext context
        , IMapper mapper)
    {
        _logger = logger;
        _dbContext = context;
    }

    [Function(nameof(QueueTiggerProcessor))]
    public async Task Run(
        [ServiceBusTrigger("hans-queue", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation($"Message {message.MessageId}, {message.ContentType}: {message.Body}");

        var messageContent = message.Body.ToObjectFromJson<ProductIdsRequest>();

        if (messageContent?.Ids?.Length > 0)
        {
            for (var i = 0; i < messageContent.Ids.Length; i++)
            {
                var product = new Product
                {
                    Id = messageContent.Ids[i],
                    Status = "Inactive",
                    LastModifiedAt = DateTime.UtcNow
                };

                var entry = _dbContext.Attach(product);

                entry.Property(p => p.Status).IsModified = true;
                entry.Property(p => p.LastModifiedAt).IsModified = true;
            }
            try
            {
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Products updated");
            }
            catch (Exception ex) {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.InnerException?.Message);
            }
            
        }

        // Complete the message
        await messageActions.CompleteMessageAsync(message);

        _logger.LogInformation($"Message completed");
    }
}