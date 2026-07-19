---
sidebar_position: 10
description: Test Terraform and Bicep infrastructure-as-code against Topaz — assert provisioned resources, SKUs, tags, and negative cases without a real Azure subscription.
keywords: [terraform testing, bicep testing, iac testing, topaz iac, azure infrastructure testing, terraform test azure, bicep test azure, testcontainers iac]
---

import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';

# Testing infrastructure-as-code with Topaz

In this tutorial, we will write automated tests that apply Terraform and Bicep configurations against Topaz, then assert the resulting resources using the Azure SDK. This goes beyond verifying that `apply` runs without error — it validates the actual shape, SKU, and tags of the provisioned resources.

A complete runnable example is available in [`Examples/Topaz.Example.IaCTesting`](https://github.com/TheCloudTheory/Topaz/tree/main/Examples/Topaz.Example.IaCTesting).

## What you will build

- An xUnit test project that starts Topaz via Testcontainers
- A Terraform configuration and a Bicep template that each provision a Storage account
- Assertions against the provisioned resources using `Azure.ResourceManager`
- A negative test that verifies an absent resource is not returned

## Prerequisites

- Topaz installed (see [Getting started](../intro.md))
- DNS setup completed and Topaz certificate trusted
- Terraform CLI installed (`terraform --version`)
- Azure CLI installed (`az --version`)
- .NET 10 SDK installed

## Step 1: Create the test project

```bash
dotnet new xunit -n Topaz.Example.IaCTesting
cd Topaz.Example.IaCTesting
dotnet add package Testcontainers
dotnet add package Azure.ResourceManager
dotnet add package Azure.ResourceManager.Storage
dotnet add package Azure.Identity
dotnet add package TheCloudTheory.Topaz.Identity
dotnet add package TheCloudTheory.Topaz.ResourceManager
```

## Step 2: Create the shared Topaz fixture

The fixture starts a single Topaz container for the entire test suite. The ARM port (8899) is used as the readiness signal.

```csharp
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit;

public class TopazFixture : IAsyncLifetime
{
    public IContainer Container { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Container = new ContainerBuilder()
            .WithImage("thecloudtheory/topaz-host:latest")
            .WithPortBinding(8890, 8890)   // Storage
            .WithPortBinding(8891, 8891)   // Blob Storage
            .WithPortBinding(8899, 8899)   // ARM / Resource Manager
            .WithName("topaz.local.dev")
            .WithCommand("--log-level", "Warning")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8899))
            .Build();

        await Container.StartAsync();
    }

    public async Task DisposeAsync() => await Container.DisposeAsync();
}

[CollectionDefinition("Topaz")]
public class TopazCollection : ICollectionFixture<TopazFixture> { }
```

## Step 3: Write the Terraform configuration

Create a file at `terraform/main.tf`:

```hcl
terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }
}

provider "azurerm" {
  features {}

  environment                      = "public"
  resource_provider_registrations  = "none"
  skip_provider_registration       = true

  # Topaz ARM endpoint
  arm_endpoint = "https://topaz.local.dev:8899/"

  # Any non-empty values are accepted by Topaz
  subscription_id = "00000000-0000-0000-0000-000000000001"
  tenant_id       = "50717675-3E5E-4A1E-8CB5-C62D8BE8CA48"
  client_id       = "topaz-terraform"
  client_secret   = "topaz-terraform"
}

resource "azurerm_resource_group" "rg" {
  name     = "rg-iac-test"
  location = "West Europe"
}

resource "azurerm_storage_account" "storage" {
  name                     = "stiactest"
  resource_group_name      = azurerm_resource_group.rg.name
  location                 = azurerm_resource_group.rg.location
  account_tier             = "Standard"
  account_replication_type = "LRS"

  tags = {
    environment = "test"
    owner       = "platform-team"
  }
}
```

## Step 4: Write the Bicep template

Create a file at `bicep/storage.bicep`:

```bicep
param storageAccountName string = 'stbiceptest'
param location string = 'westeurope'

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' existing = {
  name: 'rg-iac-test'
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  tags: {
    environment: 'test'
    owner: 'platform-team'
  }
}
```

## Step 5: Write the tests

<Tabs groupId="iac-tool">
<TabItem value="terraform" label="Terraform">

```csharp
using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Topaz.Identity;
using Xunit;

[Collection("Topaz")]
public class TerraformIaCTests
{
    private readonly ArmClient _arm;

    public TerraformIaCTests()
    {
        _arm = new ArmClient(
            new AzureLocalCredential(),
            "00000000-0000-0000-0000-000000000001");
    }

    [Fact]
    public async Task Apply_ShouldProvisionStorageAccount_WithCorrectSku()
    {
        // Apply the Terraform config against Topaz
        RunTerraform("init");
        RunTerraform("apply -auto-approve");

        // Assert using the Azure SDK
        var subscription = await _arm.GetDefaultSubscriptionAsync();
        var rg = (await subscription.GetResourceGroupAsync("rg-iac-test")).Value;
        var storage = (await rg.GetStorageAccountAsync("stiactest")).Value;

        Assert.Equal("Standard_LRS", storage.Data.Sku.Name.ToString());
        Assert.Equal("test", storage.Data.Tags["environment"]);
        Assert.Equal("platform-team", storage.Data.Tags["owner"]);
    }

    [Fact]
    public async Task Destroy_ShouldRemoveStorageAccount()
    {
        RunTerraform("apply -auto-approve");
        RunTerraform("destroy -auto-approve");

        var subscription = await _arm.GetDefaultSubscriptionAsync();
        var rg = (await subscription.GetResourceGroupAsync("rg-iac-test")).Value;

        // The storage account should no longer exist
        var accounts = rg.GetStorageAccounts();
        Assert.Empty(accounts.Where(a => a.Data.Name == "stiactest"));
    }

    private static void RunTerraform(string arguments)
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "terraform",
                Arguments = arguments,
                WorkingDirectory = "terraform",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };
        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            throw new InvalidOperationException($"terraform {arguments} failed: {error}");
        }
    }
}
```

</TabItem>
<TabItem value="bicep" label="Bicep">

```csharp
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using Topaz.Identity;
using Xunit;

[Collection("Topaz")]
public class BicepIaCTests
{
    private readonly ArmClient _arm;

    public BicepIaCTests()
    {
        _arm = new ArmClient(
            new AzureLocalCredential(),
            "00000000-0000-0000-0000-000000000001");
    }

    [Fact]
    public async Task Deploy_StorageBicep_ShouldProvisionWithCorrectSku()
    {
        var subscription = await _arm.GetDefaultSubscriptionAsync();
        var rg = (await subscription.GetResourceGroupAsync("rg-iac-test")).Value;

        // Deploy the Bicep module via az CLI
        RunAzCli("deployment group create " +
                 "--resource-group rg-iac-test " +
                 "--template-file bicep/storage.bicep");

        var storage = (await rg.GetStorageAccountAsync("stbiceptest")).Value;

        Assert.Equal("Standard_LRS", storage.Data.Sku.Name.ToString());
        Assert.Equal("test", storage.Data.Tags["environment"]);
        Assert.Equal("platform-team", storage.Data.Tags["owner"]);
    }

    [Fact]
    public async Task Query_NonExistentAccount_ShouldReturnEmpty()
    {
        var subscription = await _arm.GetDefaultSubscriptionAsync();
        var rg = (await subscription.GetResourceGroupAsync("rg-iac-test")).Value;

        var accounts = rg.GetStorageAccounts();
        Assert.DoesNotContain(accounts, a => a.Data.Name == "st-does-not-exist");
    }

    private static void RunAzCli(string arguments)
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "az",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };
        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            throw new InvalidOperationException($"az {arguments} failed: {error}");
        }
    }
}
```

</TabItem>
</Tabs>

## Step 6: Run the tests

Make sure Topaz is running (either as a background container started by the fixture, or manually via `topaz-host`), then run:

```bash
dotnet test
```

You should see the test suite start Topaz, apply the configuration, and assert the resources — all without touching a real Azure subscription.

:::tip[Speed]

The biggest time cost is `terraform init` on first run. After the provider is downloaded, subsequent test runs that reuse the same Terraform working directory are significantly faster.

:::

:::tip[Switching to production]

The only difference between Topaz and real Azure is the ARM endpoint URL and the Terraform provider `arm_endpoint` setting. Your Terraform configurations, Bicep templates, and SDK assertion code are identical.

:::
