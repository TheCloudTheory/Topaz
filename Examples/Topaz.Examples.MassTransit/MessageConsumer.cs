using System.Text.Json;
using JetBrains.Annotations;
using MassTransit;

namespace Topaz.Examples.MassTransit;

[UsedImplicitly]
public class MessageConsumer : IConsumer<ExampleMessage>
{
    public Task Consume(ConsumeContext<ExampleMessage> context)
    {
        Console.WriteLine($"Message consumed: {JsonSerializer.Serialize(context.Message)}");
        return Task.CompletedTask;
    }
}