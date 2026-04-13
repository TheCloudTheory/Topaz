---
sidebar_position: 4
description: Compatibility matrix for Topaz — tested Terraform provider versions, Azure SDK package versions, and .NET runtime requirements.
keywords: [topaz compatibility, terraform azurerm version, azure sdk version, topaz supported versions, azurerm provider version]
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
| `Azure.ResourceManager.Storage` | `1.4.2` |
| `Azure.ResourceManager.KeyVault` | `1.3.2` |
| `Azure.ResourceManager.ServiceBus` | `1.1.0` |
| `Azure.ResourceManager.EventHubs` | `1.1.0` |
| `Azure.ResourceManager.ContainerRegistry` | `1.2.0` |
| `Azure.ResourceManager.ManagedServiceIdentities` | `1.4.0` |
| `Azure.ResourceManager.Network` | `1.13.0` |
| `Azure.ResourceManager.Resources` | `1.11.2` |
| `Azure.ResourceManager.Authorization` | `1.1.6` |
| `Azure.Security.KeyVault.Secrets` | `4.7.0` |
| `Azure.Storage.Blobs` | `12.24.0` |
| `Azure.Data.Tables` | `12.11.0` |
| `Azure.Messaging.ServiceBus` | `7.20.1` |
| `Azure.Messaging.EventHubs` | `5.12.1` |
| `Azure.Messaging.EventHubs.Processor` | `5.12.1` |
| `Azure.Containers.ContainerRegistry` | `1.3.0` |

## Runtime

| Component | Requirement |
|---|---|
| .NET | 8.0 |
| Docker base image | `mcr.microsoft.com/dotnet/runtime-deps:8.0-noble-chiseled` |

## Azure CLI

The Azure CLI is supported via the Topaz cloud registration flow. No specific CLI version is pinned in CI — use the latest stable release. See [Azure CLI integration](./integrations/azure-cli-integration.md) for setup instructions.

## Versioning policy

Topaz is in active development and does not yet publish a formal compatibility table across releases. The versions listed above reflect the current CI test suite. When a new Topaz release changes behaviour that affects a specific provider or SDK version, it will be noted in the release notes.
