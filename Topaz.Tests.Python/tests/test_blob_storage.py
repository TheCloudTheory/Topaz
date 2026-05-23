"""
Azure Blob Storage data-plane E2E tests — Python port of BlobStorageTests.cs.

Covers container CRUD, blob upload/download, properties, metadata, ACL,
container/blob leases, copy, block blobs, page blobs, snapshots, undelete,
and bearer-token authentication.
"""

import base64
import time
import uuid
from datetime import datetime, timedelta, timezone

import pytest
from azure.core.exceptions import HttpResponseError, ResourceNotFoundError
from azure.mgmt.storage import StorageManagementClient
from azure.mgmt.storage.models import StorageAccountCreateParameters, Sku
from azure.storage.blob import (
    AccessPolicy,
    BlobServiceClient,
    BlobType,
    ContentSettings,
)

from topaz_sdk import AzureLocalCredential, TopazArmClient, GLOBAL_ADMIN_ID
from topaz_sdk.helpers import DEFAULT_RESOURCE_MANAGER_PORT, TopazResourceHelpers

# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------

_SUBSCRIPTION_ID = "a0000004-0000-0000-0000-000000000004"
_SUBSCRIPTION_NAME = "py-sub-blob-test"
_RESOURCE_GROUP = "py-rg-blob-test"
_ACCOUNT_NAME = "pyblobstoretest"
_RM_BASE_URL = f"https://topaz.local.dev:{DEFAULT_RESOURCE_MANAGER_PORT}"

_KEY: str = ""


# ---------------------------------------------------------------------------
# Module-scoped setup: provision subscription, RG, and storage account once
# ---------------------------------------------------------------------------

@pytest.fixture(scope="module", autouse=True)
def blob_environment():
    global _KEY
    credential = AzureLocalCredential(GLOBAL_ADMIN_ID)
    with TopazArmClient(credential) as arm:
        arm.delete_subscription(_SUBSCRIPTION_ID)
        arm.create_subscription(_SUBSCRIPTION_ID, _SUBSCRIPTION_NAME)
        arm.create_resource_group(_SUBSCRIPTION_ID, _RESOURCE_GROUP)

    mgmt = StorageManagementClient(
        credential=credential,
        subscription_id=_SUBSCRIPTION_ID,
        base_url=_RM_BASE_URL,
        credential_scopes=[f"{_RM_BASE_URL}/.default"],
    )
    mgmt.storage_accounts.begin_create(
        _RESOURCE_GROUP,
        _ACCOUNT_NAME,
        StorageAccountCreateParameters(
            sku=Sku(name="Standard_LRS"),
            kind="StorageV2",
            location="westeurope",
        ),
    ).result()
    _KEY = mgmt.storage_accounts.list_keys(_RESOURCE_GROUP, _ACCOUNT_NAME)["keys"][0]["value"]
    yield


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _service() -> BlobServiceClient:
    conn = TopazResourceHelpers.get_storage_connection_string(_ACCOUNT_NAME, _KEY)
    return BlobServiceClient.from_connection_string(conn)


# ---------------------------------------------------------------------------
# Container CRUD
# ---------------------------------------------------------------------------

def test_blob_container_create_returns_name():
    svc = _service()
    container = svc.create_container("create-test")
    assert container.container_name == "create-test"


def test_blob_multiple_containers_are_listed():
    svc = _service()
    svc.create_container("list-test-1")
    svc.create_container("list-test-2")
    svc.create_container("list-test-3")
    names = [c["name"] for c in svc.list_containers()]
    assert "list-test-1" in names
    assert "list-test-2" in names
    assert "list-test-3" in names


# ---------------------------------------------------------------------------
# Blob upload / list / download
# ---------------------------------------------------------------------------

def test_blob_upload_and_list():
    svc = _service()
    svc.create_container("blobs-list-test")
    container = svc.get_container_client("blobs-list-test")
    container.get_blob_client("hello.txt").upload_blob(b"some content")
    blobs = list(container.list_blobs())
    assert len(blobs) == 1


