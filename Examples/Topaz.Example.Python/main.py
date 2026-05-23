"""
Topaz Python SDK example — create a subscription, resource group,
Key Vault (with secrets), and Table Storage (with an entity).

Prerequisites
-------------
1. Topaz host is running. The easiest way:

     docker run -d \\
       -p 8890:8890 -p 8891:8891 -p 8898:8898 -p 8899:8899 \\
       --name topaz.local.dev \\
       thecloudtheory/topaz-host:latest start

2. DNS resolution for *.topaz.local.dev subdomains is handled by dnsmasq.
   Run the Topaz install script for your OS to set it up:

     macOS:  curl -fsSL https://raw.githubusercontent.com/TheCloudTheory/Topaz/main/install/get-topaz.sh | bash
     Linux:  sudo curl -fsSL https://raw.githubusercontent.com/TheCloudTheory/Topaz/main/install/get-topaz.sh | bash

   The script installs and configures dnsmasq so that any subdomain under
   *.topaz.local.dev resolves to 127.0.0.1 dynamically — no manual /etc/hosts
   entries are needed or supported.

3. Trust the Topaz TLS certificate and point requests at it:
     export REQUESTS_CA_BUNDLE=/path/to/topaz.crt
   Full certificate setup guide: https://topaz.thecloudtheory.com/docs/intro/
"""

import uuid

from azure.data.tables import TableEntity, TableServiceClient
from azure.keyvault.secrets import SecretClient
from azure.mgmt.keyvault import KeyVaultManagementClient
from azure.mgmt.keyvault.models import Sku, VaultCreateOrUpdateParameters, VaultProperties
from azure.mgmt.resource import ResourceManagementClient
from azure.mgmt.resource.resources.models import ResourceGroup
from azure.mgmt.storage import StorageManagementClient
from azure.mgmt.storage.models import Kind
from azure.mgmt.storage.models import Sku as StorageSku
from azure.mgmt.storage.models import StorageAccountCreateParameters

from topaz_sdk import (
    GLOBAL_ADMIN_ID,
    DEFAULT_RESOURCE_MANAGER_PORT,
    AzureLocalCredential,
    TopazArmClient,
)

_BASE_URL = f"https://topaz.local.dev:{DEFAULT_RESOURCE_MANAGER_PORT}"
_CREDENTIAL_SCOPE = f"{_BASE_URL}/.default"


def _mgmt_kwargs(subscription_id: str) -> dict:
    """Shared keyword arguments for azure-mgmt-* clients pointed at Topaz."""
    return {
        "credential": AzureLocalCredential(GLOBAL_ADMIN_ID),
        "subscription_id": subscription_id,
        "base_url": _BASE_URL,
        "credential_scopes": [_CREDENTIAL_SCOPE],
    }


# ---------------------------------------------------------------------------
# Subscription + resource group
# ---------------------------------------------------------------------------


def reset_and_create_subscription(subscription_id: str, name: str) -> None:
    """Delete the subscription (if it exists) then re-create it for a clean slate."""
    with TopazArmClient(AzureLocalCredential(GLOBAL_ADMIN_ID)) as client:
        client.delete_subscription(subscription_id)
        client.create_subscription(subscription_id, name)
    print(f"Subscription [{subscription_id}, {name}] created successfully!")


def create_resource_group(subscription_id: str, name: str, location: str) -> None:
    client = ResourceManagementClient(**_mgmt_kwargs(subscription_id))
    client.resource_groups.create_or_update(name, ResourceGroup(location=location))
    print(f"Resource group [{name}] created successfully!")


# ---------------------------------------------------------------------------
# Key Vault — control plane + secrets
# ---------------------------------------------------------------------------


def create_key_vault(subscription_id: str, resource_group: str, vault_name: str, location: str) -> None:
    client = KeyVaultManagementClient(**_mgmt_kwargs(subscription_id))
    client.vaults.begin_create_or_update(
        resource_group,
        vault_name,
        VaultCreateOrUpdateParameters(
            location=location,
            properties=VaultProperties(
                tenant_id=str(uuid.UUID(int=0)),
                sku=Sku(family="A", name="standard"),
            ),
        ),
    ).result()
    print(f"Azure Key Vault [{vault_name}] created successfully!")


def create_key_vault_secrets(vault_name: str) -> None:
    vault_url = f"https://{vault_name}.vault.topaz.local.dev:8898"
    client = SecretClient(
        vault_url=vault_url,
        credential=AzureLocalCredential(GLOBAL_ADMIN_ID),
        # Required because Topaz uses a custom domain that differs from vault.azure.net
        verify_challenge_resource=False,
    )

    client.set_secret("db-password", "s3cr3t-password")
    client.set_secret("api-key", "my-api-key-value")
    client.set_secret("connection-string", "Server=localhost;Database=mydb;")

    secret = client.get_secret("db-password")
    print(f"Secret [{secret.name}] read back: {secret.value}")
    print("Azure Key Vault secrets created successfully!")


# ---------------------------------------------------------------------------
# Storage account + table
# ---------------------------------------------------------------------------


def create_storage_account(
    subscription_id: str, resource_group: str, account_name: str, location: str
) -> None:
    client = StorageManagementClient(**_mgmt_kwargs(subscription_id))
    client.storage_accounts.begin_create(
        resource_group,
        account_name,
        StorageAccountCreateParameters(
            sku=StorageSku(name="Standard_LRS"),
            kind=Kind.STORAGE_V2,
            location=location,
        ),
    ).result()
    print(f"Storage account [{account_name}] created successfully!")


def create_storage_table(
    subscription_id: str, resource_group: str, account_name: str
) -> None:
    storage_client = StorageManagementClient(**_mgmt_kwargs(subscription_id))
    keys = storage_client.storage_accounts.list_keys(resource_group, account_name)
    # In azure-mgmt-storage >= 21, StorageAccountListKeysResult is a MutableMapping;
    # the key list is accessed via dict-style indexing.
    account_key = keys["keys"][0]["value"]

    connection_string = (
        f"DefaultEndpointsProtocol=https;"
        f"AccountName={account_name};"
        f"AccountKey={account_key};"
        f"TableEndpoint=https://{account_name}.table.storage.topaz.local.dev:8890;"
    )

    table_service = TableServiceClient.from_connection_string(connection_string)
    table_service.create_table_if_not_exists("employees")

    entity: TableEntity = {
        "PartitionKey": "Engineering",
        "RowKey": uuid.uuid4().hex,  # hex gives a hyphen-free UUID string
        "Name": "Alice",
        "Role": "Developer",
        "YearsExperience": 5,
    }
    table_service.get_table_client("employees").create_entity(entity)
    print("Table [employees] and entity created successfully!")


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------


def main() -> None:
    print("Topaz Example — Python\n")

    # Fixed IDs so re-runs start from a clean state (existing subscription is
    # deleted first, which cascades and removes all resources inside it).
    subscription_id = "e0000001-0000-0000-0000-000000000001"
    subscription_name = "topaz.example.python"
    resource_group_name = "rg-topaz-example"
    vault_name = "kvtopazexample"
    storage_account_name = "storagetopazexample"
    location = "westeurope"

    reset_and_create_subscription(subscription_id, subscription_name)
    create_resource_group(subscription_id, resource_group_name, location)
    create_key_vault(subscription_id, resource_group_name, vault_name, location)
    create_key_vault_secrets(vault_name)
    create_storage_account(subscription_id, resource_group_name, storage_account_name, location)
    create_storage_table(subscription_id, resource_group_name, storage_account_name)

    print("\nAll resources created successfully!")


if __name__ == "__main__":
    main()
