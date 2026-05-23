"""E2E tests for Azure Subscriptions and Resource Groups."""

from __future__ import annotations

import uuid

import pytest

from azure.mgmt.resource import ResourceManagementClient
from azure.mgmt.resource.resources.models import ResourceGroup, ResourceGroupPatchable
from azure.mgmt.subscription import SubscriptionClient

from topaz_sdk.helpers import DEFAULT_RESOURCE_MANAGER_PORT
from topaz_sdk.identity import GLOBAL_ADMIN_ID, AzureLocalCredential
from topaz_sdk.client import TopazArmClient

_RG_SUBSCRIPTION_ID = "b0000008-0000-0000-0000-000000000008"
_RESOURCE_GROUP = "rg-identifier-test"
_RM_BASE_URL = f"https://topaz.local.dev:{DEFAULT_RESOURCE_MANAGER_PORT}"


@pytest.fixture(scope="module", autouse=True)
def resource_group_environment():
    credential = AzureLocalCredential(GLOBAL_ADMIN_ID)
    with TopazArmClient(credential) as client:
        client.delete_subscription(_RG_SUBSCRIPTION_ID)
        client.create_subscription(_RG_SUBSCRIPTION_ID, "sub-rg-test")
        client.create_resource_group(_RG_SUBSCRIPTION_ID, _RESOURCE_GROUP)
    yield


def _subscription_client() -> SubscriptionClient:
    return SubscriptionClient(
        credential=AzureLocalCredential(GLOBAL_ADMIN_ID),
        base_url=_RM_BASE_URL,
        credential_scopes=[f"{_RM_BASE_URL}/.default"],
    )


def _resource_client(subscription_id: str) -> ResourceManagementClient:
    return ResourceManagementClient(
        credential=AzureLocalCredential(GLOBAL_ADMIN_ID),
        subscription_id=subscription_id,
        base_url=_RM_BASE_URL,
        credential_scopes=[f"{_RM_BASE_URL}/.default"],
    )


# ------------------------------------------------------------------
# Subscription tests
# ------------------------------------------------------------------


def test_subscription_create_and_get():
    sub_id = str(uuid.uuid4())
    credential = AzureLocalCredential(GLOBAL_ADMIN_ID)
    with TopazArmClient(credential) as client:
        client.create_subscription(sub_id, "sub-create-get-test")

    sub_client = _subscription_client()
    sub = sub_client.subscriptions.get(sub_id)
    assert sub.subscription_id == sub_id
    assert sub.display_name == "sub-create-get-test"


def test_subscription_tags_updated():
    sub_id = str(uuid.uuid4())
    credential = AzureLocalCredential(GLOBAL_ADMIN_ID)
    with TopazArmClient(credential) as client:
        client.create_subscription(sub_id, "sub-tags-test")
        client.update_subscription(sub_id, "sub-tags-test", tags={"env": "test"})
        raw = client._get(f"subscriptions/{sub_id}").json()
    assert raw.get("tags") is not None
    assert raw["tags"].get("env") == "test"


def test_subscription_tags_values_updated():
    sub_id = str(uuid.uuid4())
    credential = AzureLocalCredential(GLOBAL_ADMIN_ID)
    with TopazArmClient(credential) as client:
        client.create_subscription(sub_id, "sub-tagval-test")
        client.update_subscription(sub_id, "sub-tagval-test", tags={"env": "dev"})
        client.update_subscription(sub_id, "sub-tagval-test", tags={"env": "prod"})
        raw = client._get(f"subscriptions/{sub_id}").json()
    assert raw.get("tags") is not None
    assert raw["tags"].get("env") == "prod"


def test_subscription_tags_removed():
    sub_id = str(uuid.uuid4())
    credential = AzureLocalCredential(GLOBAL_ADMIN_ID)
    with TopazArmClient(credential) as client:
        client.create_subscription(sub_id, "sub-tagrm-test")
        client.update_subscription(sub_id, "sub-tagrm-test", tags={"env": "test"})
        client.update_subscription(sub_id, "sub-tagrm-test", tags={})
        raw = client._get(f"subscriptions/{sub_id}").json()
    tags = raw.get("tags") or {}
    assert len(tags) == 0


def test_subscription_cancel():
    sub_id = str(uuid.uuid4())
    credential = AzureLocalCredential(GLOBAL_ADMIN_ID)
    with TopazArmClient(credential) as client:
        client.create_subscription(sub_id, "sub-cancel-test")
        client.cancel_subscription(sub_id)

    sub_client = _subscription_client()
    sub = sub_client.subscriptions.get(sub_id)
    assert sub.state == "Disabled"


def test_subscription_enable():
    sub_id = str(uuid.uuid4())
    credential = AzureLocalCredential(GLOBAL_ADMIN_ID)
    with TopazArmClient(credential) as client:
        client.create_subscription(sub_id, "sub-enable-test")
        client.cancel_subscription(sub_id)
        client.enable_subscription(sub_id)

    sub_client = _subscription_client()
    sub = sub_client.subscriptions.get(sub_id)
    assert sub.state == "Enabled"


def test_subscription_list_locations():
    sub_id = str(uuid.uuid4())
    credential = AzureLocalCredential(GLOBAL_ADMIN_ID)
    with TopazArmClient(credential) as client:
        client.create_subscription(sub_id, "sub-locations-test")

    sub_client = _subscription_client()
    locations = list(sub_client.subscriptions.list_locations(sub_id))
    assert len(locations) > 0
    assert all(loc.name is not None for loc in locations)


# ------------------------------------------------------------------
# Resource Group tests
# ------------------------------------------------------------------


def test_resource_group_create_and_get():
    client = _resource_client(_RG_SUBSCRIPTION_ID)
    rg_name = f"rg-create-{uuid.uuid4().hex[:8]}"

    client.resource_groups.create_or_update(rg_name, ResourceGroup(location="westeurope"))
    result = client.resource_groups.get(rg_name)
    assert result.name == rg_name
    assert result.location == "westeurope"


def test_resource_group_existence_check():
    client = _resource_client(_RG_SUBSCRIPTION_ID)
    rg_name = f"rg-exists-{uuid.uuid4().hex[:8]}"

    client.resource_groups.create_or_update(rg_name, ResourceGroup(location="westeurope"))
    assert client.resource_groups.check_existence(rg_name) is True


def test_resource_group_tags_updated():
    client = _resource_client(_RG_SUBSCRIPTION_ID)
    rg_name = f"rg-tags-{uuid.uuid4().hex[:8]}"

    client.resource_groups.create_or_update(rg_name, ResourceGroup(location="westeurope"))
    result = client.resource_groups.update(rg_name, ResourceGroupPatchable(tags={"env": "test"}))
    assert result.tags is not None
    assert result.tags.get("env") == "test"


def test_resource_group_does_not_exist():
    client = _resource_client(_RG_SUBSCRIPTION_ID)
    assert client.resource_groups.check_existence("rg-nonexistent-zz99") is False


def test_resource_group_list_multiple():
    client = _resource_client(_RG_SUBSCRIPTION_ID)

    rg_names = [f"rg-list-{i}-{uuid.uuid4().hex[:6]}" for i in range(3)]
    for rg_name in rg_names:
        client.resource_groups.create_or_update(rg_name, ResourceGroup(location="westeurope"))

    rgs = list(client.resource_groups.list())
    listed_names = [rg.name for rg in rgs]
    for rg_name in rg_names:
        assert rg_name in listed_names
