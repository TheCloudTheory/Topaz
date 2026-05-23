"""
Azure Storage account E2E tests — Python port of AzureStorageServiceTests.cs.

Covers storage account CRUD, access keys, name availability, SAS token
generation, and ARM-level blob container / queue management.
All operations use the Azure management-plane SDK against the Topaz emulator.
"""

import pytest
from datetime import datetime, timedelta, timezone

from azure.core.exceptions import HttpResponseError
from azure.mgmt.storage import StorageManagementClient
from azure.mgmt.storage.models import (
    StorageAccountCreateParameters,
    StorageAccountUpdateParameters,
    StorageAccountRegenerateKeyParameters,
    AccountSasParameters,
    ServiceSasParameters,
    Sku,
    BlobContainer,
    StorageQueue,
)

from topaz_sdk import AzureLocalCredential, TopazArmClient, GLOBAL_ADMIN_ID
from topaz_sdk.helpers import DEFAULT_RESOURCE_MANAGER_PORT

# ---------------------------------------------------------------------------
# Module-level constants (distinct GUIDs from the .NET E2E suite)
# ---------------------------------------------------------------------------

_SUBSCRIPTION_ID = "a0000003-0000-0000-0000-000000000003"
_SUBSCRIPTION_NAME = "py-sub-storage-test"
_RESOURCE_GROUP = "py-rg-storage-test"
_STORAGE_ACCOUNT_NAME = "pystorageacctest"

_RM_BASE_URL = f"https://topaz.local.dev:{DEFAULT_RESOURCE_MANAGER_PORT}"


# ---------------------------------------------------------------------------
# Module-scoped setup: create subscription and resource group once
# ---------------------------------------------------------------------------

@pytest.fixture(scope="module", autouse=True)
def storage_environment():
    credential = AzureLocalCredential(GLOBAL_ADMIN_ID)
    with TopazArmClient(credential) as client:
        client.delete_subscription(_SUBSCRIPTION_ID)
        client.create_subscription(_SUBSCRIPTION_ID, _SUBSCRIPTION_NAME)
        client.create_resource_group(_SUBSCRIPTION_ID, _RESOURCE_GROUP)
    yield


# ---------------------------------------------------------------------------
# Helper: fresh StorageManagementClient per test (stateless)
# ---------------------------------------------------------------------------

def _storage_client() -> StorageManagementClient:
    return StorageManagementClient(
        credential=AzureLocalCredential(GLOBAL_ADMIN_ID),
        subscription_id=_SUBSCRIPTION_ID,
        base_url=_RM_BASE_URL,
        credential_scopes=[f"{_RM_BASE_URL}/.default"],
    )


def _create_account(client: StorageManagementClient, account_name: str = _STORAGE_ACCOUNT_NAME):
    """Create a Standard_LRS StorageV2 account and return it."""
    poller = client.storage_accounts.begin_create(
        _RESOURCE_GROUP,
        account_name,
        StorageAccountCreateParameters(
            sku=Sku(name="Standard_LRS"),
            kind="StorageV2",
            location="westeurope",
        ),
    )
    return poller.result()


# ---------------------------------------------------------------------------
# Storage account lifecycle
# ---------------------------------------------------------------------------

def test_storage_account_create_should_be_fetchable_and_deletable():
    client = _storage_client()
    _create_account(client)

    account = client.storage_accounts.get_properties(_RESOURCE_GROUP, _STORAGE_ACCOUNT_NAME)

    assert account.name == _STORAGE_ACCOUNT_NAME
    assert account.kind == "StorageV2"
    assert account.sku.name == "Standard_LRS"

    client.storage_accounts.delete(_RESOURCE_GROUP, _STORAGE_ACCOUNT_NAME)

    with pytest.raises(HttpResponseError):
        client.storage_accounts.get_properties(_RESOURCE_GROUP, _STORAGE_ACCOUNT_NAME)


def test_storage_account_should_have_two_access_keys():
    client = _storage_client()
    _create_account(client)

    result = client.storage_accounts.list_keys(_RESOURCE_GROUP, _STORAGE_ACCOUNT_NAME)
    keys = result["keys"]

    assert len(keys) == 2
    for key in keys:
        assert key["value"] is not None
        assert key["keyName"] is not None
        assert key["permissions"] is not None


def test_storage_account_list_by_subscription():
    client = _storage_client()
    _create_account(client)

    accounts = list(client.storage_accounts.list())

    assert any(a.name == _STORAGE_ACCOUNT_NAME for a in accounts)


def test_storage_account_check_name_availability():
    client = _storage_client()

    available = client.storage_accounts.check_name_availability({"name": "storcheckpyavail123", "type": "Microsoft.Storage/storageAccounts"})
    assert available.name_available is True
    assert available.reason is None

    _create_account(client)

    unavailable = client.storage_accounts.check_name_availability({"name": _STORAGE_ACCOUNT_NAME, "type": "Microsoft.Storage/storageAccounts"})
    assert unavailable.name_available is False
    assert unavailable.reason == "AlreadyExists"


def test_storage_account_update_applies_tags_and_preserves_keys():
    client = _storage_client()
    _create_account(client)
    original_keys = client.storage_accounts.list_keys(_RESOURCE_GROUP, _STORAGE_ACCOUNT_NAME)["keys"]

    updated = client.storage_accounts.update(
        _RESOURCE_GROUP,
        _STORAGE_ACCOUNT_NAME,
        StorageAccountUpdateParameters(tags={"env": "test"}),
    )

    assert updated.tags.get("env") == "test"

    updated_keys = client.storage_accounts.list_keys(_RESOURCE_GROUP, _STORAGE_ACCOUNT_NAME)["keys"]
    assert updated_keys[0]["value"] == original_keys[0]["value"]
    assert updated_keys[1]["value"] == original_keys[1]["value"]


