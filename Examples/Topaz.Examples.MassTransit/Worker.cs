using System.Text.Json;
using MassTransit;

namespace Topaz.Examples.MassTransit;

public class Worker(IBus bus) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = new ExampleMessage(DateTimeOffset.Now);
                var endpoint = await bus.GetSendEndpoint(new Uri("queue:sbqueue"));
                await endpoint.Send(message, stoppingToken);
                await Task.Delay(1000, stoppingToken);
                
                Console.WriteLine($"Message dispatched: {JsonSerializer.Serialize(message)}");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                throw;
            }
        }
    }
}

public record ExampleMessage(DateTimeOffset Timestamp)
{
    public string Message => $"The time is {Timestamp}";
};