# ---------------------------------------------------------------------------
# Properties
# ---------------------------------------------------------------------------

def test_blob_properties_content_type_and_length():
    svc = _service()
    svc.create_container("blob-props-ct-test")
    blob = svc.get_container_client("blob-props-ct-test").get_blob_client("hello.txt")
    blob.upload_blob(
        b"hello world",
        overwrite=True,
        content_settings=ContentSettings(content_type="text/plain"),
    )
    props = blob.get_blob_properties()
    assert props.content_settings.content_type == "text/plain"
    assert props.size > 0
    assert props.blob_type == "BlockBlob"
    assert props.last_modified is not None
    assert props.etag is not None


def test_blob_properties_http_headers_set_and_retrieved():
    svc = _service()
    svc.create_container("set-props-test")
    blob = svc.get_container_client("set-props-test").get_blob_client("hello.txt")
    blob.upload_blob(b"hello world", overwrite=True)
    blob.set_http_headers(
        content_settings=ContentSettings(
            content_type="text/plain",
            content_encoding="utf-8",
            content_language="en-US",
            cache_control="max-age=3600",
            content_disposition="inline",
        )
    )
    props = blob.get_blob_properties()
    assert props.content_settings.content_type == "text/plain"
    assert props.content_settings.content_encoding == "utf-8"
    assert props.content_settings.content_language == "en-US"
    assert props.content_settings.cache_control == "max-age=3600"
    assert props.content_settings.content_disposition == "inline"


def test_blob_properties_not_found_raises():
    svc = _service()
    svc.create_container("blob-nf-test")
    blob = svc.get_container_client("blob-nf-test").get_blob_client("notexisting.txt")
    with pytest.raises(ResourceNotFoundError):
        blob.get_blob_properties()


# ---------------------------------------------------------------------------
# Metadata
# ---------------------------------------------------------------------------

def test_blob_metadata_set_and_retrieved():
    svc = _service()
    svc.create_container("blob-meta-test")
    blob = svc.get_container_client("blob-meta-test").get_blob_client("file.txt")
    blob.upload_blob(b"content", overwrite=True)
    blob.set_blob_metadata({"env": "prod", "version": "42"})
    props = blob.get_blob_properties()
    assert props.metadata["env"] == "prod"
    assert props.metadata["version"] == "42"


def test_container_metadata_set_and_retrieved():
    svc = _service()
    svc.create_container("cont-meta-test")
    container = svc.get_container_client("cont-meta-test")
    container.set_container_metadata({"env": "prod", "owner": "team-a"})
    props = container.get_container_properties()
    assert props.metadata["env"] == "prod"
    assert props.metadata["owner"] == "team-a"


# ---------------------------------------------------------------------------
# Container ACL
# ---------------------------------------------------------------------------

def test_container_acl_initially_empty():
    svc = _service()
    svc.create_container("acl-empty-test")
    container = svc.get_container_client("acl-empty-test")
    policy = container.get_container_access_policy()
    assert len(policy["signed_identifiers"]) == 0


def test_container_acl_set_and_retrieved():
    svc = _service()
    svc.create_container("acl-set-test")
    container = svc.get_container_client("acl-set-test")
    container.set_container_access_policy(
        signed_identifiers={
            "pol1": AccessPolicy(
                start=datetime.now(tz=timezone.utc) - timedelta(minutes=1),
                expiry=datetime.now(tz=timezone.utc) + timedelta(hours=1),
                permission="r",
            )
        }
    )
    policy = container.get_container_access_policy()
    sids = policy["signed_identifiers"]
    assert len(sids) == 1
    assert sids[0].id == "pol1"
    assert sids[0].access_policy.permission == "r"


# ---------------------------------------------------------------------------
# Container leases
# ---------------------------------------------------------------------------

def test_container_lease_acquire():
    svc = _service()
    svc.create_container("lease-acq-test")
    container = svc.get_container_client("lease-acq-test")
    lease = container.acquire_lease(lease_duration=30)
    assert lease.id is not None and lease.id != ""


