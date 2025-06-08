using DotNet.Testcontainers.Builders;
using JetBrains.Annotations;
using Topaz.ResourceManager;

namespace Topaz.Example.Dotnet;

[UsedImplicitly]
internal class Program
{
    public static async Task Main(string[] args)
    {
        // Create a builder for Topaz container image and all the ports
        // which are exposed by the emulator. Note you don't need to expose
        // every port available.
        var container = new ContainerBuilder()
            .WithImage("thecloudtheory/topaz-cli:v1.0.76-alpha")
            .WithPortBinding(8890)
            .WithPortBinding(8899)
            .WithPortBinding(8898)
            .WithPortBinding(8897)
            .WithPortBinding(8891)
            .Build();
        
        try
        {
            Console.WriteLine("Topaz Example - .NET");
        
            var subscriptionId = Guid.NewGuid();
            var subscriptionName = "topaz.example";
    
            await container.StartAsync()
                .ConfigureAwait(false);

            await CreateSubscription(subscriptionId, subscriptionName);
            CreateResourceGroup();
        }
        finally
        {
            await container.StopAsync();
        }
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