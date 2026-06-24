using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using JetBrains.Annotations;
using Testcontainers.Topaz;
using Topaz.Identity;
using Topaz.ResourceManager;
using Xunit;

namespace Topaz.Example.BicepModuleTesting;

[UsedImplicitly]
public class TopazFixture : IAsyncLifetime
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

    private TopazContainer Container { get; set; } = null!;
    private ArmClient ArmClient { get; set; } = null!;
    public ResourceGroupResource ResourceGroup { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        const string defaultSubscriptionId = "00000000-0000-0000-0000-000000000001";
        Container = new TopazBuilder()
            .WithDefaultSubscription(Guid.Parse(defaultSubscriptionId))
            .WithLoggingToFile()
            .WithLogLevel(TopazLogLevel.Debug)
            .WithName("topaz.local.dev")
            .Build();

        await Container.StartAsync();
        await Task.Delay(3000);

        BicepDeployer.Login();

        ArmClient = new ArmClient(
            new AzureLocalCredential(Globals.GlobalAdminId),
            defaultSubscriptionId,
            ArmClientOptions);

        var subscription = await ArmClient.GetDefaultSubscriptionAsync();
        var rgOperation = await subscription.GetResourceGroups().CreateOrUpdateAsync(
            Azure.WaitUntil.Completed,
            "rg-bicep-module-tests",
            new ResourceGroupData(AzureLocation.WestEurope));

        ResourceGroup = rgOperation.Value;
    }

    public async Task DisposeAsync() => await Container.DisposeAsync();
}

[CollectionDefinition("Topaz")]
public class TopazCollection : ICollectionFixture<TopazFixture> { }
