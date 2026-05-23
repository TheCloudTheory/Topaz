"""
topaz-sdk — Python SDK for the Topaz Azure emulator.

Provides the three primitives needed to connect any Python application or test
suite to a locally running Topaz host:

  - ``AzureLocalCredential`` / ``ManagedIdentityLocalCredential``: drop-in
    ``TokenCredential`` implementations that generate locally-signed JWTs
    accepted by Topaz without requiring real Azure AD.

  - ``TopazArmClient``: thin HTTP client for management-plane operations not
    covered by ``azure-mgmt-resource`` (subscription create/delete, etc.).

  - ``TopazResourceHelpers``: static helpers that return the correct
    endpoint URLs and connection strings for every Topaz service.

  - ``TopazEnvironment``: fluent async context manager for spinning up a
    complete local Azure environment (subscription → resource group →
    Key Vault / Storage / Service Bus / …) in one expression.

Typical usage::

    from topaz_sdk import AzureLocalCredential, TopazResourceHelpers, GLOBAL_ADMIN_ID
    from azure.keyvault.secrets import SecretClient

    credential = AzureLocalCredential(GLOBAL_ADMIN_ID)
    client = SecretClient(
        vault_url=TopazResourceHelpers.get_key_vault_endpoint("my-vault"),
        credential=credential,
    )
    client.set_secret("my-secret", "hello")
"""

from topaz_sdk.identity import (
    AzureLocalCredential,
    ManagedIdentityLocalCredential,
    GLOBAL_ADMIN_ID,
)
from topaz_sdk.helpers import TopazResourceHelpers
from topaz_sdk.client import TopazArmClient
from topaz_sdk.environment import TopazEnvironment

__all__ = [
    "AzureLocalCredential",
    "ManagedIdentityLocalCredential",
    "GLOBAL_ADMIN_ID",
    "TopazResourceHelpers",
    "TopazArmClient",
    "TopazEnvironment",
]
