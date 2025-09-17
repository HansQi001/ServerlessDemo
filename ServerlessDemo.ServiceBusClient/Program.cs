namespace ServerlessDemo.ServiceBusClient
{
    using Azure.Messaging.ServiceBus;
    using Microsoft.Extensions.Configuration;

    internal class Program
    {
        const string queueName = "hans-queue";

        static async Task Main()
        {
            // Load local.settings.json
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("local.settings.json", optional: false, reloadOnChange: true)
                .Build();

            // Get the connection string from the "Values" section
            var connectionString = config["Values:ServiceBusConnection"];

            await using var client = new ServiceBusClient(connectionString);
            var sender = client.CreateSender(queueName);

            for (int i = 1; i <= 5; i++)
            {
                string messageBody = $"Order #{i}";
                var message = new ServiceBusMessage(messageBody);
                await sender.SendMessageAsync(message);
                Console.WriteLine($"Sent: {messageBody}");
            }

            Console.WriteLine("All messages sent.");
            Console.ReadKey();
        }
    }
}