def test_container_lease_renew():
    svc = _service()
    svc.create_container("lease-renew-test")
    container = svc.get_container_client("lease-renew-test")
    lease = container.acquire_lease(lease_duration=30)
    lease.renew()  # must not raise


def test_container_lease_change_returns_new_id():
    svc = _service()
    svc.create_container("lease-change-test")
    container = svc.get_container_client("lease-change-test")
    lease = container.acquire_lease(lease_duration=30)
    new_id = str(uuid.uuid4())
    lease.change(proposed_lease_id=new_id)
    assert lease.id == new_id


def test_container_lease_release():
    svc = _service()
    svc.create_container("lease-rel-test")
    container = svc.get_container_client("lease-rel-test")
    lease = container.acquire_lease(lease_duration=30)
    lease.release()  # must not raise


def test_container_lease_break():
    svc = _service()
    svc.create_container("lease-brk-test")
    container = svc.get_container_client("lease-brk-test")
    lease = container.acquire_lease(lease_duration=30)
    lease.break_lease()  # must not raise


def test_container_lease_conflict_returns_409():
    svc = _service()
    svc.create_container("lease-conflict-test")
    container = svc.get_container_client("lease-conflict-test")
    container.acquire_lease(lease_duration=30)
    with pytest.raises(HttpResponseError) as exc_info:
        container.acquire_lease(lease_duration=30)
    assert exc_info.value.status_code == 409


# ---------------------------------------------------------------------------
# Blob copy
# ---------------------------------------------------------------------------

def test_blob_copy_destination_has_same_content():
    svc = _service()
    svc.create_container("copy-src")
    svc.create_container("copy-dst")
    src = svc.get_container_client("copy-src").get_blob_client("original.txt")
    src.upload_blob(b"hello copy world", overwrite=True)
    dst = svc.get_container_client("copy-dst").get_blob_client("copied.txt")
    dst.start_copy_from_url(src.url)
    # Topaz completes copies synchronously
    assert dst.download_blob().readall() == b"hello copy world"


# ---------------------------------------------------------------------------
# Block blobs
# ---------------------------------------------------------------------------

def test_block_blob_stage_block_succeeds():
    svc = _service()
    svc.create_container("block-stage-test")
    blob = svc.get_container_client("block-stage-test").get_blob_client("staged.txt")
    block_id = base64.b64encode(b"block-001").decode()
    blob.stage_block(block_id, b"hello block world")  # must not raise


def test_block_blob_commit_and_download():
    svc = _service()
    svc.create_container("blocklist-test")
    blob = svc.get_container_client("blocklist-test").get_blob_client("assembled.txt")
    chunks = [b"Hello, ", b"block ", b"world!"]
    ids = [base64.b64encode(f"blk-{i:03d}".encode()).decode() for i in range(len(chunks))]
    for bid, content in zip(ids, chunks):
        blob.stage_block(bid, content)
    blob.commit_block_list(ids)
    assert blob.download_blob().readall() == b"Hello, block world!"


def test_block_blob_get_uncommitted_block_list():
    svc = _service()
    svc.create_container("bl-uncommitted-test")
    blob = svc.get_container_client("bl-uncommitted-test").get_blob_client("staged.txt")
    id1 = base64.b64encode(b"block-a").decode()
    id2 = base64.b64encode(b"block-b").decode()
    blob.stage_block(id1, b"hello ")
    blob.stage_block(id2, b"world")
    result = blob.get_block_list("uncommitted")
    # get_block_list returns (committed_list, uncommitted_list) tuple
    uncommitted = list(result[1])
    assert len(uncommitted) == 2
    names = [b.id for b in uncommitted]
    assert id1 in names
    assert id2 in names


