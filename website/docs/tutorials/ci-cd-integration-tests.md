---
sidebar_position: 1
description: Set up Topaz in a GitHub Actions CI pipeline to run real Azure integration tests on every pull request — no cloud subscription, no credentials, no per-run cost.
keywords: [topaz ci cd, github actions azure integration tests, azure emulator ci, topaz github actions, integration testing azure local, topaz docker ci, azure integration tests no subscription]
---

# CI/CD: integration tests without cloud costs

In this tutorial you will build a GitHub Actions workflow that starts Topaz as a container, provisions Azure resources with the Azure CLI, runs a .NET xUnit integration test suite against them, and tears everything down — all without a real Azure subscription or service principal.

This is the workflow shown in the [CI/CD use case](/use-cases#reliable-integration-tests-without-cloud-costs).

## What you will build

- A GitHub Actions workflow (`integration-tests.yml`) that runs on every pull request
- A Topaz container started as a service in the CI job
- Resources (Storage account, Service Bus namespace) provisioned by the Azure CLI inside the workflow
- An xUnit test project that sends messages and reads blobs against Topaz
- Zero secrets or service principals stored in GitHub

## Prerequisites

- A GitHub repository containing a .NET application
- Basic familiarity with GitHub Actions syntax

## Project structure

```
.github/
  workflows/
    integration-tests.yml
src/
  MyApp/
tests/
  MyApp.IntegrationTests/
    MyApp.IntegrationTests.csproj
    StorageTests.cs
    ServiceBusTests.cs
```

## Step 1: Create the test project

```bash
dotnet new xunit -n MyApp.IntegrationTests
cd MyApp.IntegrationTests
dotnet add package Azure.Storage.Blobs
dotnet add package Azure.Messaging.ServiceBus
dotnet add package Azure.Identity
dotnet add package TheCloudTheory.Topaz.Identity
```

## Step 2: Write the integration tests

### Storage test

```csharp
// tests/MyApp.IntegrationTests/StorageTests.cs
using Azure.Storage.Blobs;
using Topaz.Identity;
using Xunit;

public class StorageTests
{
    // Topaz Blob Storage endpoint — same DNS name used in the workflow
    private const string BlobEndpoint =
        "https://stcitest.blob.storage.topaz.local.dev:8891";

    [Fact]
    public async Task UploadAndDownload_ShouldRoundtrip()
    {
        var client = new BlobServiceClient(
            new Uri(BlobEndpoint),
            new AzureLocalCredential());

        var container = client.GetBlobContainerClient("uploads");
        await container.CreateIfNotExistsAsync();

        // Upload
        var content = BinaryData.FromString("integration-test-payload");
        await container.GetBlobClient("test.txt").UploadAsync(content, overwrite: true);

        // Download and verify
        var downloaded = await container
            .GetBlobClient("test.txt")
            .DownloadContentAsync();

        Assert.Equal("integration-test-payload", downloaded.Value.Content.ToString());
    }
}
```

### Service Bus test

```csharp
// tests/MyApp.IntegrationTests/ServiceBusTests.cs
using Azure.Messaging.ServiceBus;
using Xunit;

public class ServiceBusTests
{
    // UseDevelopmentEmulator=true connects over plain AMQP (port 8889)
    private const string ConnectionString =
        "Endpoint=sb://sbns-ci.servicebus.topaz.local.dev:8889;" +
        "SharedAccessKeyName=RootManageSharedAccessKey;" +
        "SharedAccessKey=SAS_KEY_VALUE;" +
        "UseDevelopmentEmulator=true;";

    [Fact]
    public async Task SendAndReceive_ShouldDeliverMessage()
    {
        await using var client = new ServiceBusClient(ConnectionString);

        await using var sender = client.CreateSender("orders");
        await sender.SendMessageAsync(new ServiceBusMessage("ci-order-001"));

        await using var receiver = client.CreateReceiver("orders");
        var message = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(10));

        Assert.NotNull(message);
        Assert.Equal("ci-order-001", message.Body.ToString());
        await receiver.CompleteMessageAsync(message);
    }
}
```

## Step 3: Write the GitHub Actions workflow

```yaml
# .github/workflows/integration-tests.yml
name: Integration tests

on:
  pull_request:
  push:
    branches: [main]

jobs:
  integration:
    runs-on: ubuntu-latest

    steps:
      # ── 1. Checkout ────────────────────────────────────────────────
      - uses: actions/checkout@v4

      # ── 2. Start Topaz ─────────────────────────────────────────────
      - name: Start Topaz
        run: |
          docker run -d \
            --name topaz \
            --network host \
            -e TOPAZ_DEFAULT_SUBSCRIPTION=00000000-0000-0000-0000-000000000001 \
            thecloudtheory/topaz-host:latest \
            --log-level Warning

          # Wait until the ARM port is reachable
          for i in $(seq 1 30); do
            curl -sk https://localhost:8899/ > /dev/null 2>&1 && break
            sleep 2
          done

      # ── 3. Trust the Topaz certificate ─────────────────────────────
      - name: Trust Topaz certificate
        run: |
          docker cp topaz:/app/certificate/topaz.crt /usr/local/share/ca-certificates/topaz.crt
          update-ca-certificates

      # ── 4. Install and configure the Azure CLI ─────────────────────
      - name: Configure Azure CLI for Topaz
        run: |
          curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
          # Register the Topaz cloud using the canonical cloud.json
          curl -fsSL https://raw.githubusercontent.com/TheCloudTheory/Topaz/refs/heads/main/cloud.json -o cloud.json
          az cloud register -n Topaz --cloud-config @cloud.json
          az cloud set -n Topaz
          export AZURE_CORE_INSTANCE_DISCOVERY=false
          az login --allow-no-subscriptions
          az account set --subscription 00000000-0000-0000-0000-000000000001

      # ── 5. Provision test resources ────────────────────────────────
      - name: Provision resources
        run: |
          az group create \
            --name rg-ci \
            --location westeurope

          az storage account create \
            --name stcitest \
            --resource-group rg-ci \
            --location westeurope \
            --sku Standard_LRS

          az servicebus namespace create \
            --name sbns-ci \
            --resource-group rg-ci \
            --location westeurope \
            --sku Standard

          az servicebus queue create \
            --name orders \
            --namespace-name sbns-ci \
            --resource-group rg-ci

      # ── 6. Build ───────────────────────────────────────────────────
      - name: Build
        run: dotnet build --configuration Release

      # ── 7. Unit tests ──────────────────────────────────────────────
      - name: Unit tests
        run: dotnet test --filter "Category=Unit" --no-build --configuration Release

      # ── 8. Integration tests ───────────────────────────────────────
      - name: Integration tests
        env:
          AZURE_CORE_INSTANCE_DISCOVERY: "false"
        run: |
          dotnet test tests/MyApp.IntegrationTests \
            --no-build \
            --configuration Release \
            --logger "trx;LogFileName=results.trx"

      # ── 9. Publish results ─────────────────────────────────────────
      - name: Publish test results
        if: always()
        uses: dorny/test-reporter@v1
        with:
          name: Integration test results
          path: "**/*.trx"
          reporter: dotnet-trx
```

## Step 4: Add DNS resolution inside the runner

Topaz uses subdomain-based routing (e.g. `stcitest.blob.storage.topaz.local.dev`). On a GitHub-hosted Linux runner, add the required entries to `/etc/hosts` so the runner resolves these names to `127.0.0.1`:

```yaml
      - name: Configure DNS
        run: |
          sudo tee -a /etc/hosts <<EOF
          127.0.0.1 topaz.local.dev
          127.0.0.1 stcitest.blob.storage.topaz.local.dev
          127.0.0.1 sbns-ci.servicebus.topaz.local.dev
          127.0.0.1 topaz.local.dev
          EOF
```

Add this step **before** "Start Topaz".

:::tip[Wildcard DNS in CI]

If you provision many resource names dynamically, install `dnsmasq` and add a wildcard rule for `*.topaz.local.dev → 127.0.0.1` instead of adding individual `/etc/hosts` entries.

:::

## Step 5: Verify the pipeline

Push the changes and open a pull request. The Actions run should show all steps green:

| Step | Expected output |
|---|---|
| Start Topaz | Container ID printed, port 8899 reachable |
| Provision resources | `"provisioningState": "Succeeded"` for each resource |
| Unit tests | All pass (no Topaz dependency) |
| Integration tests | `StorageTests` and `ServiceBusTests` pass |

## Common issues

| Symptom | Fix |
|---|---|
| `curl: (35) OpenSSL SSL_connect` during health check | Certificate not yet trusted — move the trust step before the health check loop |
| `Name or service not known` for `stcitest.blob.storage.topaz.local.dev` | DNS step missing or not yet applied — check `/etc/hosts` |
| `MessagingEntityNotFoundException` | Queue was not created before the test ran — add a wait or dependency between the provision and test steps |
| Port conflict on `--network host` | Another service on the runner uses the same port — change Topaz's port with `--blob-port` / `--service-bus-port` flags |

## What you've built

A GitHub Actions pipeline that:
- Starts a full Azure environment in under 30 seconds
- Provisions real Azure resources with the Azure CLI
- Runs integration tests against real Azure SDK clients
- Incurs zero cloud spend and requires no secrets or service principals

The same pattern works on Azure DevOps, GitLab CI, and any runner that can execute Docker.
