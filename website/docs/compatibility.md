---
sidebar_position: 5
description: Compatibility matrix for Topaz — tested Terraform providers, Azure SDKs, Azure CLI versions, Azure PowerShell setup, and runtime requirements.
keywords: [topaz compatibility, terraform azurerm version, azure sdk version, azure cli version, azure powershell, python sdk, topaz supported versions]
---

# Compatibility

This page lists the tool and SDK versions that Topaz is tested against in CI. Using versions outside these ranges may work, but is not verified.

## Terraform providers

| Provider | Source | Tested version | Notes |
|---|---|---|---|
| AzureRM | `hashicorp/azurerm` | `= 4.67.0` | Pinned exactly — other 4.x versions may work but are not tested |
| AzAPI | `azure/azapi` | `~> 2.0` | Any 2.x release |
| AzureAD | `hashicorp/azuread` | `~> 3.0` | Any 3.x release |

## Azure SDK for .NET

| Package | Tested version |
|---|---|
| `Azure.ResourceManager` | `1.14.0` |
| `Azure.Identity` | `1.18.0` |
| `Azure.ResourceManager.Storage` | `1.7.0` |
| `Azure.ResourceManager.KeyVault` | `1.4.0` |
| `Azure.ResourceManager.ServiceBus` | `1.1.0` |
| `Azure.ResourceManager.EventHubs` | `1.2.1` |
| `Azure.ResourceManager.ContainerRegistry` | `1.4.0` |
| `Azure.ResourceManager.ManagedServiceIdentities` | `1.4.1` |
| `Azure.ResourceManager.Network` | `1.15.0` |
| `Azure.ResourceManager.Resources` | `1.11.2` |
| `Azure.ResourceManager.Authorization` | `1.1.6` |
| `Azure.ResourceManager.Compute` | `1.14.0` |
| `Azure.ResourceManager.AppService` | `1.4.1` |
| `Azure.ResourceManager.CosmosDB` | `1.4.0` |
| `Azure.ResourceManager.Sql` | `1.4.0` |
| `Azure.Security.KeyVault.Certificates` | `4.8.0` |
| `Azure.Security.KeyVault.Keys` | `4.10.0` |
| `Azure.Security.KeyVault.Secrets` | `4.11.0` |
| `Azure.Storage.Blobs` | `12.28.0` |
| `Azure.Storage.Queues` | `12.26.0` |
| `Azure.Data.Tables` | `12.11.0` |
| `Azure.Messaging.ServiceBus` | `7.20.1` |
| `Azure.Messaging.EventHubs` | `5.12.2` |
| `Azure.Messaging.EventHubs.Processor` | `5.12.2` |
| `Azure.Containers.ContainerRegistry` | `1.3.0` |
| `Microsoft.Graph` | `5.103.0` |

## Runtime

| Component | Requirement |
|---|---|
| .NET | 10.0 |
| Docker base image | `mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled` |

## Azure CLI

Topaz Azure CLI compatibility is validated in CI with a version matrix:

| Tool | Tested versions | Notes |
|---|---|---|
| Azure CLI | `2.84.0`, `2.85.0`, `2.86.0` | Tested in `.github/workflows/ci-build-and-test.yml` (`cli-matrix` job) |

See [Azure CLI integration](./integrations/azure-cli-integration.md) for setup instructions.

## Azure PowerShell

Topaz Azure PowerShell tests run in the `topaz/powershell` Docker image built from `Topaz.Tests.AzurePowerShell/Dockerfile.powershell`.

| Component | Tested version / policy |
|---|---|
| PowerShell runtime | `7.4.7` |
| Base OS image | `ubuntu:22.04` |
| Az module installation policy | Installed from PSGallery at image build time (not pinned to exact versions) |

Preinstalled Az modules in the test image:

- `Az.Accounts`
- `Az.Resources`
- `Az.KeyVault`
- `Az.Storage`
- `Az.ContainerRegistry`
- `Az.Network`
- `Az.Websites`
- `Az.Sql`
- `Az.CosmosDB`

## Python SDK

Topaz Python compatibility is validated by `Topaz.Tests.Python` using a dedicated Docker image and the in-repo Topaz SDK source.

| Component | Tested version / constraint |
|---|---|
| Python runtime (test image) | `python:3.12-slim` |
| Topaz Python SDK package | `topaz-sdk` (`0.1.2`, installed from `sdk/python`) |
| SDK declared Python requirement | `>=3.10` |

Azure Python packages used by the test image:

| Package | Tested constraint |
|---|---|
| `azure-keyvault-secrets` | `>=4.8.0` |
| `azure-keyvault-keys` | `>=4.9.0` |
| `azure-keyvault-certificates` | `>=4.8.0` |
| `azure-mgmt-containerregistry` | `>=10.3.0` |
| `azure-storage-blob` | `>=12.22.0` |
| `azure-storage-queue` | `>=12.11.0` |
| `azure-data-tables` | `>=12.5.0` |
| `azure-servicebus` | `>=7.12.0` |
| `azure-mgmt-authorization` | `>=4.0.0` |
| `azure-mgmt-eventhub` | `>=11.0.0` |
| `azure-mgmt-msi` | `>=7.0.0` |
| `azure-mgmt-compute` | `>=33.0.0` |
| `azure-mgmt-network` | `>=26.0.0` |
| `azure-mgmt-web` | `>=7.3.0` |
| `azure-mgmt-resource` | `>=26.0.0` |
| `azure-mgmt-subscription` | `>=3.1.0` |

## Versioning policy

Topaz maintains the following support policies:

- **Topaz releases**: Supports the current release and the previous two major.minor versions (X-2). For example, if the current release is 1.6, versions 1.5 and 1.4 are supported.
- **Azure CLI**: Supports the 3 latest releases. See the [Azure CLI integration](./integrations/azure-cli-integration.md) page for the current list.
- **NuGet packages (.NET SDK)**: Supports the latest available version at the time of each Topaz release.
- **Terraform & Python**: No formal support policy is currently published. Compatibility is validated on a best-effort basis.

When a new Topaz release changes behaviour that affects a specific provider or SDK version, it will be noted in the release notes.
