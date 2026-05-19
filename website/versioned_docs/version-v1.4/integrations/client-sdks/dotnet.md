---
sidebar_position: 1
slug: /client-sdks/dotnet
description: Full API reference for the TheCloudTheory.Topaz.Identity and TheCloudTheory.Topaz.ResourceManager NuGet packages.
keywords: [topaz, dotnet, nuget, identity, credential, arm client, azure sdk, testing]
---

# .NET

Two NuGet packages bridge your .NET test code and local development applications to the Topaz emulator:

| Package | Purpose |
|---|---|
| [`TheCloudTheory.Topaz.Identity`](#topazidentity) | `TokenCredential` and Graph auth-provider implementations that generate tokens accepted by Topaz |
| [`TheCloudTheory.Topaz.ResourceManager`](#topazresourcemanager) | Pre-configured ARM client options, a management HTTP client for Topaz-specific operations, and connection-string / URI helpers for every emulated service |

`Topaz.ResourceManager` depends on `Topaz.Identity` — installing it brings `Topaz.Identity` along automatically.

:::info[Future language support]

Equivalent packages for Java, Python, Go, and Node.js are planned. The credential and helper concepts described here will map directly to each language SDK.

:::

---

## Topaz.Identity

### Installation

```bash
dotnet add package TheCloudTheory.Topaz.Identity
```

### `AzureLocalCredential`

The primary `TokenCredential` for use with any Azure SDK client that calls Topaz. It generates a short-lived JWT that Topaz's auth layer accepts as a valid Entra ID token.

```csharp
using Topaz.Identity;

var credential = new AzureLocalCredential(Globals.GlobalAdminId);
```

**Constructor**

```csharp
AzureLocalCredential(string objectId, bool isForGraph = false, string? preferredUsername = null)
```

| Parameter | Type | Description |
|---|---|---|
| `objectId` | `string` | The Entra ID object ID of the principal. Use `Globals.GlobalAdminId` to act as the built-in superadmin. |
| `isForGraph` | `bool` | Set to `true` when using the credential with Microsoft Graph. Adds Graph-specific claims to the token. Defaults to `false`. |
| `preferredUsername` | `string?` | Optional `preferred_username` claim injected into the token. Useful for tests that inspect the caller's UPN. |

This credential is compatible with every Azure SDK client that accepts a `TokenCredential` — `ArmClient`, `SecretClient`, `BlobServiceClient`, `QueueServiceClient`, `ServiceBusClient`, and so on.

```csharp
using Azure.ResourceManager;
using Topaz.Identity;
using Topaz.ResourceManager;

var credential = new AzureLocalCredential(Globals.GlobalAdminId);
var armClient = new ArmClient(credential, subscriptionId, TopazArmClientOptions.New);
```

### `AzureFixedTokenLocalCredential`

An advanced variant that wraps an already-issued token string. Use this when you need full control over the token claims — for example, to test token expiry or RBAC checks tied to a specific object ID that you obtained from a previous call.

```csharp
AzureFixedTokenLocalCredential(string token)
```

| Parameter | Type | Description |
|---|---|---|
| `token` | `string` | A raw Bearer token string. Topaz validates this the same way it validates tokens from `AzureLocalCredential`. |

### `ManagedIdentityLocalCredential`

A `TokenCredential` that represents a call made by a user-assigned or system-assigned managed identity. Topaz maps the `principalId` in the generated token to the corresponding identity resource.

```csharp
using Topaz.Identity;

var credential = new ManagedIdentityLocalCredential(principalId: Guid.Parse("..."));
```

**Constructor**

```csharp
ManagedIdentityLocalCredential(Guid principalId)
```

| Parameter | Type | Description |
|---|---|---|
| `principalId` | `Guid` | The principal ID of the managed identity resource created in Topaz. |

### `LocalGraphAuthenticationProvider`

An `IAuthenticationProvider` (from `Microsoft.Kiota.Abstractions`) for use with the [Microsoft Graph SDK](https://learn.microsoft.com/graph/sdks/sdks-overview). Uses `Globals.GlobalAdminId` internally, so no configuration is required.

```csharp
using Microsoft.Graph;
using Topaz.Identity;

var graphClient = new GraphServiceClient(
    new HttpClient(),
    new LocalGraphAuthenticationProvider(),
    "https://topaz.local.dev:8899"
);
```

### `LocalGraphFixedTokenAuthenticationProvider`

An `IAuthenticationProvider` that attaches a specific token string as the `Authorization` header. Useful when testing Graph endpoints with a token tied to a non-admin principal.

```csharp
LocalGraphFixedTokenAuthenticationProvider(string token)
```

| Parameter | Type | Description |
|---|---|---|
| `token` | `string` | The full `Authorization` header value (typically `Bearer <jwt>`). |

### `Globals`

| Member | Type | Value | Description |
|---|---|---|---|
| `GlobalAdminId` | `string` (const) | `"00000000-0000-0000-0000-000000000000"` | The built-in superadmin object ID. Topaz grants this principal full access to all resources with no RBAC check. |

---

## Topaz.ResourceManager

### Installation

```bash
dotnet add package TheCloudTheory.Topaz.ResourceManager
```

`Topaz.Identity` is included automatically as a transitive dependency.

### `TopazArmClientOptions`

A factory that returns a pre-configured `ArmClientOptions` instance pointing at the Topaz Resource Manager endpoint (`https://topaz.local.dev:8899`). Pass it as the third argument when constructing any `ArmClient`.

```csharp
using Azure.ResourceManager;
using Topaz.Identity;
using Topaz.ResourceManager;

var credential = new AzureLocalCredential(Globals.GlobalAdminId);
var armClient  = new ArmClient(credential, subscriptionId, TopazArmClientOptions.New);
```

| Member | Returns | Description |
|---|---|---|
| `TopazArmClientOptions.New` | `ArmClientOptions` | A new `ArmClientOptions` with `Environment` set to the Topaz ARM endpoint. |

### `TopazArmClient`

A thin HTTP client for Topaz-specific management operations that are not covered by the standard Azure SDK `ArmClient` (for example, creating emulated subscriptions, managing management-group hierarchies, or exporting ARM templates). It handles authentication internally using the `AzureLocalCredential` you pass in.

```csharp
using Topaz.Identity;
using Topaz.ResourceManager;

await using var client = new TopazArmClient(new AzureLocalCredential(Globals.GlobalAdminId));
await client.CreateSubscriptionAsync(subscriptionId, "my-subscription");
```

**Constructor**

```csharp
TopazArmClient(AzureLocalCredential credentials)
```

**Methods**

#### Subscriptions

| Method | Returns | Description |
|---|---|---|
| `CreateSubscriptionAsync(Guid subscriptionId, string subscriptionName)` | `Task` | Creates a new subscription in Topaz. |
| `UpdateSubscriptionAsync(Guid subscriptionId, string subscriptionName, Dictionary<string, string> tags)` | `Task` | Updates the display name and tags of an existing subscription. |
| `CancelSubscriptionAsync(Guid subscriptionId)` | `Task` | Cancels a subscription. Throws `HttpRequestException` with the error body if the subscription does not exist. |
| `EnableSubscriptionAsync(Guid subscriptionId)` | `Task` | Re-enables a previously cancelled subscription. |

#### Key Vault

| Method | Returns | Description |
|---|---|---|
| `PurgeKeyVault(Guid subscriptionId, string keyVaultName, string location)` | `Task` | Purges a soft-deleted Key Vault so a vault with the same name can be re-created. |

#### Resource Groups / Templates

| Method | Returns | Description |
|---|---|---|
| `ExportTemplateAsync(Guid subscriptionId, string resourceGroupName, string? options, string[]? resources)` | `Task<JsonNode>` | Exports an ARM template for a resource group. `options` and `resources` are passed through to the ARM `exportTemplate` API. |
| `ExportDeploymentTemplateAsync(Guid subscriptionId, string resourceGroupName, string deploymentName)` | `Task<JsonNode>` | Exports the ARM template that was used by a specific deployment. |

#### Management Groups

| Method | Returns | Description |
|---|---|---|
| `CreateManagementGroupAsync(string groupId, string displayName)` | `Task` | Creates a management group at the tenant root. |
| `CreateManagementGroupWithParentAsync(string groupId, string displayName, string parentGroupId)` | `Task` | Creates a management group nested under an existing parent group. |
| `GetManagementGroupAsync(string groupId)` | `Task<JsonNode>` | Returns the management group resource JSON. |
| `GetDescendantsAsync(string groupId)` | `Task<JsonNode>` | Returns the list of descendants (child groups and subscriptions) of a management group. |
| `GetEntitiesAsync()` | `Task<JsonNode>` | Returns all entities (management groups and subscriptions) visible to the caller via `POST /providers/Microsoft.Management/getEntities`. |
| `AssociateSubscriptionWithManagementGroupAsync(string groupId, string subscriptionId)` | `Task<JsonNode>` | Associates a subscription with a management group. |
| `DisassociateSubscriptionFromManagementGroupAsync(string groupId, string subscriptionId)` | `Task` | Removes the association between a subscription and a management group. |
| `GetSubscriptionUnderManagementGroupAsync(string groupId, string subscriptionId)` | `Task<JsonNode>` | Returns the subscription resource as seen from a management group scope. |

#### Hierarchy Settings

| Method | Returns | Description |
|---|---|---|
| `CreateOrUpdateHierarchySettingsAsync(string groupId, bool? requireAuthorizationForGroupCreation, string? defaultManagementGroup)` | `Task<JsonNode>` | Creates or fully replaces the hierarchy settings for a management group. |
| `UpdateHierarchySettingsAsync(string groupId, bool? requireAuthorizationForGroupCreation, string? defaultManagementGroup)` | `Task<JsonNode>` | Partially updates hierarchy settings (PATCH semantics — only supplied fields are changed). |
| `GetHierarchySettingsAsync(string groupId)` | `Task<JsonNode>` | Returns the current hierarchy settings for a management group. |
| `ListHierarchySettingsAsync(string groupId)` | `Task<JsonNode>` | Returns all hierarchy settings objects for a management group. |
| `DeleteHierarchySettingsAsync(string groupId)` | `Task` | Deletes the hierarchy settings for a management group. |

#### Deployments

| Method | Returns | Description |
|---|---|---|
| `ListDeploymentsAtTenantScopeAsync()` | `Task<JsonNode>` | Lists all ARM deployments at tenant scope. |
| `ListDeploymentsAtManagementGroupScopeAsync(string groupId)` | `Task<JsonNode>` | Lists all ARM deployments scoped to a management group. Throws `HttpRequestException` with status 404 if the group does not exist. |

#### Resource Providers

| Method | Returns | Description |
|---|---|---|
| `RegisterProviderAsync(Guid subscriptionId, string providerNamespace)` | `Task<HttpResponseMessage>` | Registers a resource provider namespace on a subscription (for example `Microsoft.KeyVault`). |
| `UnregisterProviderAsync(Guid subscriptionId, string providerNamespace)` | `Task<HttpResponseMessage>` | Unregisters a resource provider namespace. |
| `GetProviderAsync(Guid subscriptionId, string providerNamespace)` | `Task<JsonNode>` | Returns the registration details for a single provider namespace. |
| `ListProvidersAsync(Guid subscriptionId)` | `Task<JsonNode>` | Returns all provider namespaces registered on a subscription. |

#### Role Assignments

| Method | Returns | Description |
|---|---|---|
| `CreateManagementGroupRoleAssignmentAsync(string managementGroupId, string roleAssignmentName, string principalId, string roleDefinitionId)` | `Task` | Creates a role assignment at management-group scope. The assignment scope is set to `/providers/Microsoft.Management/managementGroups/{managementGroupId}` and propagates down to all child subscriptions and resources. |
| `CreateResourceGroupRoleAssignmentAsync(Guid subscriptionId, string resourceGroupName, string roleAssignmentName, string principalId, string roleDefinitionId)` | `Task` | Creates a role assignment scoped to a specific resource group. The assignment propagates down to all resources within that group. |
| `CreateResourceRoleAssignmentAsync(Guid subscriptionId, string resourceGroupName, string providerNamespace, string resourceType, string resourceName, string roleAssignmentName, string principalId, string roleDefinitionId)` | `Task` | Creates a role assignment scoped to a single resource (e.g. a Key Vault or Storage Account). The assignment does not propagate to sibling or parent scopes. |

#### Health

| Method | Returns | Description |
|---|---|---|
| `CheckIfReadyAsync()` | `Task<bool>` | Returns `true` if the Topaz host is running and responding. Uses the unauthenticated `GET /health` endpoint — no credentials required. Useful for readiness polling in integration test fixtures. |

### `TopazResourceHelpers`

A static class that generates service endpoint URIs and connection strings for every emulated Azure service. Use these instead of building strings by hand to ensure port numbers and hostname formats stay consistent with Topaz's DNS conventions.

| Method | Returns | Description |
|---|---|---|
| `GetKeyVaultEndpoint(string vaultName)` | `Uri` | Returns the data-plane URI for a Key Vault instance (e.g. `https://my-vault.vault.topaz.local.dev:8898`). Pass this to `SecretClient`, `KeyClient`, or `CertificateClient`. |
| `GetBlobServiceUri(string storageAccountName)` | `string` | Returns the Blob service endpoint URI (e.g. `https://myaccount.blob.storage.topaz.local.dev:8891/`). |
| `GetQueueServiceUri(string storageAccountName)` | `string` | Returns the Queue service endpoint URI. |
| `GetTableServiceUri(string storageAccountName)` | `string` | Returns the Table service endpoint URI. |
| `GetAzureStorageConnectionString(string storageAccountName, string accountKey)` | `string` | Returns a full `DefaultEndpointsProtocol=…` connection string covering all three Storage sub-services. Use this with `BlobServiceClient`, `QueueServiceClient`, and `TableServiceClient` constructors that accept a connection string. |
| `GetServiceBusConnectionString(string serviceBusNamespaceName)` | `string` | Returns an AMQP connection string for a Service Bus namespace. Sets `UseDevelopmentEmulator=true`. |
| `GetServiceBusConnectionStringWithTls(string serviceBusNamespaceName)` | `string` | Returns an AMQPS (port 5671) connection string — use this with MassTransit or any client that requires TLS without the development-emulator flag. |
| `GetServiceBusConnectionStringForManagement(string serviceBusNamespaceName)` | `string` | Returns a connection string for management-plane operations on a Service Bus namespace. |
| `GetEventHubConnectionString(string eventHubNamespaceName)` | `string` | Returns an AMQP connection string for an Event Hub namespace. Sets `UseDevelopmentEmulator=true`. |
| `GetContainerRegistryLoginServer(string registryName)` | `string` | Returns the `host:port` login server string for a Container Registry instance (e.g. `myregistry.cr.topaz.local.dev:8892`). Pass this to `docker login` or the ACR SDK. |

---

## Complete example

The following snippet shows a common test fixture pattern: create a credential, build an ARM client, provision a subscription and resource group, then obtain a Key Vault client using the endpoint helper.

```csharp
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.Resources;
using Azure.Security.KeyVault.Secrets;
using Topaz.Identity;
using Topaz.ResourceManager;

// ── Credentials ──────────────────────────────────────────────────────────────
var credential   = new AzureLocalCredential(Globals.GlobalAdminId);
var subscriptionId = Guid.Parse("00000000-0000-0000-0000-000000000001");

// ── Topaz management operations (subscription, not available in ARM SDK) ──────
await using var topazClient = new TopazArmClient(credential);
await topazClient.CreateSubscriptionAsync(subscriptionId, "dev-subscription");
if (!await topazClient.CheckIfReadyAsync())
    throw new InvalidOperationException("Topaz host is not reachable.");

// ── Standard Azure SDK ARM client ─────────────────────────────────────────────
var armClient    = new ArmClient(credential, subscriptionId.ToString(), TopazArmClientOptions.New);
var subscription = armClient.GetDefaultSubscription();

// Create a resource group
var rgData = new ResourceGroupData(AzureLocation.EastUS);
var rgLro  = await subscription.GetResourceGroups()
    .CreateOrUpdateAsync(WaitUntil.Completed, "rg-example", rgData);
ResourceGroupResource rg = rgLro.Value;

// Create a Key Vault
var kvProperties = new KeyVaultCreateOrUpdateContent(
    AzureLocation.EastUS,
    new KeyVaultProperties(
        tenantId: Guid.Empty,
        sku: new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard)));

await rg.GetKeyVaults().CreateOrUpdateAsync(WaitUntil.Completed, "kv-example", kvProperties);

// ── Data plane via TopazResourceHelpers ───────────────────────────────────────
var secretClient = new SecretClient(
    vaultUri:   TopazResourceHelpers.GetKeyVaultEndpoint("kv-example"),
    credential: credential,
    options:    new SecretClientOptions { DisableChallengeResourceVerification = true });

await secretClient.SetSecretAsync("db-password", "super-secret");
KeyVaultSecret secret = await secretClient.GetSecretAsync("db-password");
Console.WriteLine(secret.Value);
```

---

## See also

- [Testcontainers integration](/docs/ecosystem/testcontainers) — start Topaz automatically inside NUnit, xUnit, or MSTest fixtures
- [ASP.NET Core integration](/docs/ecosystem/aspnet-core) — provision Azure infrastructure at application startup using the `TopazEnvironmentBuilder` fluent API
- [ARM Template Deployments](/docs/ecosystem/arm-deployments) — supported resource types and deployment modes