def test_storage_account_regenerate_key_returns_new_value_and_preserves_other():
    client = _storage_client()
    _create_account(client)
    original_keys = client.storage_accounts.list_keys(_RESOURCE_GROUP, _STORAGE_ACCOUNT_NAME)["keys"]

    regenerated = client.storage_accounts.regenerate_key(
        _RESOURCE_GROUP,
        _STORAGE_ACCOUNT_NAME,
        StorageAccountRegenerateKeyParameters(key_name="key1"),
    )["keys"]

    assert regenerated[0]["value"] != original_keys[0]["value"]  # key1 changed
    assert regenerated[1]["value"] == original_keys[1]["value"]  # key2 unchanged
    assert regenerated[0]["keyName"] == "key1"
    assert regenerated[1]["keyName"] == "key2"


def test_storage_account_list_account_sas_returns_token():
    client = _storage_client()
    _create_account(client)
    expiry = datetime.now(tz=timezone.utc) + timedelta(hours=1)

    result = client.storage_accounts.list_account_sas(
        _RESOURCE_GROUP,
        _STORAGE_ACCOUNT_NAME,
        AccountSasParameters(
            services="b",
            resource_types="s",
            permissions="r",
            shared_access_expiry_time=expiry,
        ),
    )

    token = result.account_sas_token
    assert token
    assert "sv=" in token
    assert "ss=" in token
    assert "srt=" in token
    assert "sp=" in token
    assert "se=" in token
    assert "sig=" in token


def test_storage_account_list_service_sas_returns_token():
    client = _storage_client()
    _create_account(client)
    expiry = datetime.now(tz=timezone.utc) + timedelta(hours=1)

    result = client.storage_accounts.list_service_sas(
        _RESOURCE_GROUP,
        _STORAGE_ACCOUNT_NAME,
        ServiceSasParameters(
            canonicalized_resource=f"/blob/{_STORAGE_ACCOUNT_NAME}/mycontainer",
            resource="c",
            permissions="r",
            shared_access_expiry_time=expiry,
        ),
    )

    token = result.service_sas_token
    assert token
    assert "sv=" in token
    assert "sr=" in token
    assert "sp=" in token
    assert "se=" in token
    assert "sig=" in token


# ---------------------------------------------------------------------------
# Blob container (ARM)
# ---------------------------------------------------------------------------

def test_blob_container_create_returns_name():
    client = _storage_client()
    _create_account(client)

    container = client.blob_containers.create(
        _RESOURCE_GROUP, _STORAGE_ACCOUNT_NAME, "arm-test-container", BlobContainer()
    )

    assert container.name == "arm-test-container"


def test_blob_container_create_is_idempotent():
    client = _storage_client()
    _create_account(client)
    client.blob_containers.create(
        _RESOURCE_GROUP, _STORAGE_ACCOUNT_NAME, "arm-idem-container", BlobContainer()
    )

    container = client.blob_containers.create(
        _RESOURCE_GROUP, _STORAGE_ACCOUNT_NAME, "arm-idem-container", BlobContainer()
    )

    assert container.name == "arm-idem-container"


def test_blob_container_get_returns_created_container():
    client = _storage_client()
    _create_account(client)
    client.blob_containers.create(
        _RESOURCE_GROUP, _STORAGE_ACCOUNT_NAME, "arm-get-container", BlobContainer()
    )

    container = client.blob_containers.get(
        _RESOURCE_GROUP, _STORAGE_ACCOUNT_NAME, "arm-get-container"
    )

    assert container.name == "arm-get-container"
    assert "Microsoft.Storage/storageAccounts/blobServices/containers" in container.type


def test_blob_container_get_raises_for_nonexistent_container():
    client = _storage_client()
    _create_account(client)

    with pytest.raises(HttpResponseError):
        client.blob_containers.get(_RESOURCE_GROUP, _STORAGE_ACCOUNT_NAME, "nonexistent-container")


# ---------------------------------------------------------------------------
# Storage queue (ARM)
# ---------------------------------------------------------------------------

def test_storage_queue_create_returns_name():
    client = _storage_client()
    _create_account(client)

    queue = client.queue.create(
        _RESOURCE_GROUP, _STORAGE_ACCOUNT_NAME, "arm-test-queue", StorageQueue()
    )

    assert queue.name == "arm-test-queue"


def test_storage_queue_create_is_idempotent():
    client = _storage_client()
    _create_account(client)
    client.queue.create(
        _RESOURCE_GROUP, _STORAGE_ACCOUNT_NAME, "arm-idem-queue", StorageQueue()
    )

    queue = client.queue.create(
        _RESOURCE_GROUP, _STORAGE_ACCOUNT_NAME, "arm-idem-queue", StorageQueue()
    )

    assert queue.name == "arm-idem-queue"


def test_storage_queue_get_returns_created_queue():
    client = _storage_client()
    _create_account(client)
    client.queue.create(
        _RESOURCE_GROUP, _STORAGE_ACCOUNT_NAME, "arm-get-queue", StorageQueue()
    )

    queue = client.queue.get(_RESOURCE_GROUP, _STORAGE_ACCOUNT_NAME, "arm-get-queue")

    assert queue.name == "arm-get-queue"
    assert "Microsoft.Storage/storageAccounts/queueServices/queues" in queue.type


def test_storage_queue_get_raises_for_nonexistent_queue():
    client = _storage_client()
    _create_account(client)

    with pytest.raises(HttpResponseError):
        client.queue.get(_RESOURCE_GROUP, _STORAGE_ACCOUNT_NAME, "nonexistent-queue")
