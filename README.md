# Topaz ![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/TheCloudTheory/Topaz/ci-build-and-test.yml) ![GitHub Release](https://img.shields.io/github/v/release/TheCloudTheory/Topaz?include_prereleases)

<div align="center">
  <img src="./static/topaz-logo.png" />

  <b>One binary. Multiple Azure services. No cloud required.</b>
</div>

## What is Topaz?

Topaz is a single-binary Azure emulator. Instead of running Azurite for Storage, a separate emulator for Service Bus, and another for Key Vault — you run one tool.

It supports both the control and data planes of Azure services, emulates ARM deployments with Bicep and ARM Templates, and implements Azure RBAC, all locally with no Azure subscription required. Teams use it to cut cloud costs, speed up CI pipelines, and develop entirely offline.

Check the [documentation](https://topaz.thecloudtheory.com/) for guides, recipes, and a full list of supported services.

## Why Topaz?

Most Azure emulators cover a single service. Topaz covers the full stack:

* **One tool** — no more juggling multiple emulators per service
* **Control & data plane** — not just data operations, but full resource management
* **ARM / Bicep deployments** — deploy templates locally the same way you would in Azure
* **Azure RBAC emulation** — role assignments and permission checks work locally
* **Microsoft Entra ID tenant emulation** — identity flows without a real tenant
* **Azure resource hierarchy** — subscriptions, resource groups, and resource IDs behave as expected
* **Seamless Azure SDK integration** — no code changes; point your SDK at Topaz
* **Full portability** — single executable or Docker container, runs anywhere

Coming up: a UI for resource management and chaos testing support.

## Supported services

| Service | Control Plane | Data Plane |
|---|---|---|
| Azure Storage (Blob) | ✅ | ✅ |
| Azure Key Vault | ✅ | ✅ |
| Azure Service Bus | ✅ | ✅ |
| Azure Container Registry | ✅ | ✅ |
| Azure Event Hub | ✅ | ✅ |
| Azure Resource Manager | ✅ | — |
| Microsoft Entra ID | ✅ | — |

See the [API coverage docs](https://topaz.thecloudtheory.com/docs/api-coverage/) for the full operation-level breakdown.

## Getting started

```bash
# Run with Docker
docker run -p 8891:8891 -p 8892:8892 -p 8898:8898 thecloudtheory/topaz-cli start

# Or download the binary and run directly
topaz start
```

Point your Azure SDK at the relevant local port — no code changes required. See the [documentation](https://topaz.thecloudtheory.com/) for connection strings and SDK setup.

## Terraform integration

Topaz supports local Terraform workflows using the standard AzureRM provider. Configure `metadata_host` to point at Topaz and disable provider auto-registration:

```hcl
provider "azurerm" {
  features {}
  metadata_host = "topaz.local.dev:8899"
  resource_provider_registrations = "none"
}
```

Full setup and troubleshooting guide: [Terraform integration](https://topaz.thecloudtheory.com/docs/terraform-integration).

## Licensing

Topaz is open-source. A commercial license with enterprise support is planned for teams that need SLAs, priority fixes, or long-term stability guarantees. Existing users will receive advance notice well before any licensing changes take effect.

## Alternatives

If you need emulation for a single Azure service, these official Microsoft tools may be sufficient:

* [Azurite](https://github.com/Azure/Azurite) — Azure Storage only
* [Azure Cosmos DB Emulator](https://github.com/Azure/azure-cosmos-db-emulator-docker) — Cosmos DB only
* [Azure Service Bus Emulator](https://github.com/Azure/azure-service-bus-emulator-installer) — Service Bus only

If you need multiple services, RBAC, or ARM deployments locally, that's where Topaz fits.

## Responsible AI

Topaz is developed with assistance from AI tools (GitHub Copilot, JetBrains AI). Their use is limited to: generating models and DTOs, extracting built-in role definitions, explaining methods, debugging, and writing boilerplate. Conceptual design, core logic, and architecture are done manually.
