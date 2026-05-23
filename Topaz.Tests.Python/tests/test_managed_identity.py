"""E2E tests for Azure Managed Identity (user-assigned identities and federated credentials)."""

from __future__ import annotations

import uuid

import pytest

from azure.mgmt.msi import ManagedServiceIdentityClient
from azure.mgmt.msi.models import FederatedIdentityCredential, Identity

from topaz_sdk.helpers import DEFAULT_RESOURCE_MANAGER_PORT
from topaz_sdk.identity import GLOBAL_ADMIN_ID, AzureLocalCredential
from topaz_sdk.client import TopazArmClient

_SUBSCRIPTION_ID = "b0000003-0000-0000-0000-000000000003"
_RESOURCE_GROUP = "rg-msi-test"
_RM_BASE_URL = f"https://topaz.local.dev:{DEFAULT_RESOURCE_MANAGER_PORT}"


@pytest.fixture(scope="module", autouse=True)
def msi_environment():
    credential = AzureLocalCredential(GLOBAL_ADMIN_ID)
    with TopazArmClient(credential) as client:
        client.delete_subscription(_SUBSCRIPTION_ID)
        client.create_subscription(_SUBSCRIPTION_ID, "sub-msi-test")
        client.create_resource_group(_SUBSCRIPTION_ID, _RESOURCE_GROUP)
    yield


def _msi_client() -> ManagedServiceIdentityClient:
    return ManagedServiceIdentityClient(
        credential=AzureLocalCredential(GLOBAL_ADMIN_ID),
        subscription_id=_SUBSCRIPTION_ID,
        base_url=_RM_BASE_URL,
        credential_scopes=[f"{_RM_BASE_URL}/.default"],
    )


def test_identity_create():
    client = _msi_client()
    identity_name = f"identity-create-{uuid.uuid4().hex[:8]}"

    result = client.user_assigned_identities.create_or_update(
        _RESOURCE_GROUP,
        identity_name,
        Identity(location="westeurope"),
    )
    assert result.name == identity_name
    assert result.location == "westeurope"


def test_identity_delete():
    client = _msi_client()
    identity_name = f"identity-delete-{uuid.uuid4().hex[:8]}"

    client.user_assigned_identities.create_or_update(
        _RESOURCE_GROUP,
        identity_name,
        Identity(location="westeurope"),
    )

    client.user_assigned_identities.delete(_RESOURCE_GROUP, identity_name)

    with pytest.raises(Exception):
        client.user_assigned_identities.get(_RESOURCE_GROUP, identity_name)


def test_identity_with_tags():
    client = _msi_client()
    identity_name = f"identity-tags-{uuid.uuid4().hex[:8]}"

    result = client.user_assigned_identities.create_or_update(
        _RESOURCE_GROUP,
        identity_name,
        Identity(location="westeurope", tags={"env": "test", "project": "topaz"}),
    )
    assert result.tags is not None
    assert result.tags.get("env") == "test"
    assert result.tags.get("project") == "topaz"


def test_identity_update_tags():
    client = _msi_client()
    identity_name = f"identity-uptags-{uuid.uuid4().hex[:8]}"

    client.user_assigned_identities.create_or_update(
        _RESOURCE_GROUP,
        identity_name,
        Identity(location="westeurope", tags={"env": "dev"}),
    )

    updated = client.user_assigned_identities.create_or_update(
        _RESOURCE_GROUP,
        identity_name,
        Identity(location="westeurope", tags={"env": "prod"}),
    )
    assert updated.tags is not None
    assert updated.tags.get("env") == "prod"


def test_identity_list_by_rg():
    client = _msi_client()
    identity_name = f"identity-list-rg-{uuid.uuid4().hex[:8]}"

    client.user_assigned_identities.create_or_update(
        _RESOURCE_GROUP,
        identity_name,
        Identity(location="westeurope"),
    )

    identities = list(client.user_assigned_identities.list_by_resource_group(_RESOURCE_GROUP))
    names = [i.name for i in identities]
    assert identity_name in names


