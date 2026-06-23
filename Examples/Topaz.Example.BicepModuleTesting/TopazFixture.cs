using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Topaz.Identity;
using Xunit;

namespace Topaz.Example.BicepModuleTesting;

public class TopazFixture : IAsyncLifetime
{
    public IContainer Container { get; private set; } = null!;
    public ArmClient ArmClient { get; private set; } = null!;
    public ResourceGroupResource ResourceGroup { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Container = new ContainerBuilder("thecloudtheory/topaz-host:latest")
            .WithPortBinding(8891, 8891)
            .WithPortBinding(8895, 8895)
            .WithPortBinding(8899, 8899)
            .WithName("topaz.local.dev")
            .WithCommand("--log-level", "Warning")
            .Build();

        await Container.StartAsync();
        await Task.Delay(3000);

        ArmClient = new ArmClient(
            new AzureLocalCredential(Globals.GlobalAdminId),
            "00000000-0000-0000-0000-000000000001");

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
