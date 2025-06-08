using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using DotNet.Testcontainers.Builders;
using JetBrains.Annotations;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Example.Dotnet;

[UsedImplicitly]
internal class Program
{
    public static async Task Main()
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
            const string subscriptionName = "topaz.example";
            const string resourceGroupName = "rg-topaz-example";
    
            await container.StartAsync()
                .ConfigureAwait(false);
            
            // We added 5000ms of wait statement just to make sure Topaz
            // is already running. Note there's most likely a much better way
            // to do that via wait strategy from TestContainers.
            await Task.Delay(5000);

            await CreateSubscription(subscriptionId, subscriptionName);
            
            var credentials = new AzureLocalCredential();
            var armClient = new ArmClient(credentials, subscriptionId.ToString(), TopazArmClientOptions.New);
            
            CreateResourceGroup(armClient, resourceGroupName);
        }
        finally
        {
            await container.StopAsync();
        }
    }

    private static void CreateResourceGroup(ArmClient armClient, string resourceGroupName)
    {
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroups = subscription.GetResourceGroups();
        
        _ = resourceGroups.CreateOrUpdate(WaitUntil.Completed, resourceGroupName, new ResourceGroupData(AzureLocation.WestEurope));
        
        Console.WriteLine($"Resource group [{resourceGroupName}] created successfully!");
    }

    private static async Task CreateSubscription(Guid subscriptionId, string subscriptionName)
    {
        using var topaz = new TopazArmClient();
        
        await topaz.CreateSubscriptionAsync(subscriptionId, subscriptionName);
        
        Console.WriteLine($"Subscription [{subscriptionId}, {subscriptionName}] created successfully!");
    }
}