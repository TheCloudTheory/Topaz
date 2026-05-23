"""
Azure Container Registry ARM + data-plane E2E tests.

Python port of Topaz.Tests/E2E/ContainerRegistryTests.cs.

Covers:
  - ARM: registry CRUD, SKU tiers, managed identity, credentials,
    credential regeneration, token generation, usages
  - Data-plane (OCI HTTP API): catalog listing, tag listing + pagination,
    manifest push/get/delete (by tag and by digest), HEAD manifests,
    blob upload and delete
"""

from datetime import datetime, timedelta, timezone
import hashlib
import json

import pytest
import requests
from azure.core.exceptions import HttpResponseError
from azure.mgmt.containerregistry import ContainerRegistryManagementClient
from azure.mgmt.containerregistry.models import (
    RegenerateCredentialParameters,
    GenerateCredentialsParameters,
    IdentityProperties,
    PasswordName,
    Registry,
    ResourceIdentityType,
    Sku,
    TokenPasswordName,
)

from topaz_sdk import AzureLocalCredential, TopazArmClient, GLOBAL_ADMIN_ID
from topaz_sdk.helpers import (
    CONTAINER_REGISTRY_PORT,
    DEFAULT_RESOURCE_MANAGER_PORT,
    TopazResourceHelpers,
)

# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------

_SUBSCRIPTION_ID = "a0000007-0000-0000-0000-000000000007"
_SUBSCRIPTION_NAME = "py-sub-acr-test"
_RESOURCE_GROUP = "py-rg-acr-test"
_RM_BASE_URL = f"https://topaz.local.dev:{DEFAULT_RESOURCE_MANAGER_PORT}"

# Shared registry name for ARM-only tests
_REGISTRY_NAME = "pyacrtest01"

# Unique names for data-plane tests that need clean catalog state
_REG_EMPTY = "pyacrempty"
_REG_PUSH = "pyacrpush"
_REG_TAGS = "pyacrtags01"
_REG_PAGINATE = "pyacrpaginate"
_REG_DEL_MFST = "pyacrdelmfst"
_REG_DEL_DGST = "pyacrdeldgst"
_REG_DEL_NF = "pyacrdelnotfnd"
_REG_HEAD = "pyacrhead01"
_REG_HEAD_DIG = "pyacrheaddig"
_REG_HEAD_NF = "pyacrheadnf"
_REG_BLOB = "pyacrblob01"
_REG_IDENTITY = "pyacrmgid01"

_MINIMAL_MANIFEST = json.dumps({
    "schemaVersion": 2,
    "mediaType": "application/vnd.docker.distribution.manifest.v2+json",
    "config": {
        "mediaType": "application/vnd.docker.container.image.v1+json",
        "size": 0,
        "digest": "sha256:e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
    },
    "layers": [],
})
_MANIFEST_CT = "application/vnd.docker.distribution.manifest.v2+json"

# ---------------------------------------------------------------------------
# Module-scoped setup: subscription and resource group created once
# ---------------------------------------------------------------------------

@pytest.fixture(scope="module", autouse=True)
def acr_environment():
    credential = AzureLocalCredential(GLOBAL_ADMIN_ID)
    with TopazArmClient(credential) as arm:
        arm.delete_subscription(_SUBSCRIPTION_ID)
        arm.create_subscription(_SUBSCRIPTION_ID, _SUBSCRIPTION_NAME)
        arm.create_resource_group(_SUBSCRIPTION_ID, _RESOURCE_GROUP)
    yield


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _mgmt() -> ContainerRegistryManagementClient:
    return ContainerRegistryManagementClient(
        credential=AzureLocalCredential(GLOBAL_ADMIN_ID),
        subscription_id=_SUBSCRIPTION_ID,
        base_url=_RM_BASE_URL,
        credential_scopes=[f"{_RM_BASE_URL}/.default"],
    )


