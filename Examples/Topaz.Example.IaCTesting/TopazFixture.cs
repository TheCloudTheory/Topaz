using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Topaz.Identity;
using Xunit;

namespace Topaz.Example.IaCTesting;

public class TopazFixture : IAsyncLifetime
{
    public IContainer Container { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Container = new ContainerBuilder("thecloudtheory/topaz-host:latest")
            .WithPortBinding(8890, 8890)
            .WithPortBinding(8891, 8891)
            .WithPortBinding(8899, 8899)
            .WithName("topaz.local.dev")
            .WithCommand("--log-level", "Warning")
            .Build();

        await Container.StartAsync();
        await Task.Delay(3000);

        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new Topaz.ResourceManager.TopazArmClient(credential);
        await topaz.CreateSubscriptionAsync(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            "iac-tests");

        var arm = new ArmClient(
            credential,
            "00000000-0000-0000-0000-000000000001",
            Topaz.ResourceManager.TopazArmClientOptions.New);

        var subscription = await arm.GetDefaultSubscriptionAsync();
        await subscription.GetResourceGroups().CreateOrUpdateAsync(
            Azure.WaitUntil.Completed,
            "rg-iac-test",
            new ResourceGroupData(AzureLocation.WestEurope));
    }

    public async Task DisposeAsync() => await Container.DisposeAsync();
}

[CollectionDefinition("Topaz")]
public class TopazCollection : ICollectionFixture<TopazFixture> { }
