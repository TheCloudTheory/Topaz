"""E2E tests for Azure Management Groups, entities, subscriptions, and hierarchy settings."""

from __future__ import annotations

import uuid

import pytest
import requests

from topaz_sdk.identity import GLOBAL_ADMIN_ID, AzureLocalCredential
from topaz_sdk.client import TopazArmClient

_ROOT_MG_ID = "50717675-3E5E-4A1E-8CB5-C62D8BE8CA48"


def _topaz_client() -> TopazArmClient:
    return TopazArmClient(AzureLocalCredential(GLOBAL_ADMIN_ID))


# ------------------------------------------------------------------
# Descendant tests
# ------------------------------------------------------------------


def test_descendants_not_found_returns_404():
    with _topaz_client() as client:
        with pytest.raises(requests.exceptions.HTTPError) as exc_info:
            client.get_descendants("nonexistent-mg-zzz99")
        assert exc_info.value.response.status_code == 404


def test_descendants_empty_when_no_children():
    group_id = f"mg-empty-desc-{uuid.uuid4().hex[:8]}"
    with _topaz_client() as client:
        client.create_management_group(group_id, "Empty Descendants MG")
        result = client.get_descendants(group_id)
        values = result.get("value", [])
        assert len(values) == 0


def test_descendants_contains_subscription():
    group_id = f"mg-sub-desc-{uuid.uuid4().hex[:8]}"
    sub_id = str(uuid.uuid4())
    with _topaz_client() as client:
        client.create_subscription(sub_id, "desc-sub-test")
        client.create_management_group(group_id, "Sub Descendants MG")
        client.associate_subscription_with_management_group(group_id, sub_id)

        result = client.get_descendants(group_id)
        values = result.get("value", [])
        sub_names = [v.get("name") for v in values]
        assert sub_id in sub_names


def test_descendants_contains_child_group():
    parent_id = f"mg-parent-desc-{uuid.uuid4().hex[:8]}"
    child_id = f"mg-child-desc-{uuid.uuid4().hex[:8]}"
    with _topaz_client() as client:
        client.create_management_group(parent_id, "Parent MG")
        client.create_management_group_with_parent(child_id, "Child MG", parent_id)

        result = client.get_descendants(parent_id)
        values = result.get("value", [])
        child_names = [v.get("name") for v in values]
        assert child_id in child_names


def test_descendants_nested_hierarchy():
    root_id = f"mg-root-hier-{uuid.uuid4().hex[:8]}"
    child_id = f"mg-ch-hier-{uuid.uuid4().hex[:8]}"
    sub_id = str(uuid.uuid4())
    with _topaz_client() as client:
        client.create_management_group(root_id, "Root Hier MG")
        client.create_management_group_with_parent(child_id, "Child Hier MG", root_id)
        client.create_subscription(sub_id, "nested-hier-sub")
        client.associate_subscription_with_management_group(child_id, sub_id)

        result = client.get_descendants(root_id)
        values = result.get("value", [])
        names = [v.get("name") for v in values]
        assert child_id in names
        assert sub_id in names


# ------------------------------------------------------------------
# Entities tests
# ------------------------------------------------------------------


def test_entities_always_has_root_group():
    with _topaz_client() as client:
        result = client.get_entities()
        values = result.get("value", [])
        names = [v.get("name") for v in values]
        assert _ROOT_MG_ID in names, "Root management group should be present after bootstrap"


def test_entities_contains_management_group():
    group_id = f"mg-entities-mg-{uuid.uuid4().hex[:8]}"
    display_name = f"Entities MG {group_id}"
    with _topaz_client() as client:
        client.create_management_group(group_id, display_name)

        result = client.get_entities()
        values = result.get("value", [])
        mg = next((v for v in values if v.get("name") == group_id), None)

        assert mg is not None
        assert mg.get("type") == "Microsoft.Management/managementGroups"
        assert mg.get("id") == f"/providers/Microsoft.Management/managementGroups/{group_id}"
        props = mg.get("properties", {})
        assert props.get("displayName") == display_name


