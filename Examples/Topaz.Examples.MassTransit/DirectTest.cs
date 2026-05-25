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

            // Receive and complete the message so it does not remain in the queue
            // for MassTransit (which expects a JSON envelope, not plain text).
            var receiver = client.CreateReceiver("sbqueue");
            Console.WriteLine("Attempting to receive message...");

            var received = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(10));
            if (received != null)
            {
                // Port 8889 (UseDevelopmentEmulator=true) uses pre-settled (ReceiveAndDelete)
                // delivery semantics — the message is removed from the queue on delivery.
                // CompleteMessageAsync must NOT be called; the AMQP DISPOSITION frame has no
                // pending delivery to settle and Topaz will not respond, causing a 60 s timeout.
                Console.WriteLine($"Message received: '{received.Body}' (DeliveryCount={received.DeliveryCount})");
            }
            else
            {
                Console.WriteLine("No message received within timeout.");
            }

            await receiver.DisposeAsync();
            await client.DisposeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
