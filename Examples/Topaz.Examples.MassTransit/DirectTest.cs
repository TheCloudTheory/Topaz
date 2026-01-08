using Azure.Messaging.ServiceBus;
using Topaz.ResourceManager;

namespace Topaz.Examples.MassTransit;

public class DirectTest
{
    public static async Task TestDirectConnection()
    {
        Console.WriteLine("Testing direct ServiceBusClient connection...");
        
        var connectionString = TopazResourceHelpers.GetServiceBusConnectionString("sbnamespace");
        Console.WriteLine($"Connection string: {connectionString}");
        
        var clientOptions = new ServiceBusClientOptions
        {
            TransportType = ServiceBusTransportType.AmqpTcp,
            RetryOptions = new ServiceBusRetryOptions
            {
                Mode = ServiceBusRetryMode.Fixed,
                MaxRetries = 3,
                Delay = TimeSpan.FromSeconds(1)
            }
        };
        
        try
        {
            var client = new ServiceBusClient(connectionString, clientOptions);
            Console.WriteLine("Client created successfully");
            
            var sender = client.CreateSender("sbqueue");
            Console.WriteLine("Sender created successfully");
            
            var message = new ServiceBusMessage("Test message");
            Console.WriteLine("Attempting to send message...");
            
            await sender.SendMessageAsync(message);
            Console.WriteLine("Message sent successfully!");
            
            await sender.DisposeAsync();
            await client.DisposeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