def test_entities_contains_subscription():
    group_id = f"mg-entities-sub-{uuid.uuid4().hex[:8]}"
    sub_id = str(uuid.uuid4())
    with _topaz_client() as client:
        client.create_management_group(group_id, "Entities Sub MG")
        client.create_subscription(sub_id, "entities-sub-test")
        client.associate_subscription_with_management_group(group_id, sub_id)

        result = client.get_entities()
        values = result.get("value", [])
        sub_entity = next((v for v in values if v.get("name") == sub_id), None)

        assert sub_entity is not None
        assert sub_entity.get("type") == "/subscriptions"
        assert sub_entity.get("id") == f"/subscriptions/{sub_id}"
        props = sub_entity.get("properties", {})
        parent = props.get("parent", {})
        assert parent.get("id") == f"/providers/Microsoft.Management/managementGroups/{group_id}"


# ------------------------------------------------------------------
# Management Group + Subscription association tests
# ------------------------------------------------------------------


def test_associate_returns_association():
    group_id = f"mg-assoc-ret-{uuid.uuid4().hex[:8]}"
    sub_id = str(uuid.uuid4())
    with _topaz_client() as client:
        client.create_management_group(group_id, "Assoc Returns MG")
        client.create_subscription(sub_id, "assoc-test-sub")
        result = client.associate_subscription_with_management_group(group_id, sub_id)
        assert result is not None


def test_get_after_associate():
    group_id = f"mg-get-after-{uuid.uuid4().hex[:8]}"
    sub_id = str(uuid.uuid4())
    with _topaz_client() as client:
        client.create_management_group(group_id, "Get After Assoc MG")
        client.create_subscription(sub_id, "get-after-assoc-sub")
        client.associate_subscription_with_management_group(group_id, sub_id)

        result = client.get_subscription_under_management_group(group_id, sub_id)
        assert result is not None
        assert result.get("name") == sub_id


def test_disassociate_get_returns_404():
    group_id = f"mg-disassoc-{uuid.uuid4().hex[:8]}"
    sub_id = str(uuid.uuid4())
    with _topaz_client() as client:
        client.create_management_group(group_id, "Disassoc MG")
        client.create_subscription(sub_id, "disassoc-sub")
        client.associate_subscription_with_management_group(group_id, sub_id)
        client.disassociate_subscription_from_management_group(group_id, sub_id)

        with pytest.raises(requests.exceptions.HTTPError) as exc_info:
            client.get_subscription_under_management_group(group_id, sub_id)
        assert exc_info.value.response.status_code == 404


def test_nonexistent_mg_associate_returns_404():
    sub_id = str(uuid.uuid4())
    with _topaz_client() as client:
        client.create_subscription(sub_id, "nonexistent-mg-sub")
        with pytest.raises(requests.exceptions.HTTPError) as exc_info:
            client.associate_subscription_with_management_group("nonexistent-mg-zzz99", sub_id)
        assert exc_info.value.response.status_code == 404


def test_not_associated_get_returns_404():
    group_id = f"mg-not-assoc-{uuid.uuid4().hex[:8]}"
    sub_id = str(uuid.uuid4())
    with _topaz_client() as client:
        client.create_management_group(group_id, "Not Assoc MG")
        with pytest.raises(requests.exceptions.HTTPError) as exc_info:
            client.get_subscription_under_management_group(group_id, sub_id)
        assert exc_info.value.response.status_code == 404


def test_new_subscription_auto_placed_in_root():
    sub_id = str(uuid.uuid4())
    with _topaz_client() as client:
        client.create_subscription(sub_id, "auto-place-sub")

        result = client.get_subscription_under_management_group(_ROOT_MG_ID, sub_id)
        assert result is not None
        assert result.get("name") == sub_id


def test_root_contains_subscription_in_descendants():
    sub_id = str(uuid.uuid4())
    with _topaz_client() as client:
        client.create_subscription(sub_id, "root-desc-sub")

        result = client.get_descendants(_ROOT_MG_ID)
        values = result.get("value", [])
        names = [v.get("name") for v in values]
        assert sub_id in names


