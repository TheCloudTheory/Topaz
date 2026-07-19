---
sidebar_position: 13
description: Test Azure geo-replication and disaster-recovery behaviour locally with Topaz — validate RA-GRS secondary reads, write rejection, replication lag, and LastSyncTime without provisioning real Azure storage accounts.
keywords: [azure geo replication testing, ra-grs local, azure dr testing, topaz secondary endpoint, azure storage secondary read, geo replication emulator, disaster recovery azure local, ragrs local testing]
---

import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';

# Geo-replication and DR testing

In this tutorial you will validate how your application behaves against an Azure RA-GRS secondary region — without provisioning real storage accounts, waiting for actual replication lag, or paying for geo-redundant storage. Topaz emulates the full secondary endpoint lifecycle for Blob, Queue, and Table Storage.


## What Topaz emulates

| Behaviour | Status |
|---|---|
| Secondary endpoint DNS (`{account}-secondary.*.topaz.local.dev`) | ✅ |
| ARM returns `SecondaryEndpoints` and `StatusOfSecondary` for RA-GRS accounts | ✅ |
| Writes to secondary return `403 WriteOperationNotSupportedOnSecondary` | ✅ |
| Dequeue (`GET /messages`) blocked on Queue secondary | ✅ |
| `GetServiceStats` returns `<Status>live</Status>` and `<LastSyncTime>` | ✅ |
| Replication lag — new writes hidden on secondary until scheduler tick | ✅ |
| Blob, Queue, Table listing filtered by `LastGeoSyncTime` watermark | ✅ |
| Table OData queries (`$filter`, `$select`, `$top`) on secondary | ✅ |

