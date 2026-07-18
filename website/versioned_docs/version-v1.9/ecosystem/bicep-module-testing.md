---
sidebar_position: 5
slug: /ecosystem/bicep-module-testing
description: Unit-test individual Bicep modules against Topaz — deploy modules in isolation, assert resource properties, and catch contract regressions before full-stack deployment.
keywords: [bicep module testing, bicep unit test, bicep test azure, topaz bicep, azure bicep emulator, bicep module validation, bicep contract testing]
---

# How to unit-test Bicep modules with Topaz

This guide shows you how to deploy individual Bicep modules against Topaz's ARM endpoint and assert their outputs using the Azure SDK. Testing modules in isolation catches contract regressions early, before a full-stack deployment surfaces them.

A complete runnable example is available in [`Examples/Topaz.Example.BicepModuleTesting`](https://github.com/TheCloudTheory/Topaz/tree/main/Examples/Topaz.Example.BicepModuleTesting).

## Why module-level tests matter

Bicep modules define contracts: they accept parameters and produce resources. When a module changes — a parameter renamed, a property default altered, a SKU constraint added — those changes should be caught by a targeted test, not by a failed production deployment.

With Topaz you can:

- Deploy a single module against a local ARM endpoint in milliseconds
- Assert resource properties (SKU, location, tags, kind) using the Azure SDK
- Parameterise tests to cover multiple configurations of the same module
- Run these tests in CI without any Azure subscription

## Project structure

```
Topaz.Example.BicepModuleTesting/
├── Topaz.Example.BicepModuleTesting.csproj
├── TopazFixture.cs
├── StorageModuleTests.cs
├── CosmosDbModuleTests.cs
└── modules/
    ├── storage.bicep
    └── cosmos.bicep
```

## Installation

```bash
dotnet new xunit -n Topaz.Example.BicepModuleTesting
cd Topaz.Example.BicepModuleTesting

dotnet add package Testcontainers
dotnet add package Azure.ResourceManager
dotnet add package Azure.ResourceManager.Storage
dotnet add package Azure.ResourceManager.CosmosDB
dotnet add package Azure.Identity
dotnet add package TheCloudTheory.Topaz.Identity
dotnet add package TheCloudTheory.Topaz.ResourceManager
```

## The shared fixture

Start one Topaz container for the entire test suite and create a stable resource group for all module deployments:

```csharp
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Topaz.Identity;
using Xunit;

public class TopazFixture : IAsyncLifetime
{
    public IContainer Container { get; private set; } = null!;
    public ArmClient ArmClient { get; private set; } = null!;
    public ResourceGroupResource ResourceGroup { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Container = new ContainerBuilder()
            .WithImage("thecloudtheory/topaz-host:latest")
            .WithPortBinding(8899, 8899)   // ARM
            .WithPortBinding(8891, 8891)   // Storage
            .WithPortBinding(8895, 8895)   // Cosmos DB
            .WithName("topaz.local.dev")
            .WithCommand("--log-level", "Warning")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8899))
            .Build();

        await Container.StartAsync();

        ArmClient = new ArmClient(
            new AzureLocalCredential(),
            "00000000-0000-0000-0000-000000000001");

        var subscription = await ArmClient.GetDefaultSubscriptionAsync();
        var rgOperation = await subscription.GetResourceGroups().CreateOrUpdateAsync(
            Azure.WaitUntil.Completed,
            "rg-bicep-module-tests",
            new ResourceGroupData(Azure.Core.AzureLocation.WestEurope));

        ResourceGroup = rgOperation.Value;
    }

    public async Task DisposeAsync() => await Container.DisposeAsync();
}

[CollectionDefinition("Topaz")]
public class TopazCollection : ICollectionFixture<TopazFixture> { }
```

## The deploy helper

A thin wrapper around `az deployment group create` keeps test code readable:

```csharp
using System.Diagnostics;

public static class BicepDeployer
{
    public static void Deploy(
        string resourceGroup,
        string templateFile,
        string? parameters = null)
    {
        var args = $"deployment group create " +
                   $"--resource-group {resourceGroup} " +
                   $"--template-file {templateFile}" +
                   (parameters is not null ? $" --parameters {parameters}" : "");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "az",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"Bicep deployment failed: {process.StandardError.ReadToEnd()}");
    }
}
```

## Module: storage.bicep

```bicep
@description('Name of the storage account')
param storageAccountName string

@description('Azure region')
param location string = 'westeurope'

@allowed(['Standard_LRS', 'Standard_GRS', 'Premium_LRS'])
param sku string = 'Standard_LRS'

param tags object = {}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: sku
  }
  kind: 'StorageV2'
  tags: tags
}

output storageAccountId string = storage.id
output storageAccountName string = storage.name
```

## Module: cosmos.bicep

```bicep
@description('Name of the Cosmos DB account')
param accountName string

@description('Azure region')
param location string = 'westeurope'

param tags object = {}

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-11-15' = {
  name: accountName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    locations: [
      {
        locationName: location
        failoverPriority: 0
      }
    ]
  }
  tags: tags
}

output accountEndpoint string = cosmosAccount.properties.documentEndpoint
```

## Storage module tests

```csharp
using Azure.ResourceManager.Storage.Models;
using Xunit;

[Collection("Topaz")]
public class StorageModuleTests
{
    private readonly TopazFixture _topaz;

    public StorageModuleTests(TopazFixture topaz) => _topaz = topaz;

    [Theory]
    [InlineData("stmodtest001", "Standard_LRS")]
    [InlineData("stmodtest002", "Standard_GRS")]
    [InlineData("stmodtest003", "Premium_LRS")]
    public async Task Deploy_StorageModule_ShouldHaveCorrectSku(
        string accountName, string expectedSku)
    {
        BicepDeployer.Deploy(
            "rg-bicep-module-tests",
            "modules/storage.bicep",
            $"storageAccountName={accountName} sku={expectedSku}");

        var account = (await _topaz.ResourceGroup.GetStorageAccountAsync(accountName)).Value;

        Assert.Equal(expectedSku, account.Data.Sku.Name.ToString());
        Assert.Equal("StorageV2", account.Data.Kind.ToString());
    }

    [Fact]
    public async Task Deploy_StorageModule_WithTags_ShouldPreserveTags()
    {
        BicepDeployer.Deploy(
            "rg-bicep-module-tests",
            "modules/storage.bicep",
            "storageAccountName=sttagtest " +
            "tags.environment=test " +
            "tags.owner=platform-team");

        var account = (await _topaz.ResourceGroup.GetStorageAccountAsync("sttagtest")).Value;

        Assert.Equal("test", account.Data.Tags["environment"]);
        Assert.Equal("platform-team", account.Data.Tags["owner"]);
    }

    [Fact]
    public async Task Deploy_StorageModule_DefaultSku_ShouldBeStandardLrs()
    {
        BicepDeployer.Deploy(
            "rg-bicep-module-tests",
            "modules/storage.bicep",
            "storageAccountName=stdefaultsku");

        var account = (await _topaz.ResourceGroup.GetStorageAccountAsync("stdefaultsku")).Value;

        // Verify the module default is what we expect — a regression test for the module contract
        Assert.Equal("Standard_LRS", account.Data.Sku.Name.ToString());
    }
}
```

## Cosmos DB module tests

```csharp
using Xunit;

[Collection("Topaz")]
public class CosmosDbModuleTests
{
    private readonly TopazFixture _topaz;

    public CosmosDbModuleTests(TopazFixture topaz) => _topaz = topaz;

    [Fact]
    public async Task Deploy_CosmosModule_ShouldProvisionGlobalDocumentDb()
    {
        BicepDeployer.Deploy(
            "rg-bicep-module-tests",
            "modules/cosmos.bicep",
            "accountName=cosmos-mod-test");

        var accounts = _topaz.ResourceGroup.GetCosmosDBAccounts();
        var account = accounts.FirstOrDefault(a => a.Data.Name == "cosmos-mod-test");

        Assert.NotNull(account);
        Assert.Equal("GlobalDocumentDB", account.Data.Kind.ToString());
    }

    [Fact]
    public async Task Deploy_CosmosModule_WithTags_ShouldPreserveTags()
    {
        BicepDeployer.Deploy(
            "rg-bicep-module-tests",
            "modules/cosmos.bicep",
            "accountName=cosmos-tagged " +
            "tags.environment=test");

        var accounts = _topaz.ResourceGroup.GetCosmosDBAccounts();
        var account = accounts.First(a => a.Data.Name == "cosmos-tagged");

        Assert.Equal("test", account.Data.Tags["environment"]);
    }
}
```

## Running the tests

```bash
dotnet test --logger "console;verbosity=normal"
```

:::tip[Parameterised module coverage]

Use xUnit's `[Theory]` with `[InlineData]` to cover every allowed value for a constrained `@allowed` parameter. This is the fastest way to verify that a module's constraints match your organisation's policy.

:::

:::tip[Integration with the IaC testing tutorial]

This guide focuses on individual Bicep modules. For testing full Terraform and Bicep configurations — `init/plan/apply` pipelines with multiple resources — see [Testing infrastructure-as-code with Topaz](../tutorials/testing-iac-with-topaz.md).

:::