# ------------------------------------------------------------------
# Hierarchy Settings tests
# ------------------------------------------------------------------


def test_hierarchy_settings_create_returns_settings():
    group_id = f"mg-hs-create-{uuid.uuid4().hex[:8]}"
    with _topaz_client() as client:
        client.create_management_group(group_id, "HS Create MG")

        result = client.create_or_update_hierarchy_settings(
            group_id, require_authorization_for_group_creation=True
        )
        assert result is not None
        props = result.get("properties", {})
        assert props.get("requireAuthorizationForGroupCreation") is True


def test_hierarchy_settings_get_after_create():
    group_id = f"mg-hs-get-{uuid.uuid4().hex[:8]}"
    with _topaz_client() as client:
        client.create_management_group(group_id, "HS Get MG")
        client.create_or_update_hierarchy_settings(
            group_id, require_authorization_for_group_creation=True
        )

        result = client.get_hierarchy_settings(group_id)
        assert result is not None
        props = result.get("properties", {})
        assert props.get("requireAuthorizationForGroupCreation") is True


def test_hierarchy_settings_list_after_create():
    group_id = f"mg-hs-list-{uuid.uuid4().hex[:8]}"
    with _topaz_client() as client:
        client.create_management_group(group_id, "HS List MG")
        client.create_or_update_hierarchy_settings(
            group_id, require_authorization_for_group_creation=True
        )

        result = client.list_hierarchy_settings(group_id)
        assert result is not None
        values = result.get("value", [])
        assert len(values) >= 1


def test_hierarchy_settings_list_empty_when_no_settings():
    group_id = f"mg-hs-empty-{uuid.uuid4().hex[:8]}"
    with _topaz_client() as client:
        client.create_management_group(group_id, "HS Empty MG")

        result = client.list_hierarchy_settings(group_id)
        assert result is not None
        values = result.get("value", [])
        assert len(values) == 0


def test_hierarchy_settings_update_returns_updated():
    group_id = f"mg-hs-upd-{uuid.uuid4().hex[:8]}"
    child_mg_id = f"mg-hs-dflt-{uuid.uuid4().hex[:8]}"
    with _topaz_client() as client:
        client.create_management_group(group_id, "HS Update MG")
        client.create_management_group_with_parent(child_mg_id, "HS Default Child MG", group_id)
        client.create_or_update_hierarchy_settings(
            group_id, require_authorization_for_group_creation=False
        )

        result = client.update_hierarchy_settings(
            group_id, default_management_group=child_mg_id
        )
        assert result is not None
        props = result.get("properties", {})
        assert props.get("defaultManagementGroup") == child_mg_id


def test_hierarchy_settings_delete_get_returns_404():
    group_id = f"mg-hs-del-{uuid.uuid4().hex[:8]}"
    with _topaz_client() as client:
        client.create_management_group(group_id, "HS Delete MG")
        client.create_or_update_hierarchy_settings(
            group_id, require_authorization_for_group_creation=True
        )
        client.delete_hierarchy_settings(group_id)

        with pytest.raises(requests.exceptions.HTTPError) as exc_info:
            client.get_hierarchy_settings(group_id)
        assert exc_info.value.response.status_code == 404


def test_hierarchy_settings_nonexistent_mg_create_returns_404():
    with _topaz_client() as client:
        with pytest.raises(requests.exceptions.HTTPError) as exc_info:
            client.create_or_update_hierarchy_settings(
                "nonexistent-mg-hs-zzz99", require_authorization_for_group_creation=True
            )
        assert exc_info.value.response.status_code == 404


def test_hierarchy_settings_nonexistent_mg_get_returns_404():
    with _topaz_client() as client:
        with pytest.raises(requests.exceptions.HTTPError) as exc_info:
            client.get_hierarchy_settings("nonexistent-mg-hs-zzz99")
        assert exc_info.value.response.status_code == 404