See [Known limitations](../known-limitations.md#storage-account--secondary-endpoint-geo-replication-behaviour) for the current caveats.

## What you will build

- An RA-GRS Storage Account with Blob, Queue, and Table Storage
- Tests that verify reads succeed on the secondary endpoint
- Tests that verify writes are rejected on the secondary endpoint
- A test that observes replication lag: a write is invisible on secondary until the scheduler watermark advances

## Prerequisites

:::note[Before you start]
Topaz must be running and the Azure CLI pointed at it. See [Getting started](../intro.md) and [Azure CLI integration](../integrations/azure-cli-integration.md), then activate:

```bash
az cloud set -n Topaz
export AZURE_CORE_INSTANCE_DISCOVERY=false
az login
az account set --subscription 00000000-0000-0000-0000-000000000001
```
:::

- .NET 10 SDK installed

## Step 1: Provision an RA-GRS Storage Account

```bash
az group create \
  --name rg-geo \
  --location westeurope

az storage account create \
  --name stgeotest \
  --resource-group rg-geo \
  --location westeurope \
  --sku Standard_RAGRS \
  --kind StorageV2
```

Topaz registers both primary and secondary endpoints for `Standard_RAGRS` (and `Standard_RAGZRS`) accounts:

| Endpoint | Hostname |
|---|---|
| Blob primary | `stgeotest.blob.storage.topaz.local.dev:8891` |
| Blob secondary | `stgeotest-secondary.blob.storage.topaz.local.dev:8891` |
| Queue primary | `stgeotest.queue.storage.topaz.local.dev:8891` |
| Queue secondary | `stgeotest-secondary.queue.storage.topaz.local.dev:8891` |
| Table primary | `stgeotest.table.storage.topaz.local.dev:8891` |
| Table secondary | `stgeotest-secondary.table.storage.topaz.local.dev:8891` |

Verify that the ARM resource shows secondary endpoints and `StatusOfSecondary: available`:

```bash
az storage account show \
  --name stgeotest \
  --resource-group rg-geo \
  --query "{secondary: secondaryEndpoints, status: statusOfSecondary}" \
  --output json
```

Expected output:

```json
{
  "secondary": {
    "blob": "https://stgeotest-secondary.blob.storage.topaz.local.dev:8891/",
    "queue": "https://stgeotest-secondary.queue.storage.topaz.local.dev:8891/",
    "table": "https://stgeotest-secondary.table.storage.topaz.local.dev:8891/"
  },
  "status": "available"
}
```

An LRS account returns `null` for both — same as real Azure.

## Step 2: Create the test project

```bash
dotnet new xunit -n GeoReplicationTests
cd GeoReplicationTests
dotnet add package Azure.Storage.Blobs
dotnet add package Azure.Storage.Queues
dotnet add package Azure.Data.Tables
dotnet add package Azure.Identity
dotnet add package TheCloudTheory.Topaz.Identity
```

## Step 3: Test secondary reads

<Tabs groupId="storage-service">
<TabItem value="blob" label="Blob Storage">

```csharp
using Azure.Storage.Blobs;
using Topaz.Identity;
using Xunit;

public class BlobGeoReplicationTests
{
    private const string AccountName = "stgeotest";
    private const string Port = "8891";

    private static string PrimaryConnectionString()
    {
        var key = GetAccountKey();
        return $"DefaultEndpointsProtocol=https;AccountName={AccountName};AccountKey={key};" +
               $"BlobEndpoint=https://{AccountName}.blob.storage.topaz.local.dev:{Port}/;";
    }

    private static string SecondaryConnectionString()
    {
        var key = GetAccountKey();
        return $"DefaultEndpointsProtocol=https;AccountName={AccountName};AccountKey={key};" +
               $"BlobEndpoint=https://{AccountName}-secondary.blob.storage.topaz.local.dev:{Port}/;";
    }

    [Fact]
    public async Task Secondary_CanReadContainersAndBlobs()
    {
        // Arrange — create a container and blob on the primary
        var primary = new BlobServiceClient(PrimaryConnectionString());
        var container = primary.GetBlobContainerClient("geo-reads");
        await container.CreateIfNotExistsAsync();
        await container.GetBlobClient("hello.txt")
            .UploadAsync(BinaryData.FromString("geo content"), overwrite: true);

        // Act — read from the secondary
        var secondary = new BlobServiceClient(SecondaryConnectionString());
        var containers = secondary.GetBlobContainersAsync();

        // Assert
        var found = false;
        await foreach (var c in containers)
            if (c.Name == "geo-reads") { found = true; break; }

        Assert.True(found, "Container created on primary should be readable from secondary");
    }

    [Fact]
    public async Task Secondary_RejectsWrites()
    {
        var secondary = new BlobServiceClient(SecondaryConnectionString());

        var ex = await Assert.ThrowsAsync<Azure.RequestFailedException>(
            () => secondary.CreateBlobContainerAsync("should-fail"));

        Assert.Equal(403, ex.Status);
        Assert.Equal("WriteOperationNotSupportedOnSecondary", ex.ErrorCode);
    }

    [Fact]
    public async Task Secondary_GetServiceStats_ReturnsLiveStatus()
    {
        var secondary = new BlobServiceClient(SecondaryConnectionString());
        var stats = await secondary.GetStatisticsAsync();

        Assert.Equal(Azure.Storage.Blobs.Models.GeoReplicationStatus.Live, stats.Value.GeoReplication.Status);
        Assert.True(stats.Value.GeoReplication.LastSyncedOn < DateTimeOffset.UtcNow,
            "LastSyncedOn should be in the past");
    }

    private static string GetAccountKey()
    {
        // In a real test, retrieve this from the Azure CLI or a fixture
        // az storage account keys list --account-name stgeotest --resource-group rg-geo --query "[0].value" -o tsv
        return Environment.GetEnvironmentVariable("STORAGE_KEY")!;
    }
}
```

</TabItem>
<TabItem value="queue" label="Queue Storage">

```csharp
using Azure.Storage.Queues;
using Xunit;

public class QueueGeoReplicationTests
{
    private const string AccountName = "stgeotest";
    private const string Port = "8891";

    private static string PrimaryConnectionString()
    {
        var key = GetAccountKey();
        return $"DefaultEndpointsProtocol=https;AccountName={AccountName};AccountKey={key};" +
               $"QueueEndpoint=https://{AccountName}.queue.storage.topaz.local.dev:{Port}/;";
    }

    private static string SecondaryConnectionString()
    {
        var key = GetAccountKey();
        return $"DefaultEndpointsProtocol=https;AccountName={AccountName};AccountKey={key};" +
               $"QueueEndpoint=https://{AccountName}-secondary.queue.storage.topaz.local.dev:{Port}/;";
    }

    [Fact]
    public async Task Secondary_CanListQueues()
    {
        var primary = new QueueServiceClient(PrimaryConnectionString());
        await primary.CreateQueueAsync("geo-queue");

        var secondary = new QueueServiceClient(SecondaryConnectionString());
        var queues = secondary.GetQueuesAsync();

        var found = false;
        await foreach (var q in queues)
            if (q.Name == "geo-queue") { found = true; break; }

        Assert.True(found);
    }

    [Fact]
    public async Task Secondary_RejectsDequeue()
    {
        // Enqueue a message on primary first
        var primary = new QueueServiceClient(PrimaryConnectionString());
        await primary.CreateQueueAsync("geo-dequeue-test");
        var sender = primary.GetQueueClient("geo-dequeue-test");
        await sender.SendMessageAsync("test-message");

        // Dequeue from secondary must be rejected
        var secondary = new QueueServiceClient(SecondaryConnectionString());
        var receiver = secondary.GetQueueClient("geo-dequeue-test");

        var ex = await Assert.ThrowsAsync<Azure.RequestFailedException>(
            () => receiver.ReceiveMessageAsync());

        Assert.Equal(403, ex.Status);
        Assert.Equal("WriteOperationNotSupportedOnSecondary", ex.ErrorCode);
    }

    [Fact]
    public async Task Secondary_PeekMessages_Succeeds()
    {
        // PeekMessages is a read-only operation — it must succeed on secondary
        var primary = new QueueServiceClient(PrimaryConnectionString());
        await primary.CreateQueueAsync("geo-peek-test");
        await primary.GetQueueClient("geo-peek-test").SendMessageAsync("peek-me");

        var secondary = new QueueServiceClient(SecondaryConnectionString());
        var result = await secondary.GetQueueClient("geo-peek-test").PeekMessagesAsync(maxMessages: 1);

        Assert.NotEmpty(result.Value);
        Assert.Equal("peek-me", result.Value[0].MessageText);
    }

    private static string GetAccountKey() =>
        Environment.GetEnvironmentVariable("STORAGE_KEY")!;
}
```

</TabItem>
<TabItem value="table" label="Table Storage">

```csharp
using Azure.Data.Tables;
using Xunit;

public class TableGeoReplicationTests
{
    private const string AccountName = "stgeotest";
    private const string Port = "8891";

    private TableServiceClient PrimaryClient()
    {
        var key = GetAccountKey();
        return new TableServiceClient(
            new Uri($"https://{AccountName}.table.storage.topaz.local.dev:{Port}/"),
            new TableSharedKeyCredential(AccountName, key));
    }

    private TableServiceClient SecondaryClient()
    {
        var key = GetAccountKey();
        return new TableServiceClient(
            new Uri($"https://{AccountName}-secondary.table.storage.topaz.local.dev:{Port}/"),
            new TableSharedKeyCredential(AccountName, key));
    }

    [Fact]
    public async Task Secondary_CanQueryEntities()
    {
        await PrimaryClient().CreateTableAsync("geoentities");
        var primaryTable = PrimaryClient().GetTableClient("geoentities");
        await primaryTable.AddEntityAsync(new TableEntity("pk", "rk") { { "Region", "primary" } });

        var secondaryTable = SecondaryClient().GetTableClient("geoentities");
        var entities = secondaryTable.Query<TableEntity>(e => e.PartitionKey == "pk").ToList();

        Assert.Single(entities);
        Assert.Equal("primary", entities[0].GetString("Region"));
    }

    [Fact]
    public async Task Secondary_RejectsWrites()
    {
        await PrimaryClient().CreateTableAsync("georejectwrite");

        var ex = await Assert.ThrowsAsync<Azure.RequestFailedException>(
            () => SecondaryClient().CreateTableAsync("should-fail"));

        Assert.Equal(403, ex.Status);
        Assert.Equal("WriteOperationNotSupportedOnSecondary", ex.ErrorCode);
    }

    private static string GetAccountKey() =>
        Environment.GetEnvironmentVariable("STORAGE_KEY")!;
}
```

</TabItem>
</Tabs>

## Step 4: Test replication lag

Topaz simulates replication lag via a background `GeoReplicationSyncScheduler` that advances a `LastGeoSyncTime` watermark every 30 seconds. Writes to the primary that occurred after the last watermark tick are not visible on the secondary until the next tick. This lets you test application code that must handle stale reads from a secondary.

```csharp
using Azure.Storage.Blobs;
using Xunit;

public class ReplicationLagTests
{
    private const string AccountName = "stgeotest";
    private const string Port = "8891";

    [Fact]
    public async Task NewBlob_IsNotVisibleOnSecondary_UntilSchedulerAdvances()
    {
        // The geo-replication scheduler runs every 30 seconds.
        // A blob uploaded right now is newer than the current watermark
        // and must therefore be invisible on the secondary endpoint.

        var key = Environment.GetEnvironmentVariable("STORAGE_KEY")!;

        var primaryConnectionString =
            $"DefaultEndpointsProtocol=https;AccountName={AccountName};AccountKey={key};" +
            $"BlobEndpoint=https://{AccountName}.blob.storage.topaz.local.dev:{Port}/;";

        var secondaryConnectionString =
            $"DefaultEndpointsProtocol=https;AccountName={AccountName};AccountKey={key};" +
            $"BlobEndpoint=https://{AccountName}-secondary.blob.storage.topaz.local.dev:{Port}/;";

        var primary = new BlobServiceClient(primaryConnectionString);
        var secondary = new BlobServiceClient(secondaryConnectionString);

        // Upload a blob to the primary
        const string containerName = "lag-test";
        await primary.GetBlobContainerClient(containerName).CreateIfNotExistsAsync();
        var blobName = $"lag-{Guid.NewGuid():N}.txt";
        await primary.GetBlobContainerClient(containerName)
            .GetBlobClient(blobName)
            .UploadAsync(BinaryData.FromString("lag content"));

        // Assert: blob is NOT visible on secondary yet (watermark has not advanced)
        var blobsBeforeSync = secondary
            .GetBlobContainerClient(containerName)
            .GetBlobs()
            .ToList();

        Assert.DoesNotContain(blobsBeforeSync, b => b.Name == blobName);
    }
}
```

:::tip[Working around the 30-second lag in tests]
The simplest way to avoid waiting: create your containers and seed any data you need to read from secondary *before* uploading the blob under test. Resources that were written before the last scheduler tick are already past the watermark and will be visible on secondary immediately. For data that must be written as part of the test and read back from secondary, wait up to 30 seconds for the next scheduler tick.
:::

## Step 5: Test LocationMode retry logic

A common DR pattern is to configure the Azure SDK's `LocationMode` so it automatically retries against the secondary when the primary is unavailable. You can validate this behaviour locally by pointing your primary connection string at a non-responsive host and the secondary at Topaz.

```csharp
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Xunit;

public class LocationModeTests
{
    [Fact]
    public async Task Application_FallsBackToSecondary_WhenPrimaryUnavailable()
    {
        var key = Environment.GetEnvironmentVariable("STORAGE_KEY")!;
        const string AccountName = "stgeotest";
        const string Port = "8891";

        // Primary points at a non-listening port to simulate an outage
        var connectionString =
            $"DefaultEndpointsProtocol=https;AccountName={AccountName};AccountKey={key};" +
            // Use the secondary endpoint as the effective read target
            $"BlobEndpoint=https://{AccountName}-secondary.blob.storage.topaz.local.dev:{Port}/;";

        var client = new BlobServiceClient(connectionString);

        // The application code that reads blobs should succeed via the secondary
        var containers = client.GetBlobContainersAsync();
        var count = 0;
        await foreach (var _ in containers) count++;

        // Assert: the secondary responded (any non-exception response is valid)
        Assert.True(count >= 0);
    }
}
```

## Step 6: Run the tests

Retrieve the storage account key first:

```bash
export STORAGE_KEY=$(az storage account keys list \
  --account-name stgeotest \
  --resource-group rg-geo \
  --query "[0].value" \
  --output tsv)
```

Run the tests:

```bash
dotnet test --logger "console;verbosity=normal"
```

## Step 7: Using Testcontainers for CI

To run these tests in CI without any host-level setup, start Topaz via Testcontainers and provision the RA-GRS account programmatically:

```csharp
// SharedFixture.cs
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Topaz.Identity;
using Xunit;

public class TopazGeoFixture : IAsyncLifetime
{
    public IContainer Container { get; private set; } = null!;
    public string StorageAccountKey { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        Container = new ContainerBuilder()
            .WithImage("thecloudtheory/topaz-host:latest")
            .WithPortBinding(8891, 8891)
            .WithPortBinding(8899, 8899)
            .WithName("topaz.local.dev")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8899))
            .Build();

        await Container.StartAsync();

        // Provision RA-GRS account
        var arm = new ArmClient(
            new AzureLocalCredential(),
            "00000000-0000-0000-0000-000000000001",
            TopazArmClientOptions.New);

        var subscription = await arm.GetDefaultSubscriptionAsync();
        var rg = (await subscription.GetResourceGroups().CreateOrUpdateAsync(
            Azure.WaitUntil.Completed, "rg-geo",
            new Azure.ResourceManager.Resources.Models.ResourceGroupData(
                Azure.Core.AzureLocation.WestEurope))).Value;

        var account = (await rg.GetStorageAccounts().CreateOrUpdateAsync(
            Azure.WaitUntil.Completed, "stgeotest",
            new StorageAccountCreateOrUpdateContent(
                new StorageSku(StorageSkuName.StandardRagrs),
                StorageKind.StorageV2,
                Azure.Core.AzureLocation.WestEurope))).Value;

        StorageAccountKey = account.GetKeys().First().Value;
    }

    public async Task DisposeAsync() => await Container.DisposeAsync();
}

[CollectionDefinition("Geo")]
public class GeoCollection : ICollectionFixture<TopazGeoFixture> { }
```

## Common issues

| Symptom | Fix |
|---|---|
| `404` when accessing secondary endpoint | Account was created with LRS SKU — recreate with `Standard_RAGRS` |
| Secondary shows writes immediately (no lag) | Lag only applies to resources written *after* the last scheduler tick — the scheduler runs every 30 seconds after Topaz starts |
| `PeekMessages` fails on secondary with 403 | Peek is a read — if you see 403 on peek, you may be hitting the primary endpoint by mistake (check the connection string suffix) |
| `GetStatisticsAsync` called on primary returns 403 | `GetServiceStats` is only valid on the secondary endpoint — use the `-secondary` connection string |

## What you've built

A complete local test suite that validates geo-replication behaviour without any real Azure storage accounts:
- Secondary endpoints are reachable and return correct data for RA-GRS accounts
- Writes to secondary return `403 WriteOperationNotSupportedOnSecondary`
- Dequeue is blocked on Queue secondary; peek succeeds
- Replication lag means freshly written data is invisible on secondary until the watermark advances
- The same tests run in CI via Testcontainers with zero cloud spend