def test_block_blob_get_committed_block_list():
    svc = _service()
    svc.create_container("bl-committed-test")
    blob = svc.get_container_client("bl-committed-test").get_blob_client("committed.txt")
    chunks = [b"Hello, ", b"block ", b"world!"]
    ids = [base64.b64encode(f"blk-{i:03d}".encode()).decode() for i in range(len(chunks))]
    for bid, content in zip(ids, chunks):
        blob.stage_block(bid, content)
    blob.commit_block_list(ids)
    result = blob.get_block_list("committed")
    # get_block_list returns (committed_list, uncommitted_list) tuple
    committed = list(result[0])
    assert len(committed) == 3
    assert [b.id for b in committed] == ids


def test_block_blob_get_all_block_list_shows_both():
    svc = _service()
    svc.create_container("bl-all-test")
    blob = svc.get_container_client("bl-all-test").get_blob_client("mixed.txt")
    committed_id = base64.b64encode(b"committed-blk").decode()
    uncommitted_id = base64.b64encode(b"uncommitted-blk").decode()
    blob.stage_block(committed_id, b"committed content")
    blob.commit_block_list([committed_id])
    blob.stage_block(uncommitted_id, b"pending content")
    result = blob.get_block_list("all")
    assert len(list(result[0])) == 1
    assert len(list(result[1])) == 1


# ---------------------------------------------------------------------------
# Page blobs
# ---------------------------------------------------------------------------

def test_page_blob_create_and_properties():
    svc = _service()
    svc.create_container("page-create-test")
    blob = svc.get_container_client("page-create-test").get_blob_client("page.bin")
    blob.create_page_blob(size=512)
    props = blob.get_blob_properties()
    assert props.blob_type == "PageBlob"
    assert props.size == 512


def test_page_blob_upload_and_download():
    svc = _service()
    svc.create_container("page-upload-test")
    blob = svc.get_container_client("page-upload-test").get_blob_client("page.bin")
    content = bytes(i % 256 for i in range(512))
    blob.create_page_blob(size=512)
    blob.upload_page(content, offset=0, length=512)
    assert blob.download_blob().readall() == content


def test_page_blob_get_page_ranges():
    svc = _service()
    svc.create_container("page-ranges-test")
    blob = svc.get_container_client("page-ranges-test").get_blob_client("ranges.bin")
    blob.create_page_blob(size=1024)
    blob.upload_page(bytes(512), offset=0, length=512)
    blob.upload_page(bytes(512), offset=512, length=512)
    response = blob.get_page_ranges()
    # get_page_ranges returns (page_ranges_list, clear_ranges_list) tuple
    ranges = list(response[0])
    assert len(ranges) == 1
    assert ranges[0]["start"] == 0
    assert ranges[0]["end"] == 1023


def test_page_blob_clear_page_removes_range():
    svc = _service()
    svc.create_container("page-clear-test")
    blob = svc.get_container_client("page-clear-test").get_blob_client("clear.bin")
    blob.create_page_blob(size=1024)
    blob.upload_page(bytes(512), offset=0, length=512)
    blob.upload_page(bytes(512), offset=512, length=512)
    blob.clear_page(offset=0, length=512)
    response = blob.get_page_ranges()
    # get_page_ranges returns (page_ranges_list, clear_ranges_list) tuple
    ranges = list(response[0])
    assert len(ranges) == 1
    assert ranges[0]["start"] == 512


# ---------------------------------------------------------------------------
# Blob leases
# ---------------------------------------------------------------------------

def test_blob_lease_acquire():
    svc = _service()
    svc.create_container("blob-lease-acq")
    blob = svc.get_container_client("blob-lease-acq").get_blob_client("lease.txt")
    blob.upload_blob(b"hello", overwrite=True)
    lease = blob.acquire_lease(lease_duration=30)
    assert lease.id is not None and lease.id != ""


def test_blob_lease_conflict_returns_409():
    svc = _service()
    svc.create_container("blob-lease-conf")
    blob = svc.get_container_client("blob-lease-conf").get_blob_client("lease.txt")
    blob.upload_blob(b"hello", overwrite=True)
    blob.acquire_lease(lease_duration=30)
    with pytest.raises(HttpResponseError) as exc_info:
        blob.acquire_lease(lease_duration=30)
    assert exc_info.value.status_code == 409


