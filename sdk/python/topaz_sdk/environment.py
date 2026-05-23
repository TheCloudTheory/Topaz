"""
Fluent async builder for setting up a local Azure environment in Topaz.

Python port of ``Topaz.AspNetCore.Extensions.TopazConfigurationExtensions``.
Use ``TopazEnvironment`` as an async context manager to create subscriptions,
resource groups, Key Vaults, Storage Accounts, and Service Bus namespaces in
a single chained expression.

Example::

    from topaz_sdk import TopazEnvironment, AzureLocalCredential, GLOBAL_ADMIN_ID

    credential = AzureLocalCredential(GLOBAL_ADMIN_ID)
    subscription_id = "00000000-0000-0000-0000-000000000001"

    async with TopazEnvironment(credential) as env:
        await env.create_subscription(subscription_id, "my-sub")
        await env.create_resource_group(subscription_id, "my-rg")
        await env.create_key_vault(subscription_id, "my-rg", "my-kv")
"""

from __future__ import annotations

from typing import Any

from topaz_sdk.client import TopazArmClient
from topaz_sdk.helpers import (
    DEFAULT_RESOURCE_MANAGER_PORT,
    DEFAULT_KEY_VAULT_PORT,
    DEFAULT_BLOB_STORAGE_PORT,
    DEFAULT_QUEUE_STORAGE_PORT,
    DEFAULT_TABLE_STORAGE_PORT,
)
from topaz_sdk.identity import AzureLocalCredential


class TopazEnvironment:
    """
    Async context manager / fluent builder for creating a local Azure topology.

    All ``create_*`` methods are idempotent: they attempt to delete the resource
    first (ignoring 404) before re-creating it, mirroring the ``[SetUp]``
    pattern used in the .NET E2E tests.

    Args:
        credential: Credential used for all management-plane calls.
    """

    def __init__(self, credential: AzureLocalCredential) -> None:
        self._credential = credential
        self._client = TopazArmClient(credential)

    async def create_subscription(
        self, subscription_id: str, subscription_name: str
    ) -> "TopazEnvironment":
        """Deletes (if present) and re-creates a subscription."""
        self._client.delete_subscription(subscription_id)
        self._client.create_subscription(subscription_id, subscription_name)
        return self

    async def create_resource_group(
        self,
        subscription_id: str,
        resource_group_name: str,
        location: str = "westeurope",
    ) -> "TopazEnvironment":
        """Deletes (if present) and re-creates a resource group."""
        self._client.delete_resource_group(subscription_id, resource_group_name)
        self._client.create_resource_group(subscription_id, resource_group_name, location)
        return self

    async def create_key_vault(
        self,
        subscription_id: str,
        resource_group_name: str,
        vault_name: str,
        location: str = "westeurope",
    ) -> "TopazEnvironment":
        """Creates a Key Vault via the ARM management client."""
        from azure.mgmt.keyvault import KeyVaultManagementClient
        from azure.mgmt.keyvault.models import (
            VaultCreateOrUpdateParameters,
            VaultProperties,
            Sku,
            SkuName,
            SkuFamily,
        )

        client = KeyVaultManagementClient(
            credential=self._credential,
            subscription_id=subscription_id,
            base_url=f"https://topaz.local.dev:{DEFAULT_RESOURCE_MANAGER_PORT}",
            credential_scopes=[f"https://topaz.local.dev:{DEFAULT_RESOURCE_MANAGER_PORT}/.default"],
        )
        params = VaultCreateOrUpdateParameters(
            location=location,
            properties=VaultProperties(
                tenant_id="50717675-3E5E-4A1E-8CB5-C62D8BE8CA48",
                sku=Sku(family=SkuFamily.A, name=SkuName.STANDARD),
                enable_soft_delete=True,
            ),
        )
        client.vaults.begin_create_or_update(
            resource_group_name, vault_name, params
        ).result()
        return self

    async def create_storage_account(
        self,
        subscription_id: str,
        resource_group_name: str,
        account_name: str,
        location: str = "westeurope",
    ) -> "TopazEnvironment":
        """Creates a Storage Account via the ARM management client."""
        from azure.mgmt.storage import StorageManagementClient
        from azure.mgmt.storage.models import (
            StorageAccountCreateParameters,
            Sku,
            SkuName,
            Kind,
        )

        client = StorageManagementClient(
            credential=self._credential,
            subscription_id=subscription_id,
            base_url=f"https://topaz.local.dev:{DEFAULT_RESOURCE_MANAGER_PORT}",
            credential_scopes=[f"https://topaz.local.dev:{DEFAULT_RESOURCE_MANAGER_PORT}/.default"],
        )
        client.storage_accounts.begin_create(
            resource_group_name,
            account_name,
            StorageAccountCreateParameters(
                sku=Sku(name=SkuName.STANDARD_LRS),
                kind=Kind.STORAGE_V2,
                location=location,
            ),
        ).result()
        return self

    async def create_service_bus_namespace(
        self,
        subscription_id: str,
        resource_group_name: str,
        namespace_name: str,
        location: str = "westeurope",
    ) -> "TopazEnvironment":
        """Creates a Service Bus namespace via the ARM management client."""
        from azure.mgmt.servicebus import ServiceBusManagementClient
        from azure.mgmt.servicebus.models import SBNamespace, SBSku, SkuName, SkuTier

        client = ServiceBusManagementClient(
            credential=self._credential,
            subscription_id=subscription_id,
            base_url=f"https://topaz.local.dev:{DEFAULT_RESOURCE_MANAGER_PORT}",
            credential_scopes=[f"https://topaz.local.dev:{DEFAULT_RESOURCE_MANAGER_PORT}/.default"],
        )
        client.namespaces.begin_create_or_update(
            resource_group_name,
            namespace_name,
            SBNamespace(
                location=location,
                sku=SBSku(name=SkuName.STANDARD, tier=SkuTier.STANDARD),
            ),
        ).result()
        return self

    # ------------------------------------------------------------------
    # Context-manager support
    # ------------------------------------------------------------------

    async def __aenter__(self) -> "TopazEnvironment":
        return self

    async def __aexit__(self, *args: Any) -> None:
        self._client.__exit__()

    def __enter__(self) -> "TopazEnvironment":
        return self

    def __exit__(self, *args: Any) -> None:
        self._client.__exit__(*args)