def _acr_url(path: str, registry_name: str = _REGISTRY_NAME) -> str:
    host = TopazResourceHelpers.get_container_registry_login_server(registry_name)
    return f"https://{host}{path}"


def _push_manifest(
    repo: str,
    tag: str,
    manifest: str = _MINIMAL_MANIFEST,
    registry_name: str = _REGISTRY_NAME,
) -> requests.Response:
    return requests.put(
        _acr_url(f"/v2/{repo}/manifests/{tag}", registry_name),
        data=manifest.encode(),
        headers={"Content-Type": _MANIFEST_CT},
        timeout=10,
    )


def _create_registry(registry_name: str, sku: str = "Basic", **kwargs) -> Registry:
    mgmt = _mgmt()
    return mgmt.registries.begin_create(
        _RESOURCE_GROUP,
        registry_name,
        Registry(location="westeurope", sku=Sku(name=sku), **kwargs),
    ).result()


# ---------------------------------------------------------------------------
# ARM — Registry CRUD
# ---------------------------------------------------------------------------

def test_registry_create_and_get():
    _create_registry(_REGISTRY_NAME)
    registry = _mgmt().registries.get(_RESOURCE_GROUP, _REGISTRY_NAME)
    assert registry.name == _REGISTRY_NAME
    assert registry.login_server
    assert registry.sku.name == "Basic"


def test_registry_update_admin_user_enabled():
    _create_registry(_REGISTRY_NAME, admin_user_enabled=False)
    assert _mgmt().registries.get(_RESOURCE_GROUP, _REGISTRY_NAME).admin_user_enabled is False

    _create_registry(_REGISTRY_NAME, admin_user_enabled=True)
    assert _mgmt().registries.get(_RESOURCE_GROUP, _REGISTRY_NAME).admin_user_enabled is True


def test_registry_delete():
    _create_registry(_REGISTRY_NAME)
    _mgmt().registries.begin_delete(_RESOURCE_GROUP, _REGISTRY_NAME).result()
    with pytest.raises(HttpResponseError):
        _mgmt().registries.get(_RESOURCE_GROUP, _REGISTRY_NAME)


def test_registry_list_by_resource_group():
    _create_registry(_REGISTRY_NAME)
    names = [r.name for r in _mgmt().registries.list_by_resource_group(_RESOURCE_GROUP)]
    assert _REGISTRY_NAME in names


def test_registry_premium_sku():
    _create_registry(_REGISTRY_NAME, sku="Premium")
    registry = _mgmt().registries.get(_RESOURCE_GROUP, _REGISTRY_NAME)
    assert registry.sku.name == "Premium"


def test_registry_system_assigned_identity():
    _create_registry(
        _REG_IDENTITY,
        identity=IdentityProperties(type=ResourceIdentityType.SYSTEM_ASSIGNED),
    )
    registry = _mgmt().registries.get(_RESOURCE_GROUP, _REG_IDENTITY)
    assert registry.identity is not None
    assert registry.identity.principal_id is not None
    assert registry.identity.tenant_id is not None


# ---------------------------------------------------------------------------
# ARM — Credentials
# ---------------------------------------------------------------------------

def test_registry_list_credentials_admin_enabled():
    _create_registry(_REGISTRY_NAME, admin_user_enabled=True)
    creds = _mgmt().registries.list_credentials(_RESOURCE_GROUP, _REGISTRY_NAME)
    assert creds.username == _REGISTRY_NAME
    assert creds.passwords and len(creds.passwords) > 0
    assert creds.passwords[0].value


def test_registry_list_credentials_admin_disabled_fails():
    _create_registry(_REGISTRY_NAME, admin_user_enabled=False)
    with pytest.raises(HttpResponseError):
        _mgmt().registries.list_credentials(_RESOURCE_GROUP, _REGISTRY_NAME)


def test_registry_list_credentials_after_enabling_admin():
    _create_registry(_REGISTRY_NAME, admin_user_enabled=False)
    _create_registry(_REGISTRY_NAME, admin_user_enabled=True)
    creds = _mgmt().registries.list_credentials(_RESOURCE_GROUP, _REGISTRY_NAME)
    assert creds.username == _REGISTRY_NAME
    assert creds.passwords and creds.passwords[0].value


