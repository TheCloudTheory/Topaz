using MassTransit;

namespace Topaz.Examples.MassTransit;

public class Worker(IBus bus) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await bus.Publish(new ExampleMessage(DateTimeOffset.Now), stoppingToken);

            await Task.Delay(1000, stoppingToken);
        }
    }
}

public record ExampleMessage(DateTimeOffset Timestamp)
{
    public string Message => $"The time is {Timestamp}";
};