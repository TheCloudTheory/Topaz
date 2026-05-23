"""E2E tests for Azure Authorization (role definitions and role assignments)."""

from __future__ import annotations

import uuid

import pytest
import requests

from azure.mgmt.authorization import AuthorizationManagementClient
from azure.mgmt.authorization.models import (
    Permission,
    RoleAssignmentCreateParameters,
    RoleDefinition,
)

from topaz_sdk.helpers import DEFAULT_RESOURCE_MANAGER_PORT
from topaz_sdk.identity import GLOBAL_ADMIN_ID, AzureLocalCredential
from topaz_sdk.client import TopazArmClient

_SUBSCRIPTION_ID = "b0000001-0000-0000-0000-000000000001"
_RESOURCE_GROUP = "rg-authorization-test"
_RM_BASE_URL = f"https://topaz.local.dev:{DEFAULT_RESOURCE_MANAGER_PORT}"
_SUB_SCOPE = f"/subscriptions/{_SUBSCRIPTION_ID}"
_READER_ROLE_DEF_ID = "acdd72a7-3385-48ef-bd42-f606fba81ae7"


@pytest.fixture(scope="module", autouse=True)
def authorization_environment():
    credential = AzureLocalCredential(GLOBAL_ADMIN_ID)
    with TopazArmClient(credential) as client:
        client.delete_subscription(_SUBSCRIPTION_ID)
        client.create_subscription(_SUBSCRIPTION_ID, "sub-authorization-test")
        client.create_resource_group(_SUBSCRIPTION_ID, _RESOURCE_GROUP)
    yield


def _auth_client() -> AuthorizationManagementClient:
    return AuthorizationManagementClient(
        credential=AzureLocalCredential(GLOBAL_ADMIN_ID),
        subscription_id=_SUBSCRIPTION_ID,
        base_url=_RM_BASE_URL,
        credential_scopes=[f"{_RM_BASE_URL}/.default"],
    )


def _make_role_definition(role_name: str, actions: list[str]) -> RoleDefinition:
    return RoleDefinition(
        role_name=role_name,
        description=f"Test role: {role_name}",
        permissions=[Permission(actions=actions)],
        assignable_scopes=[_SUB_SCOPE],
    )


def test_role_definition_create_update_delete():
    client = _auth_client()
    role_def_id = str(uuid.uuid4())

    created = client.role_definitions.create_or_update(
        _SUB_SCOPE, role_def_id, _make_role_definition("test-role-sdk", ["Microsoft.Network/*/read"])
    )
    assert created.role_name == "test-role-sdk"

    read_back = client.role_definitions.get(_SUB_SCOPE, role_def_id)
    assert read_back.role_name == "test-role-sdk"

    updated_def = RoleDefinition(
        role_name="test-role-sdk",
        description="Updated description",
        permissions=[Permission(actions=["Microsoft.Network/*/read", "Microsoft.Network/*/write"])],
        assignable_scopes=[_SUB_SCOPE],
    )
    client.role_definitions.create_or_update(_SUB_SCOPE, role_def_id, updated_def)

    after_update = client.role_definitions.get(_SUB_SCOPE, role_def_id)
    assert after_update.description == "Updated description"

    client.role_definitions.delete(_SUB_SCOPE, role_def_id)

    with pytest.raises(Exception):
        client.role_definitions.get(_SUB_SCOPE, role_def_id)


def test_role_assignment_create_and_delete():
    client = _auth_client()
    role_def_id = str(uuid.uuid4())
    principal_id = str(uuid.uuid4())

    client.role_definitions.create_or_update(
        _SUB_SCOPE, role_def_id, _make_role_definition("assignment-role", ["*"])
    )

    assignment_name = str(uuid.uuid4())
    full_role_def_id = (
        f"{_SUB_SCOPE}/providers/Microsoft.Authorization/roleDefinitions/{role_def_id}"
    )
    created = client.role_assignments.create(
        _SUB_SCOPE,
        assignment_name,
        RoleAssignmentCreateParameters(
            role_definition_id=full_role_def_id,
            principal_id=principal_id,
        ),
    )
    assert str(created.principal_id) == principal_id

    client.role_assignments.delete(_SUB_SCOPE, assignment_name)
    with pytest.raises(Exception):
        client.role_assignments.get(_SUB_SCOPE, assignment_name)

    client.role_definitions.delete(_SUB_SCOPE, role_def_id)