def test_registry_regenerate_credential_password():
    _create_registry(_REGISTRY_NAME, admin_user_enabled=True)
    original = _mgmt().registries.list_credentials(_RESOURCE_GROUP, _REGISTRY_NAME)
    original_pw = next(p for p in original.passwords if p.name == PasswordName.PASSWORD).value

    result = _mgmt().registries.regenerate_credential(
        _RESOURCE_GROUP, _REGISTRY_NAME,
        RegenerateCredentialParameters(name=PasswordName.PASSWORD),
    )
    new_pw = next(p for p in result.passwords if p.name == PasswordName.PASSWORD).value
    assert result.username == _REGISTRY_NAME
    assert new_pw != original_pw


def test_registry_regenerate_credential_password2():
    _create_registry(_REGISTRY_NAME, admin_user_enabled=True)
    original = _mgmt().registries.list_credentials(_RESOURCE_GROUP, _REGISTRY_NAME)
    original_pw2 = next(p for p in original.passwords if p.name == PasswordName.PASSWORD2).value

    result = _mgmt().registries.regenerate_credential(
        _RESOURCE_GROUP, _REGISTRY_NAME,
        RegenerateCredentialParameters(name=PasswordName.PASSWORD2),
    )
    new_pw2 = next(p for p in result.passwords if p.name == PasswordName.PASSWORD2).value
    assert new_pw2 != original_pw2


def test_registry_regenerate_credential_admin_disabled_fails():
    _create_registry(_REGISTRY_NAME, admin_user_enabled=False)
    with pytest.raises(HttpResponseError):
        _mgmt().registries.regenerate_credential(
            _RESOURCE_GROUP, _REGISTRY_NAME,
            RegenerateCredentialParameters(name=PasswordName.PASSWORD),
        )


# ---------------------------------------------------------------------------
# ARM — Token credential generation
# ---------------------------------------------------------------------------

def _token_id(registry_name: str = _REGISTRY_NAME, token_name: str = "myToken") -> str:
    return (
        f"/subscriptions/{_SUBSCRIPTION_ID}/resourceGroups/{_RESOURCE_GROUP}"
        f"/providers/Microsoft.ContainerRegistry/registries/{registry_name}/tokens/{token_name}"
    )


def test_registry_generate_credentials():
    _create_registry(_REGISTRY_NAME)
    result = _mgmt().registries.begin_generate_credentials(
        _RESOURCE_GROUP, _REGISTRY_NAME,
        GenerateCredentialsParameters(
            token_id=_token_id(),
            expiry=datetime.now(tz=timezone.utc) + timedelta(days=30),
        ),
    ).result()
    assert result.username == "myToken"
    assert result.passwords and len(result.passwords) > 0
    assert result.passwords[0].value


def test_registry_generate_credentials_specific_password_name():
    _create_registry(_REGISTRY_NAME)
    result = _mgmt().registries.begin_generate_credentials(
        _RESOURCE_GROUP, _REGISTRY_NAME,
        GenerateCredentialsParameters(
            token_id=_token_id(),
            name=TokenPasswordName.PASSWORD1,
        ),
    ).result()
    assert result.username == "myToken"
    assert len(result.passwords) == 1
    assert result.passwords[0].name == TokenPasswordName.PASSWORD1


def test_registry_generate_credentials_nonexistent_registry_fails():
    with pytest.raises(HttpResponseError):
        _mgmt().registries.begin_generate_credentials(
            _RESOURCE_GROUP, "nonexistentregistry99",
            GenerateCredentialsParameters(token_id=_token_id("nonexistentregistry99")),
        ).result()


# ---------------------------------------------------------------------------
# ARM — Usages
# ---------------------------------------------------------------------------