def test_identity_list_by_subscription():
    client = _msi_client()
    identity_name = f"identity-list-sub-{uuid.uuid4().hex[:8]}"

    client.user_assigned_identities.create_or_update(
        _RESOURCE_GROUP,
        identity_name,
        Identity(location="westeurope"),
    )

    identities = list(client.user_assigned_identities.list_by_subscription())
    names = [i.name for i in identities]
    assert identity_name in names


def test_identity_unique_properties():
    client = _msi_client()
    identity_name = f"identity-unique-{uuid.uuid4().hex[:8]}"

    result = client.user_assigned_identities.create_or_update(
        _RESOURCE_GROUP,
        identity_name,
        Identity(location="westeurope"),
    )

    assert result.client_id is not None
    assert result.principal_id is not None
    assert result.client_id != result.principal_id

    result2 = client.user_assigned_identities.create_or_update(
        _RESOURCE_GROUP,
        f"identity-unique2-{uuid.uuid4().hex[:8]}",
        Identity(location="westeurope"),
    )
    assert result2.client_id != result.client_id
    assert result2.principal_id != result.principal_id


def test_federated_credential_create():
    client = _msi_client()
    identity_name = f"identity-fic-create-{uuid.uuid4().hex[:8]}"
    fic_name = "fic-test-create"

    client.user_assigned_identities.create_or_update(
        _RESOURCE_GROUP,
        identity_name,
        Identity(location="westeurope"),
    )

    result = client.federated_identity_credentials.create_or_update(
        _RESOURCE_GROUP,
        identity_name,
        fic_name,
        FederatedIdentityCredential(
            issuer="https://token.actions.githubusercontent.com",
            subject="repo:myorg/myrepo:ref:refs/heads/main",
            audiences=["api://AzureADTokenExchange"],
        ),
    )
    assert result.name == fic_name
    assert result.issuer == "https://token.actions.githubusercontent.com"
    assert result.subject == "repo:myorg/myrepo:ref:refs/heads/main"


def test_federated_credential_delete():
    client = _msi_client()
    identity_name = f"identity-fic-del-{uuid.uuid4().hex[:8]}"
    fic_name = "fic-test-delete"

    client.user_assigned_identities.create_or_update(
        _RESOURCE_GROUP,
        identity_name,
        Identity(location="westeurope"),
    )
    client.federated_identity_credentials.create_or_update(
        _RESOURCE_GROUP,
        identity_name,
        fic_name,
        FederatedIdentityCredential(
            issuer="https://token.actions.githubusercontent.com",
            subject="repo:myorg/myrepo:ref:refs/heads/main",
            audiences=["api://AzureADTokenExchange"],
        ),
    )

    client.federated_identity_credentials.delete(_RESOURCE_GROUP, identity_name, fic_name)

    with pytest.raises(Exception):
        client.federated_identity_credentials.get(_RESOURCE_GROUP, identity_name, fic_name)


def test_federated_credentials_list():
    client = _msi_client()
    identity_name = f"identity-fic-list-{uuid.uuid4().hex[:8]}"

    client.user_assigned_identities.create_or_update(
        _RESOURCE_GROUP,
        identity_name,
        Identity(location="westeurope"),
    )

    fic_names = ["fic-list-a", "fic-list-b"]
    for fic_name in fic_names:
        client.federated_identity_credentials.create_or_update(
            _RESOURCE_GROUP,
            identity_name,
            fic_name,
            FederatedIdentityCredential(
                issuer="https://token.actions.githubusercontent.com",
                subject=f"repo:myorg/myrepo:ref:refs/heads/{fic_name}",
                audiences=["api://AzureADTokenExchange"],
            ),
        )

    fics = list(client.federated_identity_credentials.list(_RESOURCE_GROUP, identity_name))
    fic_listed_names = [f.name for f in fics]
    for name in fic_names:
        assert name in fic_listed_names