def test_role_assignment_list():
    client = _auth_client()
    role_def_id = str(uuid.uuid4())
    principal_id = str(uuid.uuid4())

    client.role_definitions.create_or_update(
        _SUB_SCOPE, role_def_id, _make_role_definition("list-assignment-role", ["*"])
    )

    assignment_name = str(uuid.uuid4())
    full_role_def_id = (
        f"{_SUB_SCOPE}/providers/Microsoft.Authorization/roleDefinitions/{role_def_id}"
    )
    client.role_assignments.create(
        _SUB_SCOPE,
        assignment_name,
        RoleAssignmentCreateParameters(
            role_definition_id=full_role_def_id,
            principal_id=principal_id,
        ),
    )

    try:
        assignments = list(client.role_assignments.list_for_scope(_SUB_SCOPE))
        found = any(str(ra.principal_id) == principal_id for ra in assignments)
        assert found, "Created role assignment should appear in list"
    finally:
        client.role_assignments.delete(_SUB_SCOPE, assignment_name)
        client.role_definitions.delete(_SUB_SCOPE, role_def_id)


def test_role_assignment_scope_subscription():
    client = _auth_client()
    role_def_id = str(uuid.uuid4())
    principal_id = str(uuid.uuid4())

    client.role_definitions.create_or_update(
        _SUB_SCOPE, role_def_id, _make_role_definition("scope-test-role", ["*"])
    )

    assignment_name = str(uuid.uuid4())
    full_role_def_id = (
        f"{_SUB_SCOPE}/providers/Microsoft.Authorization/roleDefinitions/{role_def_id}"
    )
    client.role_assignments.create(
        _SUB_SCOPE,
        assignment_name,
        RoleAssignmentCreateParameters(
            role_definition_id=full_role_def_id,
            principal_id=principal_id,
        ),
    )

    try:
        created = client.role_assignments.get(_SUB_SCOPE, assignment_name)
        assert created.scope == _SUB_SCOPE
    finally:
        client.role_assignments.delete(_SUB_SCOPE, assignment_name)
        client.role_definitions.delete(_SUB_SCOPE, role_def_id)


def test_role_definition_list_well_formed():
    client = _auth_client()
    role_def_id = str(uuid.uuid4())

    client.role_definitions.create_or_update(
        _SUB_SCOPE,
        role_def_id,
        _make_role_definition("list-test-role", ["Microsoft.Network/*/read"]),
    )

    try:
        definitions = list(client.role_definitions.list(_SUB_SCOPE))
        assert len(definitions) > 0
        for rd in definitions:
            assert rd.role_name is not None and rd.role_name != ""
            assert rd.assignable_scopes is not None
    finally:
        client.role_definitions.delete(_SUB_SCOPE, role_def_id)


def test_role_definition_list_includes_builtin():
    client = _auth_client()
    definitions = list(client.role_definitions.list(_SUB_SCOPE))
    builtin_names = {rd.role_name.lower() for rd in definitions if rd.role_name}
    assert "reader" in builtin_names or "contributor" in builtin_names, (
        "Built-in role definitions (Reader or Contributor) should be present"
    )


def test_role_definition_list_filter():
    client = _auth_client()
    role_def_id = str(uuid.uuid4())
    role_name = f"filter-test-role-{uuid.uuid4().hex}"

    client.role_definitions.create_or_update(
        _SUB_SCOPE,
        role_def_id,
        RoleDefinition(
            role_name=role_name,
            description="Role for filter test",
            permissions=[Permission(actions=["Microsoft.Network/*/read"])],
            assignable_scopes=[_SUB_SCOPE],
        ),
    )

    try:
        odata_filter = f"roleName eq '{role_name}'"
        results = list(client.role_definitions.list(_SUB_SCOPE, filter=odata_filter))

        assert any(rd.role_name.lower() == role_name.lower() for rd in results), (
            "Filter should return the matching role definition"
        )
        assert all(rd.role_name.lower() == role_name.lower() for rd in results), (
            "Filter should not return non-matching role definitions"
        )
    finally:
        client.role_definitions.delete(_SUB_SCOPE, role_def_id)


def test_role_definition_list_pagination():
    client = _auth_client()
    created_ids = []

    try:
        for i in range(3):
            role_def_id = str(uuid.uuid4())
            client.role_definitions.create_or_update(
                _SUB_SCOPE,
                role_def_id,
                _make_role_definition(
                    f"paged-list-test-role-{uuid.uuid4().hex}", ["Microsoft.Network/*/read"]
                ),
            )
            created_ids.append(role_def_id)

        all_defs = list(client.role_definitions.list(_SUB_SCOPE))
        assert len(all_defs) >= 3, "Should have at least the 3 created role definitions"
    finally:
        for role_def_id in created_ids:
            client.role_definitions.delete(_SUB_SCOPE, role_def_id)


def test_role_definition_get_by_id():
    client = _auth_client()
    full_id = f"/providers/Microsoft.Authorization/roleDefinitions/{_READER_ROLE_DEF_ID}"
    result = client.role_definitions.get_by_id(full_id)

    assert result is not None
    assert result.role_name == "Reader"
    assert _READER_ROLE_DEF_ID in result.id


