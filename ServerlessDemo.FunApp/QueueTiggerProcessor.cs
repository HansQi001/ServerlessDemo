using AgileObjects.AgileMapper;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ServerlessDemo.FunApp.Models.DTOs;
using ServerlessDemo.FunApp.Models.Entities;
using System.Net;

namespace ServerlessDemo.FunApp;

public class QueueTiggerProcessor
{
    private readonly ILogger<QueueTiggerProcessor> _logger;
    private readonly CosmosClient _cosmosClient;

    public QueueTiggerProcessor(ILogger<QueueTiggerProcessor> logger
        , CosmosClient cosmosClient
        , IMapper mapper)
    {
        _logger = logger;
        _cosmosClient = cosmosClient;
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
            try
            {
                var container = _cosmosClient.GetContainer("ServerlessDemo", "Products");
                /* // version 1
                var queryDef = new QueryDefinition(
                                    "SELECT * FROM c WHERE ARRAY_CONTAINS(@ids, c.id)"
                                ).WithParameter("@ids", messageContent.Ids);

                var feedIterator = container.GetItemQueryIterator<Product>(queryDef);

                while (feedIterator.HasMoreResults)
                {
                    var page = await feedIterator.ReadNextAsync();
                    var tasks = page.Select(async p =>
                    {
                        p.Status = "Inactive";
                        p.LastModifiedAt = DateTime.UtcNow;
                        await container.UpsertItemAsync(p, new PartitionKey(p.Id));
                    }
                    ).ToList();

                    await Task.WhenAll(tasks);
                }
                */
                /* // version 2
                var throttler = new SemaphoreSlim(10);

                var tasks = messageContent.Ids.Select(async id =>
                    {
                        await throttler.WaitAsync();
                        try
                        {
                            var p = await container.ReadItemAsync<Product>(id, new PartitionKey(id));
                            var product = p.Resource;
                            product.Status = "Inactive";
                            product.LastModifiedAt = DateTime.UtcNow;
                            await container.UpsertItemAsync(product, new PartitionKey(product.Id));
                        }
                        finally
                        {
                            throttler.Release();
                        }

                    }
                );

                await Task.WhenAll(tasks);
                */

                // version 3
                var throttler = new SemaphoreSlim(10);

                var tasks = messageContent.Ids.Select(async id =>
                {
                    await throttler.WaitAsync();
                    try
                    {
                        await container.PatchItemAsync<Product>(
                            id,
                            new PartitionKey(id),
                            [
                                PatchOperation.Replace("/status", "Inactive"),
                                PatchOperation.Replace("/lastModifiedAt", DateTime.UtcNow.ToString("o"))
                            ]
                        );

                    }
                    catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                    {
                        _logger.LogWarning("Product {Id} not found", id);
                    }

                    finally
                    {
                        throttler.Release();
                    }

                }
                );

                await Task.WhenAll(tasks);

                _logger.LogInformation($"Products updated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating products");
            }
        }

        // Complete the message
        await messageActions.CompleteMessageAsync(message);

        _logger.LogInformation($"Message completed");
    }
}