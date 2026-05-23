# topaz-sdk

Python SDK for [Topaz](https://topaz.thecloudtheory.com/) — the single-binary Azure emulator.

[![PyPI](https://img.shields.io/pypi/v/topaz-sdk)](https://pypi.org/project/topaz-sdk/)
[![License](https://img.shields.io/pypi/l/topaz-sdk)](https://github.com/TheCloudTheory/Topaz/blob/main/LICENSE)
[![Docs](https://img.shields.io/badge/docs-topaz.thecloudtheory.com-blue)](https://topaz.thecloudtheory.com/)

## What is Topaz?

Topaz is a single-binary local Azure emulator. Instead of running Azurite for Storage, a separate emulator for Service Bus, and another for Key Vault — you run one tool that covers the control and data planes of many Azure services, emulates ARM deployments, and implements Azure RBAC, all with no Azure subscription required.

## Installation

```bash
pip install topaz-sdk
```

Requires Python 3.10 or later.

## Quick start

```python
from topaz_sdk import AzureLocalCredential, TopazArmClient, GLOBAL_ADMIN_ID

# Authenticate as the built-in admin user
credential = AzureLocalCredential(GLOBAL_ADMIN_ID)
client = TopazArmClient(credential)

# Create a subscription and resource group
client.create_subscription("my-subscription", "00000000-0000-0000-0000-000000000001")
client.create_resource_group(
    "00000000-0000-0000-0000-000000000001",
    "my-resource-group",
    "westeurope",
)
```

## Using standard Azure SDK clients

`topaz-sdk` provides credentials and helpers that work directly with the standard `azure-mgmt-*` packages:

```python
from azure.mgmt.keyvault import KeyVaultManagementClient
from topaz_sdk import AzureLocalCredential, GLOBAL_ADMIN_ID, DEFAULT_RESOURCE_MANAGER_PORT

credential = AzureLocalCredential(GLOBAL_ADMIN_ID)

client = KeyVaultManagementClient(
    credential=credential,
    subscription_id="00000000-0000-0000-0000-000000000001",
    base_url=f"https://topaz.local.dev:{DEFAULT_RESOURCE_MANAGER_PORT}",
    credential_scopes=["https://topaz.local.dev/.default"],
)
```

Point any `azure-mgmt-*` client at Topaz by overriding `base_url` and `credential_scopes`. No application code changes are required.

## Provided classes

| Symbol | Description |
|---|---|
| `AzureLocalCredential` | `TokenCredential` implementation for Topaz's built-in identity endpoint |
| `TopazArmClient` | Thin HTTP client for management-plane operations not covered by `azure-mgmt-resource` (subscription lifecycle, resource groups, management groups, role assignments) |
| `GLOBAL_ADMIN_ID` | Constant for the built-in admin user ID (`00000000-0000-0000-0000-000000000000`) |
| `DEFAULT_RESOURCE_MANAGER_PORT` | Default ARM port (`8899`) |

## Requirements

The SDK expects a running Topaz host. See the [Topaz quickstart](https://topaz.thecloudtheory.com/docs/intro/) for installation instructions.

SSL verification uses the `REQUESTS_CA_BUNDLE` environment variable. When running inside the Topaz test fixture container this is set automatically.

## Links

- **Docs:** [topaz.thecloudtheory.com](https://topaz.thecloudtheory.com/)
- **Source:** [github.com/TheCloudTheory/Topaz](https://github.com/TheCloudTheory/Topaz)
- **Issues:** [github.com/TheCloudTheory/Topaz/issues](https://github.com/TheCloudTheory/Topaz/issues)
- **Discord:** [discord.gg/eGTkS76w](https://discord.gg/eGTkS76w)