def test_registry_list_usages_basic_sku():
    _create_registry(_REGISTRY_NAME, sku="Basic")
    usages = _mgmt().registries.list_usages(_RESOURCE_GROUP, _REGISTRY_NAME).value
    size = next((u for u in usages if u.name == "Size"), None)
    webhooks = next((u for u in usages if u.name == "Webhooks"), None)
    assert size is not None and size.limit == 10_737_418_240 and size.current_value == 0
    assert webhooks is not None and webhooks.limit == 2


def test_registry_list_usages_premium_sku():
    _create_registry(_REGISTRY_NAME, sku="Premium")
    usages = _mgmt().registries.list_usages(_RESOURCE_GROUP, _REGISTRY_NAME).value
    size = next((u for u in usages if u.name == "Size"), None)
    webhooks = next((u for u in usages if u.name == "Webhooks"), None)
    assert size is not None and size.limit == 536_870_912_000
    assert webhooks is not None and webhooks.limit == 500


# ---------------------------------------------------------------------------
# Data-plane — Catalog
# ---------------------------------------------------------------------------

def test_catalog_empty_for_fresh_registry():
    _create_registry(_REG_EMPTY)
    resp = requests.get(_acr_url("/v2/_catalog", _REG_EMPTY), timeout=10)
    assert resp.status_code == 200
    assert len(resp.json()["repositories"]) == 0


def test_catalog_lists_pushed_repository():
    _create_registry(_REG_PUSH)
    assert _push_manifest("my-app", "v1", registry_name=_REG_PUSH).status_code == 201
    resp = requests.get(_acr_url("/v2/_catalog", _REG_PUSH), timeout=10)
    assert resp.status_code == 200
    assert "my-app" in resp.json()["repositories"]


# ---------------------------------------------------------------------------
# Data-plane — Tags
# ---------------------------------------------------------------------------

def test_tags_list_returns_pushed_tags():
    _create_registry(_REG_TAGS)
    for tag in ("v1", "v2", "latest"):
        assert _push_manifest("tag-test-app", tag, registry_name=_REG_TAGS).status_code == 201

    resp = requests.get(_acr_url("/v2/tag-test-app/tags/list", _REG_TAGS), timeout=10)
    assert resp.status_code == 200
    tags = resp.json()["tags"]
    assert set(tags) == {"v1", "v2", "latest"}


def test_tags_list_pagination():
    _create_registry(_REG_PAGINATE)
    for tag in ("v1", "v2", "v3", "v4", "v5"):
        _push_manifest("pagination-app", tag, registry_name=_REG_PAGINATE)

    page1 = requests.get(
        _acr_url("/v2/pagination-app/tags/list?n=2", _REG_PAGINATE), timeout=10
    ).json()["tags"]
    page2 = requests.get(
        _acr_url("/v2/pagination-app/tags/list?n=2&last=v2", _REG_PAGINATE), timeout=10
    ).json()["tags"]

    assert page1 == ["v1", "v2"]
    assert page2 == ["v3", "v4"]


# ---------------------------------------------------------------------------
# Data-plane — Manifests
# ---------------------------------------------------------------------------

def test_manifest_delete_by_tag():
    _create_registry(_REG_DEL_MFST)
    assert _push_manifest("delete-manifest-app", "v1", registry_name=_REG_DEL_MFST).status_code == 201

    del_resp = requests.delete(_acr_url("/v2/delete-manifest-app/manifests/v1", _REG_DEL_MFST), timeout=10)
    assert del_resp.status_code == 202

    get_resp = requests.get(_acr_url("/v2/delete-manifest-app/manifests/v1", _REG_DEL_MFST), timeout=10)
    assert get_resp.status_code == 404


