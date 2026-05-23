"""
Key Vault secret E2E tests — Python port of Topaz.Tests/E2E/KeyVaultTests.cs.

Uses azure-keyvault-secrets against the locally running Topaz Key Vault emulator.
Each test reuses the vault created in the module-scoped setup fixture.
"""

import pytest
from azure.core.exceptions import ResourceNotFoundError
from azure.keyvault.secrets import SecretClient

from topaz_sdk import AzureLocalCredential, TopazArmClient, TopazResourceHelpers, GLOBAL_ADMIN_ID

# ---------------------------------------------------------------------------
# Module-level constants (distinct GUIDs from the .NET E2E suite)
# ---------------------------------------------------------------------------

_SUBSCRIPTION_ID = "a0000001-0000-0000-0000-000000000001"
_SUBSCRIPTION_NAME = "py-sub-kv-test"
_RESOURCE_GROUP = "py-rg-kv-test"
_VAULT_NAME = "pytest-kv"

_VAULT_URL = TopazResourceHelpers.get_key_vault_endpoint(_VAULT_NAME)


# ---------------------------------------------------------------------------
# Module-scoped setup: create subscription, RG, and Key Vault once
# ---------------------------------------------------------------------------

@pytest.fixture(scope="module", autouse=True)
def kv_environment():
    credential = AzureLocalCredential(GLOBAL_ADMIN_ID)
    with TopazArmClient(credential) as client:
        client.delete_subscription(_SUBSCRIPTION_ID)
        client.create_subscription(_SUBSCRIPTION_ID, _SUBSCRIPTION_NAME)
        client.create_resource_group(_SUBSCRIPTION_ID, _RESOURCE_GROUP)

    from azure.mgmt.keyvault import KeyVaultManagementClient
    from azure.mgmt.keyvault.models import (
        VaultCreateOrUpdateParameters,
        VaultProperties,
        Sku,
        SkuName,
        SkuFamily,
    )
    from topaz_sdk.helpers import DEFAULT_RESOURCE_MANAGER_PORT

    kv_client = KeyVaultManagementClient(
        credential=credential,
        subscription_id=_SUBSCRIPTION_ID,
        base_url=f"https://topaz.local.dev:{DEFAULT_RESOURCE_MANAGER_PORT}",
        credential_scopes=[f"https://topaz.local.dev:{DEFAULT_RESOURCE_MANAGER_PORT}/.default"],
    )
    kv_client.vaults.begin_create_or_update(
        _RESOURCE_GROUP,
        _VAULT_NAME,
        VaultCreateOrUpdateParameters(
            location="westeurope",
            properties=VaultProperties(
                tenant_id="50717675-3E5E-4A1E-8CB5-C62D8BE8CA48",
                sku=Sku(family=SkuFamily.A, name=SkuName.STANDARD),
                enable_soft_delete=True,
            ),
        ),
    ).result()

    yield


# ---------------------------------------------------------------------------
# Helper: fresh SecretClient per test (stateless)
# ---------------------------------------------------------------------------

def _secret_client() -> SecretClient:
    return SecretClient(
        vault_url=_VAULT_URL,
        credential=AzureLocalCredential(GLOBAL_ADMIN_ID),
        verify_challenge_resource=False,
    )


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------

def test_secret_create_should_be_fetchable():
    client = _secret_client()
    created = client.set_secret("secret-name", "test")
    fetched = client.get_secret("secret-name")

    assert fetched.value == "test"
    assert created.value == "test"
    assert fetched.id is not None


def test_secret_not_created_should_not_be_fetchable():
    client = _secret_client()
    with pytest.raises(ResourceNotFoundError):
        client.get_secret("secret-not-existing-py")


def test_secret_created_twice_should_have_two_versions():
    client = _secret_client()
    first = client.set_secret("versioned-secret", "value-v1")
    second = client.set_secret("versioned-secret", "value-v2")

    latest = client.get_secret("versioned-secret")
    original = client.get_secret("versioned-secret", version=first.properties.version)

    assert latest.value == "value-v2"
    assert first.value == "value-v1"
    assert latest.id is not None
    assert second.id is not None
    assert original.value == "value-v1"


def test_list_secrets_should_return_all():
    client = _secret_client()

    for prop in client.list_properties_of_secrets():
        client.begin_delete_secret(prop.name).wait()

    client.set_secret("list-secret-one", "alpha")
    client.set_secret("list-secret-two", "beta")

    secrets = list(client.list_properties_of_secrets())

    assert len(secrets) == 2
    assert all(s.id is not None for s in secrets)


def test_multiple_deleted_secrets_should_be_listable():
    client = _secret_client()
    client.set_secret("del-secret-a", "value-a")
    client.set_secret("del-secret-b", "value-b")

    client.begin_delete_secret("del-secret-a").wait()
    client.begin_delete_secret("del-secret-b").wait()

    deleted = list(client.list_deleted_secrets())
    names = [s.name for s in deleted]

    assert "del-secret-a" in names
    assert "del-secret-b" in names
    # In azure-keyvault-secrets 4.11+, these attributes live directly on DeletedSecret
    for s in deleted:
        assert s.deleted_date is not None
        assert s.scheduled_purge_date is not None


def test_deleted_secret_should_be_retrievable():
    client = _secret_client()
    client.set_secret("secret-to-delete", "original-value")
    client.begin_delete_secret("secret-to-delete").wait()

    deleted = client.get_deleted_secret("secret-to-delete")

    assert deleted.name == "secret-to-delete"
    # Python SDK may expose value via .value or not at all; verify the secret was stored
    assert deleted.id is not None
    # In azure-keyvault-secrets 4.11+, deleted_date is a direct attribute on DeletedSecret
    assert deleted.deleted_date is not None


def test_deleted_secret_should_be_recoverable():
    client = _secret_client()
    client.set_secret("secret-to-recover", "original-value")
    client.begin_delete_secret("secret-to-recover").wait()
    client.begin_recover_deleted_secret("secret-to-recover").wait()

    recovered = client.get_secret("secret-to-recover")

    assert recovered.name == "secret-to-recover"
    assert recovered.value == "original-value"


def test_recovered_secret_should_not_appear_in_deleted():
    client = _secret_client()
    client.set_secret("recover-cleanup", "some-value")
    client.begin_delete_secret("recover-cleanup").wait()
    client.begin_recover_deleted_secret("recover-cleanup").wait()

    deleted_names = [s.name for s in client.list_deleted_secrets()]
    assert "recover-cleanup" not in deleted_names


def test_removed_secret_should_no_longer_be_available():
    client = _secret_client()
    client.set_secret("removed-secret", "test")
    client.begin_delete_secret("removed-secret")

    with pytest.raises(ResourceNotFoundError):
        client.get_secret("removed-secret")