# ---------------------------------------------------------------------------
# Snapshots
# ---------------------------------------------------------------------------

def test_blob_snapshot_create_returns_timestamp():
    svc = _service()
    svc.create_container("snap-create-test")
    blob = svc.get_container_client("snap-create-test").get_blob_client("snap.txt")
    blob.upload_blob(b"snapshot me", overwrite=True)
    response = blob.create_snapshot()
    assert response["snapshot"] is not None and response["snapshot"] != ""


def test_blob_snapshot_multiple_have_unique_timestamps():
    svc = _service()
    svc.create_container("snap-multi-test")
    blob = svc.get_container_client("snap-multi-test").get_blob_client("snap.txt")
    blob.upload_blob(b"multi snapshot", overwrite=True)
    snap1 = blob.create_snapshot()["snapshot"]
    time.sleep(0.01)
    snap2 = blob.create_snapshot()["snapshot"]
    assert snap1 != snap2


def test_blob_snapshot_not_found_raises():
    svc = _service()
    svc.create_container("snap-nf-test")
    blob = svc.get_container_client("snap-nf-test").get_blob_client("nonexistent.txt")
    with pytest.raises(HttpResponseError) as exc_info:
        blob.create_snapshot()
    assert exc_info.value.status_code == 404


# ---------------------------------------------------------------------------
# Delete and undelete
# ---------------------------------------------------------------------------

def test_blob_undelete_restores_blob():
    svc = _service()
    svc.create_container("undelete-test")
    blob = svc.get_container_client("undelete-test").get_blob_client("restore.txt")
    blob.upload_blob(b"restore me", overwrite=True)
    blob.delete_blob()
    assert not blob.exists()
    blob.undelete_blob()
    assert blob.exists()
    assert blob.download_blob().readall() == b"restore me"


def test_blob_undelete_on_active_blob_raises():
    svc = _service()
    svc.create_container("undelete-active-test")
    blob = svc.get_container_client("undelete-active-test").get_blob_client("active.txt")
    blob.upload_blob(b"still here", overwrite=True)
    with pytest.raises(HttpResponseError) as exc_info:
        blob.undelete_blob()
    assert exc_info.value.status_code == 404


# ---------------------------------------------------------------------------
# Bearer token authentication
# ---------------------------------------------------------------------------

def test_blob_bearer_auth_container_create():
    credential = AzureLocalCredential(GLOBAL_ADMIN_ID)
    svc = BlobServiceClient(
        TopazResourceHelpers.get_blob_service_uri(_ACCOUNT_NAME),
        credential=credential,
    )
    container = svc.create_container("token-auth-test")
    assert container.container_name == "token-auth-test"


def test_blob_bearer_auth_upload_and_download():
    credential = AzureLocalCredential(GLOBAL_ADMIN_ID)
    svc = BlobServiceClient(
        TopazResourceHelpers.get_blob_service_uri(_ACCOUNT_NAME),
        credential=credential,
    )
    svc.create_container("token-auth-blob-test")
    blob = svc.get_container_client("token-auth-blob-test").get_blob_client("hello.txt")
    blob.upload_blob(b"hello from token auth", overwrite=True)
    assert blob.download_blob().readall() == b"hello from token auth"


# ---------------------------------------------------------------------------
# Public access levels
# ---------------------------------------------------------------------------

def test_container_public_access_container_level():
    svc = _service()
    svc.create_container("anon-container-props", public_access="container")
    props = svc.get_container_client("anon-container-props").get_container_properties()
    assert props.public_access == "container"


def test_container_public_access_blob_level():
    svc = _service()
    svc.create_container("anon-blob-props", public_access="blob")
    props = svc.get_container_client("anon-blob-props").get_container_properties()
    assert props.public_access == "blob"
