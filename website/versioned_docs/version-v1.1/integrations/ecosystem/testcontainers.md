---
sidebar_position: 2
slug: /ecosystem/testcontainers
---

# Testcontainers

[Testcontainers for .NET](https://dotnet.testcontainers.org/) is the recommended way to run Topaz automatically inside test projects and CI pipelines. It manages the full container lifecycle — pulling the image, starting the container before tests run, and disposing of it cleanly afterwards.

## Installation

```bash
dotnet add package Testcontainers
```

## The reusable fixture pattern

The best practice is to start Topaz once per test suite (not per test) using a shared setup fixture. All the major .NET test frameworks support this through their own lifecycle hooks.

### NUnit

NUnit's `[SetUpFixture]` runs once per namespace assembly, making it ideal for a shared container:

```csharp
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using NUnit.Framework;

[SetUpFixture]
public class TopazFixture
{
    private IContainer _container = null!;

    [OneTimeSetUp]
    public async Task StartAsync()
    {
        _container = new ContainerBuilder()
            .WithImage("thecloudtheory/topaz-cli:<tag>")
            .WithPortBinding(8899)   // ARM / Resource Manager
            .WithPortBinding(8898)   // Key Vault
            .WithPortBinding(8891)   // Blob Storage
            .WithPortBinding(8890)   // Table Storage
            .WithPortBinding(8897)   // Event Hub (HTTP)
            .WithName("topaz.local.dev")
            .WithCommand("start", "--log-level", "Information")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8899))
            .Build();

        await _container.StartAsync();
    }

    [OneTimeTearDown]
    public async Task StopAsync()
    {
        await _container.DisposeAsync();
    }
}
```

Each test class in the same namespace can then interact with Topaz on `localhost` using the standard Azure SDKs or the `TopazEnvironmentBuilder` API.

### xUnit

xUnit uses a shared `IAsyncLifetime` class fixture:

```csharp
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit;

public class TopazFixture : IAsyncLifetime
{
    private IContainer _container = null!;

    public async Task InitializeAsync()
    {
        _container = new ContainerBuilder()
            .WithImage("thecloudtheory/topaz-cli:<tag>")
            .WithPortBinding(8899)
            .WithPortBinding(8898)
            .WithPortBinding(8891)
            .WithPortBinding(8890)
            .WithPortBinding(8897)
            .WithName("topaz.local.dev")
            .WithCommand("start", "--log-level", "Information")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8899))
            .Build();

        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

// Declare the fixture on each collection that needs Topaz
[CollectionDefinition("Topaz")]
public class TopazCollection : ICollectionFixture<TopazFixture> { }

[Collection("Topaz")]
public class KeyVaultTests
{
    [Fact]
    public async Task CreateKeyVault_ShouldSucceed()
    {
        // arrange / act / assert using Azure SDK against localhost
    }
}
```

### MSTest

```csharp
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TopazFixture
{
    private static IContainer _container = null!;

    [AssemblyInitialize]
    public static async Task StartAsync(TestContext _)
    {
        _container = new ContainerBuilder()
            .WithImage("thecloudtheory/topaz-cli:<tag>")
            .WithPortBinding(8899)
            .WithPortBinding(8898)
            .WithPortBinding(8891)
            .WithPortBinding(8890)
            .WithPortBinding(8897)
            .WithName("topaz.local.dev")
            .WithCommand("start", "--log-level", "Information")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8899))
            .Build();

        await _container.StartAsync();
    }

    [AssemblyCleanup]
    public static async Task StopAsync()
    {
        await _container.DisposeAsync();
    }
}
```

## Writing tests against Topaz

Once the container is running, use `AzureLocalCredential` and the standard Azure SDK clients pointed at `localhost`. The `TopazArmClientOptions` type configures the ARM client to bypass the Azure endpoint discovery that would otherwise try to reach the public cloud.

```csharp
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Topaz.Identity;
using Topaz.ResourceManager;

public class KeyVaultTests
{
    // Use a fixed subscription GUID per test class to avoid collision
    private static readonly Guid SubscriptionId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

    [OneTimeSetUp]
    public async Task SetUp()
    {
        // Create test subscription and resource group via Topaz CLI or ARM client
        await Program.Main(["subscription", "create",
            "--id", SubscriptionId.ToString(),
            "--name", "test-sub"]);
        await Program.Main(["group", "create",
            "--name", "rg-test",
            "--location", "westeurope",
            "--subscription-id", SubscriptionId.ToString()]);
    }

    [Test]
    public void CreateKeyVault_ShouldBeAvailableAfterCreation()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);

        var resourceGroup = armClient
            .GetDefaultSubscription()
            .GetResourceGroup("rg-test").Value;

        var operation = new KeyVaultCreateOrUpdateContent(
            AzureLocation.WestEurope,
            new KeyVaultProperties(
                Guid.Empty,
                new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard)));

        resourceGroup.GetKeyVaults()
            .CreateOrUpdate(WaitUntil.Completed, "kv-test", operation);

        var kv = resourceGroup.GetKeyVault("kv-test");

        Assert.That(kv.Value.Data.Name, Is.EqualTo("kv-test"));
    }
}
```

## Bringing your own certificate

By default Topaz uses its bundled self-signed certificate. If you want to test HTTPS certificate handling or need specific TLS behaviour, pass the certificate files into the container at startup using `WithResourceMapping`:

```csharp
var certificatePem = File.ReadAllBytes("topaz.crt");
var privateKeyPem  = File.ReadAllBytes("topaz.key");

_container = new ContainerBuilder()
    .WithImage("thecloudtheory/topaz-cli:<tag>")
    .WithPortBinding(8899)
    .WithPortBinding(8898)
    .WithName("topaz.local.dev")
    .WithResourceMapping(certificatePem, "/app/topaz.crt")
    .WithResourceMapping(privateKeyPem,  "/app/topaz.key")
    .WithCommand("start",
        "--certificate-file", "topaz.crt",
        "--certificate-key",  "topaz.key",
        "--log-level", "Information")
    .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8899))
    .Build();
```

The certificate files in the example above are the ones bundled with the Topaz release package. The container is started with the custom certificate, so all HTTPS clients must trust that certificate to connect.

## Using a dynamic image tag

Hard-coding an image tag in tests makes upgrades tedious. Read the tag from an environment variable and fall back to a sensible default:

```csharp
private static readonly string TopazImage =
    Environment.GetEnvironmentVariable("TOPAZ_CLI_CONTAINER_IMAGE")
    ?? "thecloudtheory/topaz-cli:latest";
```

In CI set `TOPAZ_CLI_CONTAINER_IMAGE` to the specific tag being tested. Locally, `latest` keeps things up to date automatically.

## Port reference

| Port | Service |
|---|---|
| 8899 | ARM / Resource Manager (HTTPS) |
| 8898 | Key Vault (HTTPS) |
| 8891 | Blob Storage (HTTP) |
| 8890 | Table Storage (HTTP) |
| 8897 | Event Hub (HTTP) |
| 8888 | Event Hub (AMQP) |
| 8889 | Service Bus (AMQP) |
| 5671 | Service Bus (AMQP/TLS) |

Expose only the ports needed by your tests. The `WaitStrategy` should target port `8899` (ARM) as that is the last service to become ready.