def test_hierarchy_subscription_scope_propagates_to_resource():
    """Role assigned at subscription scope should allow reading a vault in that subscription."""
    principal_id = str(uuid.uuid4())
    admin_credential = AzureLocalCredential(GLOBAL_ADMIN_ID)

    vault_name = f"hiersub{uuid.uuid4().hex}"[:20]
    vault_path = (
        f"subscriptions/{_SUBSCRIPTION_ID}/resourceGroups/{_RESOURCE_GROUP}"
        f"/providers/Microsoft.KeyVault/vaults/{vault_name}"
    )

    with TopazArmClient(admin_credential) as admin_client:
        admin_client._put(
            vault_path,
            {
                "location": "westeurope",
                "properties": {
                    "sku": {"family": "A", "name": "standard"},
                    "tenantId": "00000000-0000-0000-0000-000000000000",
                },
            },
        )

    client = _auth_client()
    role_def_id = str(uuid.uuid4())
    client.role_definitions.create_or_update(
        _SUB_SCOPE,
        role_def_id,
        RoleDefinition(
            role_name=f"hier-sub-{role_def_id[:8]}",
            description="Grants KV read",
            permissions=[Permission(actions=["Microsoft.KeyVault/vaults/read"])],
            assignable_scopes=[_SUB_SCOPE],
        ),
    )

    assignment_name = str(uuid.uuid4())
    full_role_def_id = (
        f"{_SUB_SCOPE}/providers/Microsoft.Authorization/roleDefinitions/{role_def_id}"
    )
    client.role_assignments.create(
        _SUB_SCOPE,
        assignment_name,
        RoleAssignmentCreateParameters(
            role_definition_id=full_role_def_id,
            principal_id=principal_id,
        ),
    )

    non_admin_credential = AzureLocalCredential(principal_id)
    with TopazArmClient(non_admin_credential) as non_admin_client:
        non_admin_client._get(vault_path)  # should not raise


def test_hierarchy_rg_scope_propagates_but_not_to_sibling():
    """Role assigned at RG scope should allow read in that RG but not a sibling RG."""
    principal_id = str(uuid.uuid4())
    rg_a = "hier-rg-a"
    rg_b = "hier-rg-b"
    admin_credential = AzureLocalCredential(GLOBAL_ADMIN_ID)

    with TopazArmClient(admin_credential) as admin_client:
        admin_client.delete_resource_group(_SUBSCRIPTION_ID, rg_a)
        admin_client.delete_resource_group(_SUBSCRIPTION_ID, rg_b)
        admin_client.create_resource_group(_SUBSCRIPTION_ID, rg_a)
        admin_client.create_resource_group(_SUBSCRIPTION_ID, rg_b)

    vault_a = f"hierrga{uuid.uuid4().hex}"[:20]
    vault_b = f"hierrgb{uuid.uuid4().hex}"[:20]
    vault_a_path = (
        f"subscriptions/{_SUBSCRIPTION_ID}/resourceGroups/{rg_a}"
        f"/providers/Microsoft.KeyVault/vaults/{vault_a}"
    )
    vault_b_path = (
        f"subscriptions/{_SUBSCRIPTION_ID}/resourceGroups/{rg_b}"
        f"/providers/Microsoft.KeyVault/vaults/{vault_b}"
    )
    vault_payload = {
        "location": "westeurope",
        "properties": {
            "sku": {"family": "A", "name": "standard"},
            "tenantId": "00000000-0000-0000-0000-000000000000",
        },
    }

    with TopazArmClient(admin_credential) as admin_client:
        admin_client._put(vault_a_path, vault_payload)
        admin_client._put(vault_b_path, vault_payload)

    client = _auth_client()
    role_def_id = str(uuid.uuid4())
    rg_scope = f"{_SUB_SCOPE}/resourceGroups/{rg_a}"
    client.role_definitions.create_or_update(
        _SUB_SCOPE,
        role_def_id,
        RoleDefinition(
            role_name=f"hier-rg-{role_def_id[:8]}",
            description="Grants KV read for RG A",
            permissions=[Permission(actions=["Microsoft.KeyVault/vaults/read"])],
            assignable_scopes=[_SUB_SCOPE],
        ),
    )

    assignment_name = str(uuid.uuid4())
    full_role_def_id = (
        f"{_SUB_SCOPE}/providers/Microsoft.Authorization/roleDefinitions/{role_def_id}"
    )
    with TopazArmClient(admin_credential) as admin_client:
        admin_client.create_resource_group_role_assignment(
            _SUBSCRIPTION_ID, rg_a, assignment_name, principal_id, full_role_def_id
        )

    non_admin_credential = AzureLocalCredential(principal_id)
    with TopazArmClient(non_admin_credential) as non_admin_client:
        # Should be able to read vault in rg_a
        non_admin_client._get(vault_a_path)

        # Should NOT be able to read vault in rg_b
        with pytest.raises(requests.exceptions.HTTPError) as exc_info:
            non_admin_client._get(vault_b_path)
        assert exc_info.value.response is not None
        assert exc_info.value.response.status_code in (401, 403)