def test_manifest_delete_by_digest_removes_only_that_tag():
    _create_registry(_REG_DEL_DGST)
    manifest_v2 = json.dumps({
        "schemaVersion": 2,
        "mediaType": _MANIFEST_CT,
        "config": {
            "mediaType": "application/vnd.docker.container.image.v1+json",
            "size": 0,
            "digest": "sha256:f3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
        },
        "layers": [],
    })

    resp_v1 = _push_manifest("del-by-digest-app", "v1", registry_name=_REG_DEL_DGST)
    assert resp_v1.status_code == 201
    digest_v1 = resp_v1.headers["Docker-Content-Digest"]

    resp_v2 = requests.put(
        _acr_url("/v2/del-by-digest-app/manifests/v2", _REG_DEL_DGST),
        data=manifest_v2.encode(),
        headers={"Content-Type": _MANIFEST_CT},
        timeout=10,
    )
    assert resp_v2.status_code == 201

    del_resp = requests.delete(
        _acr_url(f"/v2/del-by-digest-app/manifests/{digest_v1}", _REG_DEL_DGST), timeout=10
    )
    assert del_resp.status_code == 202

    tags = requests.get(
        _acr_url("/v2/del-by-digest-app/tags/list", _REG_DEL_DGST), timeout=10
    ).json()["tags"]
    assert "v1" not in tags
    assert "v2" in tags


def test_manifest_delete_not_found():
    _create_registry(_REG_DEL_NF)
    resp = requests.delete(_acr_url("/v2/nonexistent-repo/manifests/v1", _REG_DEL_NF), timeout=10)
    assert resp.status_code == 404


def test_manifest_head_existing():
    _create_registry(_REG_HEAD)
    assert _push_manifest("head-manifest-app", "v1", registry_name=_REG_HEAD).status_code == 201

    resp = requests.head(_acr_url("/v2/head-manifest-app/manifests/v1", _REG_HEAD), timeout=10)
    assert resp.status_code == 200
    assert "Docker-Content-Digest" in resp.headers
    assert int(resp.headers.get("Content-Length", 0)) > 0
    assert _MANIFEST_CT in resp.headers.get("Content-Type", "")
    assert len(resp.content) == 0  # HEAD must have empty body


def test_manifest_head_by_digest():
    _create_registry(_REG_HEAD_DIG)
    put_resp = _push_manifest("head-by-digest-app", "v1", registry_name=_REG_HEAD_DIG)
    assert put_resp.status_code == 201
    digest = put_resp.headers["Docker-Content-Digest"]

    resp = requests.head(
        _acr_url(f"/v2/head-by-digest-app/manifests/{digest}", _REG_HEAD_DIG), timeout=10
    )
    assert resp.status_code == 200
    assert resp.headers["Docker-Content-Digest"] == digest


def test_manifest_head_not_found():
    _create_registry(_REG_HEAD_NF)
    resp = requests.head(_acr_url("/v2/nonexistent-repo/manifests/v1", _REG_HEAD_NF), timeout=10)
    assert resp.status_code == 404


# ---------------------------------------------------------------------------
# Data-plane — Blobs
# ---------------------------------------------------------------------------

def test_blob_upload_and_delete():
    _create_registry(_REG_BLOB)
    payload = b"delete-blob-payload"
    digest = "sha256:" + hashlib.sha256(payload).hexdigest()

    # Initiate upload session
    init_resp = requests.post(_acr_url("/v2/delete-blob-app/blobs/uploads/", _REG_BLOB), timeout=10)
    assert init_resp.status_code == 202
    upload_url = init_resp.headers["Location"]

    # Complete upload with full payload
    complete_resp = requests.put(
        f"{upload_url}?digest={digest}",
        data=payload,
        headers={"Content-Type": "application/octet-stream"},
        timeout=10,
    )
    assert complete_resp.status_code == 201

    # Delete the blob
    del_resp = requests.delete(
        _acr_url(f"/v2/delete-blob-app/blobs/{digest}", _REG_BLOB), timeout=10
    )
    assert del_resp.status_code == 202

    # Verify it is gone
    head_resp = requests.head(
        _acr_url(f"/v2/delete-blob-app/blobs/{digest}", _REG_BLOB), timeout=10
    )
    assert head_resp.status_code == 404
