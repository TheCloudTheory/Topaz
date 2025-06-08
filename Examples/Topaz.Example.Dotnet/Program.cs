using DotNet.Testcontainers.Builders;
using JetBrains.Annotations;
using Topaz.ResourceManager;

namespace Topaz.Example.Dotnet;

[UsedImplicitly]
internal class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Topaz Example - .NET");
        
        var subscriptionId = Guid.NewGuid();
        var subscriptionName = "topaz.example";

        // Create a builder for Topaz container image and all the ports
        // which are exposed by the emulator. Note you don't need to expose
        // every port available.
        var container = new ContainerBuilder()
            .WithImage("thecloudtheory/topaz-cli:v1.0.61-alpha")
            .WithPortBinding(8890)
            .WithPortBinding(8899)
            .WithPortBinding(8898)
            .WithPortBinding(8897)
            .WithPortBinding(8891)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(8890).ForPort(8899).ForPort(8898).ForPort(8897).ForPort(8891)))
            .Build();
    
        await container.StartAsync()
            .ConfigureAwait(false);

        await CreateSubscription(subscriptionId, subscriptionName);
        CreateResourceGroup();
    }

    private static void CreateResourceGroup()
    {
    }

    private static async Task CreateSubscription(Guid subscriptionId, string subscriptionName)
    {
        using var topaz = new TopazArmClient();
        
        await topaz.CreateSubscriptionAsync(subscriptionId, subscriptionName);
    }
}