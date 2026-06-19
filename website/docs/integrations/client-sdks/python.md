---
sidebar_position: 2
slug: /client-sdks/python
description: Connect Python applications and test projects to Topaz using the topaz-sdk package — credentials, ARM client, and endpoint helpers.
keywords: [topaz python sdk, topaz pip, topaz python credential, azure sdk local python, topaz python testing]
---

# How to connect Python apps to Topaz

This guide shows you how to install and use the `topaz-sdk` package to connect Python applications and test projects to the Topaz emulator.

| Symbol | Purpose |
|---|---|
| [`AzureLocalCredential`](#azurelocalcredential) | `TokenCredential` implementation that generates tokens accepted by Topaz |
| [`ManagedIdentityLocalCredential`](#managedidentitylocalcredential) | `TokenCredential` that emulates a managed identity principal |
| [`GLOBAL_ADMIN_ID`](#global_admin_id) | The built-in superadmin object ID constant |
| [`TopazArmClient`](#topazarmclient) | HTTP client for Topaz-specific management operations |
| [`TopazResourceHelpers`](#topazresourcehelpers) | Static helpers that return endpoint URIs and connection strings |
| [Port constants](#port-constants) | Named constants for every Topaz service port |

---

## Installation

```bash
pip install topaz-sdk
```

---

## `AzureLocalCredential`

The primary `TokenCredential` for use with any Azure SDK client that calls Topaz. It generates a short-lived JWT locally — no network call is made — that Topaz's auth layer accepts as a valid Entra ID token.

Equivalent to `Topaz.Identity.AzureLocalCredential` in C#.

```python
from topaz_sdk import AzureLocalCredential, GLOBAL_ADMIN_ID

credential = AzureLocalCredential(GLOBAL_ADMIN_ID)
```

**Constructor**

```python
AzureLocalCredential(object_id: str, preferred_username: str | None = None)
```

| Parameter | Type | Description |
|---|---|---|
| `object_id` | `str` | The principal object ID embedded in the token. Use `GLOBAL_ADMIN_ID` (`"00000000-0000-0000-0000-000000000000"`) to bypass RBAC checks, or a specific UUID to represent a named principal. |
| `preferred_username` | `str \| None` | Optional UPN injected into the token as the `preferred_username` claim. Useful for tests that inspect the caller's identity. |

This credential is compatible with every Azure SDK client that accepts a `TokenCredential` — `SecretClient`, `BlobServiceClient`, `QueueServiceClient`, `TableServiceClient`, and the `azure-mgmt-*` management clients.

```python
from azure.mgmt.resource import ResourceManagementClient
from topaz_sdk import AzureLocalCredential, GLOBAL_ADMIN_ID, DEFAULT_RESOURCE_MANAGER_PORT

base_url = f"https://topaz.local.dev:{DEFAULT_RESOURCE_MANAGER_PORT}"
credential = AzureLocalCredential(GLOBAL_ADMIN_ID)

client = ResourceManagementClient(
    credential=credential,
    subscription_id="<subscription-id>",
    base_url=base_url,
    credential_scopes=[base_url + "/.default"],
)
```

---

## `ManagedIdentityLocalCredential`

A `TokenCredential` that emulates a call made by a user-assigned or system-assigned managed identity. Use this when your production code authenticates via `ManagedIdentityCredential` and you want to exercise that path locally.

Equivalent to `Topaz.Identity.ManagedIdentityLocalCredential` in C#.

```python
from topaz_sdk import ManagedIdentityLocalCredential

credential = ManagedIdentityLocalCredential(principal_id="<managed-identity-principal-id>")
```

**Constructor**

```python
ManagedIdentityLocalCredential(principal_id: str)
```

| Parameter | Type | Description |
|---|---|---|
| `principal_id` | `str` | UUID of the managed identity resource created in Topaz. |

---

## `GLOBAL_ADMIN_ID`

```python
GLOBAL_ADMIN_ID: str = "00000000-0000-0000-0000-000000000000"
```

The built-in superadmin object ID. Pass it to `AzureLocalCredential` to act as a principal with full access to all resources, bypassing all RBAC checks. Use this in test fixtures where you need unrestricted access.

---

## `TopazArmClient`

A thin HTTP client for Topaz-specific management operations not covered by `azure-mgmt-resource` — primarily subscription lifecycle, management group hierarchy, and scoped role assignments needed to set up test fixtures.

Equivalent to `Topaz.ResourceManager.TopazArmClient` in C#.

SSL verification is controlled automatically via the `REQUESTS_CA_BUNDLE` environment variable. Set it to the path of the Topaz TLS certificate before running your scripts or tests.

```python
from topaz_sdk import AzureLocalCredential, GLOBAL_ADMIN_ID, TopazArmClient

with TopazArmClient(AzureLocalCredential(GLOBAL_ADMIN_ID)) as client:
    client.create_subscription("e0000001-0000-0000-0000-000000000001", "dev-subscription")
```

**Constructor**

```python
TopazArmClient(credential: AzureLocalCredential)
```

`TopazArmClient` implements the context-manager protocol (`__enter__` / `__exit__`) and closes the underlying `requests.Session` on exit. Use it with `with` statements or call `.close()` manually in test teardown.

### Subscriptions

| Method | Returns | Description |
|---|---|---|
| `create_subscription(subscription_id, subscription_name)` | `None` | Creates a subscription in Topaz. |
| `delete_subscription(subscription_id)` | `None` | Deletes a subscription. Silently succeeds if not found. |
| `update_subscription(subscription_id, subscription_name, tags)` | `None` | Updates the display name and/or tags of an existing subscription. |
| `cancel_subscription(subscription_id)` | `None` | Cancels (disables) a subscription. |
| `enable_subscription(subscription_id)` | `None` | Re-enables a previously cancelled subscription. |

### Resource Groups

| Method | Returns | Description |
|---|---|---|
| `create_resource_group(subscription_id, resource_group_name, location)` | `None` | Creates or updates a resource group. `location` defaults to `"westeurope"`. |
| `delete_resource_group(subscription_id, resource_group_name)` | `None` | Deletes a resource group. Silently succeeds if not found. |

### Key Vault

| Method | Returns | Description |
|---|---|---|
| `purge_key_vault(subscription_id, key_vault_name, location)` | `None` | Purges a soft-deleted Key Vault so a vault with the same name can be re-created. `location` defaults to `"westeurope"`. |

### Management Groups

| Method | Returns | Description |
|---|---|---|
| `create_management_group(group_id, display_name)` | `None` | Creates a management group at the tenant root. |
| `create_management_group_with_parent(group_id, display_name, parent_id)` | `None` | Creates a management group nested under an existing parent group. |
| `get_management_group(group_id)` | `dict` | Returns the management group resource as a dict. |
| `get_descendants(group_id)` | `dict` | Returns the list of descendants (child groups and subscriptions) of a management group. |
| `get_entities()` | `dict` | Returns all entities (management groups and subscriptions) visible to the caller. Follows `@nextLink` pagination automatically. |
| `associate_subscription_with_management_group(group_id, subscription_id)` | `dict` | Associates a subscription with a management group. |
| `disassociate_subscription_from_management_group(group_id, subscription_id)` | `None` | Removes the association between a subscription and a management group. |
| `get_subscription_under_management_group(group_id, subscription_id)` | `dict` | Returns the subscription resource as seen from a management group scope. |

### Hierarchy Settings

| Method | Returns | Description |
|---|---|---|
| `create_or_update_hierarchy_settings(group_id, require_authorization_for_group_creation, default_management_group)` | `dict` | Creates or fully replaces hierarchy settings for a management group. |
| `update_hierarchy_settings(group_id, require_authorization_for_group_creation, default_management_group)` | `dict` | Partially updates hierarchy settings (PATCH semantics). |
| `get_hierarchy_settings(group_id)` | `dict` | Returns current hierarchy settings for a management group. |
| `list_hierarchy_settings(group_id)` | `dict` | Returns all hierarchy settings objects for a management group. |
| `delete_hierarchy_settings(group_id)` | `None` | Deletes hierarchy settings for a management group. |

### Role Assignments

| Method | Returns | Description |
|---|---|---|
| `create_management_group_role_assignment(management_group_id, assignment_name, principal_id, role_definition_id)` | `None` | Creates a role assignment at management-group scope. |
| `create_resource_group_role_assignment(subscription_id, resource_group_name, assignment_name, principal_id, role_definition_id)` | `None` | Creates a role assignment scoped to a resource group. |
| `create_resource_role_assignment(subscription_id, resource_group_name, provider_namespace, resource_type, resource_name, assignment_name, principal_id, role_definition_id)` | `None` | Creates a role assignment scoped to a single resource. |

### Health

| Method | Returns | Description |
|---|---|---|
| `check_ready()` | `bool` | Returns `True` when the Topaz host is running and responding. Uses the unauthenticated `GET /health` endpoint. Useful for readiness polling in test fixtures. |

---

## `TopazResourceHelpers`

A static class that generates service endpoint URIs and connection strings for every emulated Azure service. Use these instead of building strings by hand to ensure port numbers and hostname formats stay consistent with Topaz's DNS conventions.

```python
from topaz_sdk import TopazResourceHelpers

vault_url = TopazResourceHelpers.get_key_vault_endpoint("my-vault")
conn_str  = TopazResourceHelpers.get_storage_connection_string("myaccount", account_key)
```

| Method | Returns | Description |
|---|---|---|
| `get_key_vault_endpoint(vault_name)` | `str` | Returns the data-plane URI for a Key Vault (e.g. `https://my-vault.vault.topaz.local.dev:8898`). Pass to `SecretClient`, `KeyClient`, or `CertificateClient`. |
| `get_blob_service_uri(storage_account_name)` | `str` | Returns the Blob service HTTPS URI (e.g. `https://myaccount.blob.storage.topaz.local.dev:8891/`). |
| `get_queue_service_uri(storage_account_name)` | `str` | Returns the Queue service HTTPS URI. |
| `get_table_service_uri(storage_account_name)` | `str` | Returns the Table service HTTPS URI. |
| `get_storage_connection_string(storage_account_name, account_key)` | `str` | Returns a full `DefaultEndpointsProtocol=…` connection string covering Blob, Queue, and Table Storage. Pass to `BlobServiceClient`, `QueueServiceClient`, or `TableServiceClient` constructors that accept a connection string. |
| `get_service_bus_connection_string(namespace_name)` | `str` | Returns an AMQP connection string for a Service Bus namespace (`UseDevelopmentEmulator=true`). |
| `get_service_bus_connection_string_with_tls(namespace_name)` | `str` | Returns an AMQPS (port 5671) connection string — use with MassTransit or clients that require TLS. |
| `get_service_bus_connection_string_for_management(namespace_name)` | `str` | Returns a connection string for Service Bus management-plane operations. |
| `get_event_hub_connection_string(namespace_name)` | `str` | Returns an AMQP connection string for an Event Hub namespace (`UseDevelopmentEmulator=true`). |
| `get_container_registry_login_server(registry_name)` | `str` | Returns the `host:port` login server string for a Container Registry instance (e.g. `myregistry.cr.topaz.local.dev:8892`). |
| `get_web_site_default_host_name(site_name)` | `str` | Returns the default hostname for an App Service site. |

---

## Port constants

All port constants are importable directly from `topaz_sdk`.

| Constant | Port | Service |
|---|---|---|
| `DEFAULT_RESOURCE_MANAGER_PORT` | `8899` | ARM / Resource Manager |
| `DEFAULT_KEY_VAULT_PORT` | `8898` | Key Vault |
| `DEFAULT_BLOB_STORAGE_PORT` | `8891` | Blob Storage |
| `DEFAULT_TABLE_STORAGE_PORT` | `8890` | Table Storage |
| `DEFAULT_QUEUE_STORAGE_PORT` | `8893` | Queue Storage |
| `DEFAULT_FILE_STORAGE_PORT` | `8894` | File Storage |
| `DEFAULT_EVENT_HUB_PORT` | `8897` | Event Hub (HTTP) |
| `DEFAULT_EVENT_HUB_AMQP_PORT` | `8888` | Event Hub (AMQP) |
| `DEFAULT_SERVICE_BUS_AMQP_PORT` | `8889` | Service Bus (AMQP) |
| `ADDITIONAL_SERVICE_BUS_PORT` | `8887` | Service Bus (management) |
| `AMQP_TLS_CONNECTION_PORT` | `5671` | AMQP over TLS |
| `CONTAINER_REGISTRY_PORT` | `8892` | Container Registry |

---

## Complete example

The following snippet shows a common test fixture pattern: create a credential, provision a subscription and resource group, then work with Key Vault secrets and Table Storage.

```python
import os
import uuid

from azure.data.tables import TableServiceClient
from azure.keyvault.secrets import SecretClient
from azure.mgmt.keyvault import KeyVaultManagementClient
from azure.mgmt.keyvault.models import Sku, VaultCreateOrUpdateParameters, VaultProperties
from azure.mgmt.resource import ResourceManagementClient
from azure.mgmt.resource.resources.models import ResourceGroup
from azure.mgmt.storage import StorageManagementClient
from azure.mgmt.storage.models import Kind, StorageAccountCreateParameters
from azure.mgmt.storage.models import Sku as StorageSku

from topaz_sdk import (
    GLOBAL_ADMIN_ID,
    DEFAULT_RESOURCE_MANAGER_PORT,
    AzureLocalCredential,
    TopazArmClient,
    TopazResourceHelpers,
)

SUBSCRIPTION_ID = "e0000001-0000-0000-0000-000000000001"
BASE_URL = f"https://topaz.local.dev:{DEFAULT_RESOURCE_MANAGER_PORT}"
CREDENTIAL_SCOPE = f"{BASE_URL}/.default"

def mgmt_kwargs(subscription_id: str) -> dict:
    return {
        "credential": AzureLocalCredential(GLOBAL_ADMIN_ID),
        "subscription_id": subscription_id,
        "base_url": BASE_URL,
        "credential_scopes": [CREDENTIAL_SCOPE],
    }

# ── Subscription (Topaz-specific management client) ───────────────────────────
with TopazArmClient(AzureLocalCredential(GLOBAL_ADMIN_ID)) as client:
    client.delete_subscription(SUBSCRIPTION_ID)   # clean slate on re-runs
    client.create_subscription(SUBSCRIPTION_ID, "dev-subscription")

# ── Resource group (standard azure-mgmt-resource client) ─────────────────────
rm = ResourceManagementClient(**mgmt_kwargs(SUBSCRIPTION_ID))
rm.resource_groups.create_or_update("rg-example", ResourceGroup(location="westeurope"))

# ── Key Vault (control plane + data plane) ───────────────────────────────────
kv_mgmt = KeyVaultManagementClient(**mgmt_kwargs(SUBSCRIPTION_ID))
kv_mgmt.vaults.begin_create_or_update(
    "rg-example", "my-vault",
    VaultCreateOrUpdateParameters(
        location="westeurope",
        properties=VaultProperties(
            tenant_id=str(uuid.UUID(int=0)),
            sku=Sku(family="A", name="standard"),
        ),
    ),
).result()

secret_client = SecretClient(
    vault_url=TopazResourceHelpers.get_key_vault_endpoint("my-vault"),
    credential=AzureLocalCredential(GLOBAL_ADMIN_ID),
    verify_challenge_resource=False,  # required for Topaz custom domain
)
secret_client.set_secret("db-password", "s3cr3t")
print(secret_client.get_secret("db-password").value)  # → s3cr3t

# ── Storage account + Table Storage ──────────────────────────────────────────
st_mgmt = StorageManagementClient(**mgmt_kwargs(SUBSCRIPTION_ID))
st_mgmt.storage_accounts.begin_create(
    "rg-example", "myaccount",
    StorageAccountCreateParameters(
        sku=StorageSku(name="Standard_LRS"),
        kind=Kind.STORAGE_V2,
        location="westeurope",
    ),
).result()

keys = st_mgmt.storage_accounts.list_keys("rg-example", "myaccount")
# azure-mgmt-storage >= 21: StorageAccountListKeysResult is a MutableMapping
account_key = keys["keys"][0]["value"]

table_service = TableServiceClient.from_connection_string(
    TopazResourceHelpers.get_storage_connection_string("myaccount", account_key)
)
table_service.create_table_if_not_exists("employees")
table_service.get_table_client("employees").create_entity({
    "PartitionKey": "Engineering",
    "RowKey": uuid.uuid4().hex,  # hex avoids hyphens, which Topaz's regex rejects
    "Name": "Alice",
})
```

:::tip[TLS certificate]

Set `REQUESTS_CA_BUNDLE` to the path of the Topaz TLS certificate before running your script or test suite:

```bash
export REQUESTS_CA_BUNDLE=/path/to/topaz.crt
python3 my_script.py
```

Full certificate setup guide: https://topaz.thecloudtheory.com/docs/intro/

:::
