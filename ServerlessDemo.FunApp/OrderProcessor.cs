using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ServerlessDemo.FunApp;

public class OrderProcessor
{
    private readonly ILogger<OrderProcessor> _logger;

    public OrderProcessor(ILogger<OrderProcessor> logger)
    {
        _logger = logger;
    }

    [Function(nameof(OrderProcessor))]
    public async Task Run(
        [ServiceBusTrigger("hans-queue", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation($"Message ID: {message.MessageId}");
        _logger.LogInformation($"Message Body for Message ID {message.MessageId}: {message.Body}");
        _logger.LogInformation($"Message Content-Type for Message ID {message.MessageId}: {message.ContentType}");

        // Complete the message
        await messageActions.CompleteMessageAsync(message);
    }
}