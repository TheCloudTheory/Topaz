using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
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
    }

    public async Task DisposeAsync() => await Container.DisposeAsync();
}

[CollectionDefinition("Topaz")]
public class TopazCollection : ICollectionFixture<TopazFixture> { }
