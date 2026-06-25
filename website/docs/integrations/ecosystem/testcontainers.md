---
sidebar_position: 2
slug: /ecosystem/testcontainers
description: Use the official Testcontainers.Topaz package to run Topaz automatically in NUnit, xUnit, and MSTest projects — shared fixture pattern with full container lifecycle management.
keywords: [topaz testcontainers, azure emulator integration tests, testcontainers dotnet topaz, local azure unit tests]
---

# How to write integration tests with Testcontainers

This guide shows you how to use the official [Testcontainers.Topaz](https://www.nuget.org/packages/TheCloudTheory.Topaz.Testcontainers) package to run a Topaz instance automatically in your test suite.

A fully working end-to-end example — including xUnit fixtures, Bicep deployments via the Azure CLI, and SDK-level assertions — is available in the [Topaz GitHub repository](https://github.com/TheCloudTheory/Topaz/tree/main/Examples/Topaz.Example.BicepModuleTesting).

## Installation

```bash
dotnet add package TheCloudTheory.Topaz.Testcontainers
```

## Prerequisites

### 1. DNS resolution

Topaz uses `topaz.local.dev` as its base hostname. Service endpoints use subdomains (e.g. `myaccount.blob.storage.topaz.local.dev`). Configure a wildcard DNS resolver so all subdomains resolve automatically:

**macOS / Linux (dnsmasq)**

```shell
# Install
brew install dnsmasq        # macOS
sudo apt install dnsmasq    # Debian/Ubuntu

# Route all *.topaz.local.dev to localhost
echo "address=/.topaz.local.dev/127.0.0.1" | sudo tee /etc/dnsmasq.d/topaz.conf

# Restart
sudo brew services restart dnsmasq   # macOS
sudo systemctl restart dnsmasq       # Linux
```

On macOS, also add a resolver file so the system uses dnsmasq for this domain:

```shell
sudo mkdir -p /etc/resolver
echo "nameserver 127.0.0.1" | sudo tee /etc/resolver/topaz.local.dev
```

**Fallback: `/etc/hosts`**

If you cannot install a DNS resolver, add an entry for each hostname your tests use:

```
127.0.0.1 topaz.local.dev
127.0.0.1 myaccount.blob.storage.topaz.local.dev
127.0.0.1 myvault.vault.topaz.local.dev
```

This requires a new entry per resource name and does not support wildcards.

### 2. Certificate trust

Topaz uses a self-signed TLS certificate. Call `TopazContainer.InstallCertificateToCurrentUserStore()` after the container starts to install it into the current user's trusted root store. Remove it on teardown with `TopazContainer.UninstallCertificateFromCurrentUserStore()`. No certificate validation bypass is needed.

### 3. Required environment variables

Two environment variables must be set whenever you call the Azure CLI (`az`) from within your tests or helper code:

| Variable | Value | Purpose |
|---|---|---|
| `AZURE_CORE_INSTANCE_DISCOVERY` | `false` | Skips the AAD metadata discovery request that fails against a local emulator. |
| `HTTPS_PROXY` | `http://topaz.local.dev:44380` | Routes `az` traffic through Topaz's built-in CONNECT proxy so TLS termination works against the self-signed certificate. |

Set them on the `ProcessStartInfo` before starting the process:

```csharp
process.StartInfo.Environment["AZURE_CORE_INSTANCE_DISCOVERY"] = "false";
process.StartInfo.Environment["HTTPS_PROXY"] = "http://topaz.local.dev:44380";
```

Or export them in your shell / CI environment if you prefer a global approach:

```shell
export AZURE_CORE_INSTANCE_DISCOVERY=false
export HTTPS_PROXY=http://topaz.local.dev:44380
```

:::note
These variables are only required when invoking the Azure CLI. Azure SDK clients in the same process do not need `HTTPS_PROXY` — they rely on the certificate installed by `TopazContainer.InstallCertificateToCurrentUserStore()`.
:::

## Basic usage

```csharp
public sealed class MyServiceTests : IAsyncLifetime
{
    private readonly TopazContainer _topaz = new TopazBuilder().Build();

    public async Task InitializeAsync()
    {
        await _topaz.StartAsync();

        // Installs the Topaz cert into CurrentUser\Root so HttpClient and
        // Azure SDK clients trust it without disabling certificate validation.
        TopazContainer.InstallCertificateToCurrentUserStore();
    }

    public async Task DisposeAsync()
    {
        TopazContainer.UninstallCertificateFromCurrentUserStore();
        await _topaz.DisposeAsync();
    }

    [Fact]
    public async Task StorageTest()
    {
        var blobUri = _topaz.GetStorageBlobUri("myaccount");
        // Use blobUri with Azure.Storage.Blobs.BlobServiceClient ...

        var cosmosUri = _topaz.GetCosmosDbUri("mycosmosaccount");
        // Use cosmosUri with Microsoft.Azure.Cosmos.CosmosClient ...
    }
}
```

## The reusable fixture pattern

The best practice is to start Topaz once per test suite (not per test) using a shared setup fixture. All the major .NET test frameworks support this through their own lifecycle hooks.

### NUnit

NUnit's `[SetUpFixture]` runs once per namespace assembly, making it ideal for a shared container:

```csharp
using NUnit.Framework;

[SetUpFixture]
public class TopazFixture
{
    private TopazContainer _topaz = null!;

    [OneTimeSetUp]
    public async Task StartAsync()
    {
        _topaz = new TopazBuilder().Build();
        await _topaz.StartAsync();
        TopazContainer.InstallCertificateToCurrentUserStore();
    }

    [OneTimeTearDown]
    public async Task StopAsync()
    {
        TopazContainer.UninstallCertificateFromCurrentUserStore();
        await _topaz.DisposeAsync();
    }
}
```

### xUnit

xUnit uses a shared `IAsyncLifetime` class fixture:

```csharp
using Xunit;

public class TopazFixture : IAsyncLifetime
{
    public TopazContainer Topaz { get; } = new TopazBuilder().Build();

    public async Task InitializeAsync()
    {
        await Topaz.StartAsync();
        TopazContainer.InstallCertificateToCurrentUserStore();
    }

    public async Task DisposeAsync()
    {
        TopazContainer.UninstallCertificateFromCurrentUserStore();
        await Topaz.DisposeAsync();
    }
}

[CollectionDefinition("Topaz")]
public class TopazCollection : ICollectionFixture<TopazFixture> { }

[Collection("Topaz")]
public class KeyVaultTests
{
    private readonly TopazFixture _fixture;

    public KeyVaultTests(TopazFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CreateKeyVault_ShouldSucceed()
    {
        var kvUri = _fixture.Topaz.GetKeyVaultUri("kv-test");
        // arrange / act / assert using Azure SDK
    }
}
```

### MSTest

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TopazFixture
{
    private static TopazContainer _topaz = null!;

    [AssemblyInitialize]
    public static async Task StartAsync(TestContext _)
    {
        _topaz = new TopazBuilder().Build();
        await _topaz.StartAsync();
        TopazContainer.InstallCertificateToCurrentUserStore();
    }

    [AssemblyCleanup]
    public static async Task StopAsync()
    {
        TopazContainer.UninstallCertificateFromCurrentUserStore();
        await _topaz.DisposeAsync();
    }
}
```

## Builder options

`TopazBuilder` exposes several optional configuration methods:

| Method | Description |
|---|---|
| `WithDefaultSubscription(Guid)` | Sets the subscription ID created on startup. Defaults to a random GUID if omitted. |
| `WithLogLevel(TopazLogLevel)` | Sets the verbosity of the Topaz host process (e.g. `TopazLogLevel.Debug`). |
| `WithLoggingToFile(bool refreshLog = true)` | Enables logging to file. Pass `false` to keep the log from previous runs. |
| `WithEmulatorIpAddress(string)` | Overrides the IP address the emulator listens on. |

Example:

```csharp
var topaz = new TopazBuilder()
    .WithDefaultSubscription(Guid.Parse("00000000-0000-0000-0000-000000000001"))
    .WithLogLevel(TopazLogLevel.Debug)
    .WithLoggingToFile()
    .Build();
```

## Writing tests against Topaz

Once the container is running, use `AzureLocalCredential` and the standard Azure SDK clients. Retrieve service URIs via the typed helpers on `TopazContainer`:

```csharp
using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Topaz.Identity;
using Topaz.ResourceManager;

public class KeyVaultTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

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

## Container-to-container setup

When your tests run inside a Docker container (e.g. CI), attach both containers to a shared network and inject the Topaz certificate into the secondary container's CA bundle:

```csharp
var network = new NetworkBuilder().WithName(Guid.NewGuid().ToString("D")).Build();

var topaz = new TopazBuilder()
    .WithNetwork(network)
    .Build();

await topaz.StartAsync();

var certPem = TopazContainer.GetCertificatePem();

var myContainer = new ContainerBuilder()
    .WithNetwork(network)
    .WithExtraHost("topaz.local.dev", topaz.IpAddress)
    .WithExtraHost("myaccount.blob.storage.topaz.local.dev", topaz.IpAddress)
    // Add one WithExtraHost per subdomain your tests use
    .WithResourceMapping(Encoding.UTF8.GetBytes(certPem), "/tmp/topaz.crt")
    .WithEnvironment("REQUESTS_CA_BUNDLE", "/usr/lib64/az/lib/python3.12/site-packages/certifi/cacert.pem")
    .Build();

await myContainer.StartAsync();

// Append the cert to the CA bundle inside the container:
await myContainer.ExecAsync(["/bin/sh", "-c",
    "cat /tmp/topaz.crt >> /usr/lib64/az/lib/python3.12/site-packages/certifi/cacert.pem"]);
```

## URI helpers

| Method | Returns |
|---|---|
| `GetResourceManagerUri()` | `https://topaz.local.dev:{port}` |
| `GetKeyVaultUri(vaultName)` | `https://{vaultName}.vault.topaz.local.dev:{port}` |
| `GetStorageBlobUri(account)` | `https://{account}.blob.storage.topaz.local.dev:{port}` |
| `GetStorageQueueUri(account)` | `https://{account}.queue.storage.topaz.local.dev:{port}` |
| `GetStorageTableUri(account)` | `https://{account}.table.storage.topaz.local.dev:{port}` |
| `GetCosmosDbUri(account)` | `https://{account}.documents.topaz.local.dev:{port}/` |
| `GetContainerRegistryUri(name)` | `https://{name}.cr.topaz.local.dev:{port}` |
| `GetServiceBusAmqpUri(ns)` | `amqp://{ns}.servicebus.topaz.local.dev:{port}` |
| `GetServiceBusHttpUri(ns)` | `https://{ns}.servicebus.topaz.local.dev:{port}` |
| `GetEventHubAmqpUri(ns)` | `amqp://{ns}.eventhub.topaz.local.dev:{port}` |
| `GetEventHubHttpUri(ns)` | `https://{ns}.eventhub.topaz.local.dev:{port}` |
| `GetAppServiceUri(appName)` | `https://{appName}.scm.azurewebsites.topaz.local.dev:{port}` |

All ports are mapped to random host ports at runtime.

## Port reference

| Constant | Port | Service |
|---|---|---|
| `ResourceManagerPort` | 8899 | ARM / Resource Manager (HTTPS) |
| `KeyVaultPort` | 8898 | Key Vault (HTTPS) |
| `StoragePort` | 8891 | Blob / Queue / Table / File Storage (HTTP) |
| `CosmosDbPort` | 8895 | Cosmos DB |
| `ContainerRegistryPort` | 8892 | Container Registry |
| `ServiceBusAmqpPort` | 8889 | Service Bus (AMQP) |
| `ServiceBusHttpPort` | 8887 | Service Bus (HTTP) |
| `EventHubAmqpPort` | 8888 | Event Hubs (AMQP) |
| `EventHubHttpPort` | 8897 | Event Hubs (HTTP) |
| `AppServicePort` | 8896 | App Service / Kudu |
| `ConnectProxyPort` | 44380 | HTTP CONNECT proxy (used for `az login` from the host) |

The `WaitStrategy` targets port `8899` (ARM) as that is the last service to become ready.
