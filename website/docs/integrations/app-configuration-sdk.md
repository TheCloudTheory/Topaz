---
sidebar_position: 7
slug: /app-configuration-sdk
description: Use the Azure App Configuration SDK with Topaz to load and refresh configuration values from a locally emulated App Configuration store — no real Azure subscription required.
keywords: [azure app configuration local, app configuration sdk topaz, azure app configuration emulator, local app config, feature flags local]
---

import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';

# How to use Azure App Configuration SDK with Topaz

This guide shows you how to use the [Azure App Configuration SDK](https://learn.microsoft.com/en-us/azure/azure-app-configuration/quickstart-dotnet-core-app) against a locally emulated App Configuration store provided by Topaz.

## Prerequisites

- Topaz installed and the certificate trusted at the OS level (see [Getting started](../intro.md))
- .NET 8 or later

## Step 1 — Add NuGet packages

```bash
dotnet add package Topaz.AspNetCore.Extensions
dotnet add package Topaz.ResourceManager
dotnet add package Testcontainers.Topaz
dotnet add package Microsoft.Azure.AppConfiguration.AspNetCore
dotnet add package Azure.ResourceManager.AppConfiguration
```

## Step 2 — Start Topaz and provision resources

Spin up a Topaz container and create a subscription, resource group, and App Configuration store before the application starts:

```csharp
var container = new TopazBuilder(useNightlyImage: true).Build();

await container.StartAsync();
await Task.Delay(5000); // wait for Topaz to be ready

var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
var subscriptionId = Guid.NewGuid();
const string resourceGroupName = "rg-myapp";
const string storeName = "myappconfig";

var resourceGroupIdentifier = ResourceGroupIdentifier.From(resourceGroupName);

await builder.Configuration.AddTopaz(subscriptionId, Globals.GlobalAdminId)
    .AddSubscription(subscriptionId, "myapp", credentials)
    .AddResourceGroup(subscriptionId, resourceGroupName, AzureLocation.WestEurope)
    .AddConfigurationStore(
        resourceGroupIdentifier,
        storeName,
        new AppConfigurationStoreData(AzureLocation.WestEurope, new AppConfigurationSku("Standard")))
    .AddKeyValuesToStore(resourceGroupIdentifier, storeName, "MyApp:Setting1", "hello")
    .AddKeyValuesToStore(resourceGroupIdentifier, storeName, "MyApp:Setting2", Guid.NewGuid().ToString());
```

## Step 3 — Connect the SDK to the emulated store

Use `TopazResourceHelpers.GetAppConfigurationStoreEndpoint` to resolve the local endpoint, then connect with `AzureLocalCredential`:

```csharp
var appConfigEndpoint = TopazResourceHelpers.GetAppConfigurationStoreEndpoint(storeName);

builder.Configuration.AddAzureAppConfiguration(options =>
{
    options.Connect(new Uri(appConfigEndpoint), new AzureLocalCredential(Globals.GlobalAdminId))
        .Select("MyApp:*")
        .ConfigureRefresh(refresh =>
        {
            refresh.RegisterAll();
        })
        .UseFeatureFlags();
});

builder.Services.AddAzureAppConfiguration();
```

## Step 4 — Read configuration values

Configuration values loaded from the emulated store are available through `IConfiguration` like any other provider:

```csharp
app.MapGet("/config", (IConfiguration configuration) =>
{
    return configuration.GetSection("MyApp").AsEnumerable();
});
```

## Key Vault references

App Configuration supports [Key Vault references](https://learn.microsoft.com/en-us/azure/azure-app-configuration/use-key-vault-references-dotnet-core) — values stored in App Configuration that point to secrets in Key Vault. Topaz emulates both services, so you can use this pattern locally without any real Azure resources.

### Provision Key Vault and link secrets to App Configuration

During setup, create a Key Vault and use `AddKeyValuesToStoreAsSecret` to add an App Configuration key whose value is a Key Vault reference:

```csharp
await builder.Configuration.AddTopaz(subscriptionId, Globals.GlobalAdminId)
    // ...
    .AddKeyVault(
        resourceGroupIdentifier,
        keyVaultName,
        new KeyVaultCreateOrUpdateContent(
            AzureLocation.WestEurope,
            new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard))),
        secrets: new Dictionary<string, string>
        {
            { "secrets-generic-secret", "This is just example secret!" }
        },
        Globals.GlobalAdminId)
    .AddStorageAccountConnectionStringAsSecret(
        resourceGroupIdentifier, storageAccountName, keyVaultName,
        "connectionstring-storageaccount", Globals.GlobalAdminId)
    .AddKeyValuesToStoreAsSecret(
        resourceGroupIdentifier, keyVaultName, Globals.GlobalAdminId, storeName,
        "MyApp:Secret", "VerySecretValue");
```

### Configure the SDK to resolve Key Vault references

Pass a `SecretClient` to the App Configuration SDK so it can dereference Key Vault URIs at runtime. Set `DisableChallengeResourceVerification = true` because the local Topaz certificate is self-signed:

```csharp
var credentials = new AzureLocalCredential(Globals.GlobalAdminId);

builder.Configuration.AddAzureAppConfiguration(options =>
{
    options.ReplicaDiscoveryEnabled = true;
    options.LoadBalancingEnabled = true;
    options.ConfigureKeyVault(keyVaultOptions =>
    {
        keyVaultOptions.SetCredential(credentials);
        keyVaultOptions.Register(new SecretClient(
            vaultUri: TopazResourceHelpers.GetKeyVaultEndpoint(keyVaultName),
            credential: credentials,
            new SecretClientOptions
            {
                DisableChallengeResourceVerification = true
            }));
    });
    options.Connect(new Uri(appConfigEndpoint), credentials)
        .Select("MyApp:*")
        .ConfigureRefresh(refresh => refresh.RegisterAll())
        .UseFeatureFlags();
});
```

You can also load secrets directly from Key Vault (outside of App Configuration) using `AddAzureKeyVault`:

```csharp
builder.Configuration.AddAzureKeyVault(
    TopazResourceHelpers.GetKeyVaultEndpoint(keyVaultName),
    new AzureLocalCredential(Globals.GlobalAdminId));
```

## Replica support

Topaz supports App Configuration replicas. Add a replica to a different region during provisioning:

```csharp
.AddConfigurationStoreReplica(
    resourceGroupIdentifier,
    storeName,
    "ne",
    new AppConfigurationReplicaData { Location = AzureLocation.NorthEurope })
```

Then enable replica discovery and load balancing in the SDK options:

```csharp
builder.Configuration.AddAzureAppConfiguration(options =>
{
    options.ReplicaDiscoveryEnabled = true;
    options.LoadBalancingEnabled = true;
    options.Connect(new Uri(appConfigEndpoint), new AzureLocalCredential(Globals.GlobalAdminId))
        .Select("MyApp:*");
});
```

## Full example

A runnable example combining App Configuration, Key Vault, and Storage is available in the Topaz repository under [`Examples/Topaz.Example.Dotnet.Web`](https://github.com/TheCloudTheory/Topaz/tree/main/Examples/Topaz.Example.Dotnet.Web).
