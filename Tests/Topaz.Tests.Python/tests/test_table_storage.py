"""
Azure Table Storage data-plane E2E tests — Python port of TableStorageTests.cs.

Covers table CRUD, entity CRUD with ETag concurrency, update modes, service
properties, access policies, and geo-replication stats.
"""

from datetime import datetime, timedelta, timezone
import base64
import hashlib
import hmac
import urllib.parse

import pytest
from azure.core.exceptions import HttpResponseError, ResourceExistsError, ResourceNotFoundError
from azure.core import MatchConditions
from azure.core.credentials import AzureNamedKeyCredential
from azure.data.tables import TableAccessPolicy, TableServiceClient, UpdateMode

from azure.mgmt.storage import StorageManagementClient
from azure.mgmt.storage.models import StorageAccountCreateParameters, Sku

from topaz_sdk import AzureLocalCredential, TopazArmClient, GLOBAL_ADMIN_ID
from topaz_sdk.helpers import DEFAULT_RESOURCE_MANAGER_PORT, DEFAULT_TABLE_STORAGE_PORT, TopazResourceHelpers

# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------

_SUBSCRIPTION_ID = "a0000005-0000-0000-0000-000000000005"
_SUBSCRIPTION_NAME = "py-sub-table-test"
_RESOURCE_GROUP = "py-rg-table-test"
_ACCOUNT_NAME = "pytablestortest"
_RAGRS_ACCOUNT_NAME = "pytblragrstest"
_RM_BASE_URL = f"https://topaz.local.dev:{DEFAULT_RESOURCE_MANAGER_PORT}"

_KEY: str = ""


# ---------------------------------------------------------------------------
# Module-scoped setup
# ---------------------------------------------------------------------------

@pytest.fixture(scope="module", autouse=True)
def table_environment():
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

def _svc() -> TableServiceClient:
    conn = TopazResourceHelpers.get_storage_connection_string(_ACCOUNT_NAME, _KEY)
    return TableServiceClient.from_connection_string(conn)


def _entity(pk: str, rk: str, name: str) -> dict:
    return {"PartitionKey": pk, "RowKey": rk, "Name": name}


# ---------------------------------------------------------------------------
# Table CRUD
# ---------------------------------------------------------------------------

def test_table_create_single_and_list():
    svc = _svc()
    svc.create_table_if_not_exists("tblisttest")
    names = [t.name for t in svc.query_tables("")]
    assert "tblisttest" in names


def test_table_create_and_delete():
    svc = _svc()
    svc.create_table("tbcreatedel")
    names = [t.name for t in svc.query_tables("")]
    assert "tbcreatedel" in names
    svc.delete_table("tbcreatedel")
    names_after = [t.name for t in svc.query_tables("")]
    assert "tbcreatedel" not in names_after


def test_table_create_if_not_exists_is_idempotent():
    svc = _svc()
    table_name = "tbidemptest"
    svc.create_table_if_not_exists(table_name)
    table = svc.get_table_client(table_name)
    table.create_entity(_entity("test", "1", "foo"))
    svc.create_table_if_not_exists(table_name)  # must not raise or reset
    table = svc.get_table_client(table_name)
    entities = list(table.query_entities(""))
    assert any(e["RowKey"] == "1" for e in entities)


# ---------------------------------------------------------------------------
# Entity CRUD
# ---------------------------------------------------------------------------

def test_entity_insert_and_query():
    svc = _svc()
    svc.create_table_if_not_exists("tbinserttest")
    table = svc.get_table_client("tbinserttest")
    table.create_entity(_entity("test", "1", "foo"))
    entities = list(table.query_entities("PartitionKey eq 'test'"))
    assert len(entities) == 1


def test_entity_insert_multiple_and_query_ordered():
    svc = _svc()
    svc.create_table_if_not_exists("tbmultitest")
    table = svc.get_table_client("tbmultitest")
    for i in range(1, 4):
        table.create_entity(_entity("test", str(i), "foo"))
    entities = sorted(
        list(table.query_entities("PartitionKey eq 'test'")),
        key=lambda e: e["RowKey"],
    )
    assert len(entities) == 3
    assert entities[0]["RowKey"] == "1"
    assert entities[1]["RowKey"] == "2"
    assert entities[2]["RowKey"] == "3"


def test_entity_duplicate_raises():
    svc = _svc()
    svc.create_table_if_not_exists("tbduptest")
    table = svc.get_table_client("tbduptest")
    table.create_entity(_entity("test", "1", "foo"))
    with pytest.raises(ResourceExistsError):
        table.create_entity(_entity("test", "1", "foo"))


def test_entity_table_not_found_raises():
    svc = _svc()
    table = svc.get_table_client("tbnofound")
    with pytest.raises(HttpResponseError):
        table.create_entity(_entity("test", "1", "foo"))


def test_entity_update_replace():
    svc = _svc()
    svc.create_table_if_not_exists("tbupdreplace")
    table = svc.get_table_client("tbupdreplace")
    table.create_entity(_entity("test", "1", "foo"))
    entity = table.get_entity("test", "1")
    entity["Name"] = "bar"
    table.update_entity(entity, mode=UpdateMode.REPLACE)
    updated = table.get_entity("test", "1")
    assert updated["Name"] == "bar"


def test_entity_update_with_etag():
    svc = _svc()
    svc.create_table_if_not_exists("tbupdetetag")
    table = svc.get_table_client("tbupdetetag")
    table.create_entity(_entity("test", "1", "foo"))
    entity = table.get_entity("test", "1")
    entity["Name"] = "bar"
    table.update_entity(entity, mode=UpdateMode.REPLACE, etag=entity.metadata["etag"], match_condition=MatchConditions.IfNotModified)
    updated = table.get_entity("test", "1")
    assert updated["Name"] == "bar"


def test_entity_has_etag_and_timestamp():
    svc = _svc()
    svc.create_table_if_not_exists("tbmetacheck")
    table = svc.get_table_client("tbmetacheck")
    table.create_entity(_entity("test", "1", "foo"))
    entity = table.get_entity("test", "1")
    assert entity.metadata.get("etag") not in (None, "", "{}")
    assert entity.metadata.get("timestamp") is not None


def test_entity_etag_conflict_raises():
    svc = _svc()
    svc.create_table_if_not_exists("tbetconflict")
    table = svc.get_table_client("tbetconflict")
    table.create_entity(_entity("test", "1", "foo"))
    entity = table.get_entity("test", "1")
    same_entity = table.get_entity("test", "1")
    entity["Name"] = "bar"
    table.update_entity(entity, mode=UpdateMode.REPLACE, etag=entity.metadata["etag"], match_condition=MatchConditions.IfNotModified)
    same_entity["Name"] = "foobar"
    with pytest.raises(HttpResponseError):
        table.update_entity(
            same_entity, mode=UpdateMode.REPLACE, etag=same_entity.metadata["etag"], match_condition=MatchConditions.IfNotModified
        )


def test_entity_unconditional_update_succeeds():
    svc = _svc()
    svc.create_table_if_not_exists("tbuncondupd")
    table = svc.get_table_client("tbuncondupd")
    table.create_entity(_entity("test", "1", "foo"))
    entity = table.get_entity("test", "1")
    same = table.get_entity("test", "1")
    entity["Name"] = "bar"
    table.update_entity(entity, mode=UpdateMode.REPLACE, etag=entity.metadata["etag"], match_condition=MatchConditions.IfNotModified)
    same["Name"] = "foobar"
    table.update_entity(same, mode=UpdateMode.REPLACE)  # unconditional
    updated = table.get_entity("test", "1")
    assert updated["Name"] == "foobar"


def test_entity_update_merge():
    svc = _svc()
    svc.create_table_if_not_exists("tbmergetest")
    table = svc.get_table_client("tbmergetest")
    table.create_entity(_entity("test", "1", "foo"))
    entity = table.get_entity("test", "1")
    entity["Name"] = "bar"
    table.update_entity(entity, mode=UpdateMode.MERGE)
    updated = table.get_entity("test", "1")
    assert updated["Name"] == "bar"


def test_entity_delete():
    svc = _svc()
    svc.create_table_if_not_exists("tbdeltest")
    table = svc.get_table_client("tbdeltest")
    table.create_entity(_entity("test", "1", "foo"))
    table.delete_entity("test", "1")
    entities = list(table.query_entities(""))
    assert not any(e["RowKey"] == "1" for e in entities)


def test_entity_delete_with_matching_etag():
    svc = _svc()
    svc.create_table_if_not_exists("tbdeletag")
    table = svc.get_table_client("tbdeletag")
    table.create_entity(_entity("test", "1", "foo"))
    entity = table.get_entity("test", "1")
    table.delete_entity("test", "1", etag=entity.metadata["etag"], match_condition=MatchConditions.IfNotModified)
    entities = list(table.query_entities(""))
    assert not any(e["RowKey"] == "1" for e in entities)


def test_entity_delete_with_stale_etag_raises():
    svc = _svc()
    svc.create_table_if_not_exists("tbstaletag")
    table = svc.get_table_client("tbstaletag")
    table.create_entity(_entity("test", "1", "foo"))
    entity = table.get_entity("test", "1")
    stale_etag = entity.metadata["etag"]
    entity["Name"] = "bar"
    table.update_entity(entity, mode=UpdateMode.REPLACE)
    with pytest.raises(HttpResponseError):
        table.delete_entity("test", "1", etag=stale_etag, match_condition=MatchConditions.IfNotModified)
    svc = _svc()
    svc.create_table_if_not_exists("tbdelnx")
    table = svc.get_table_client("tbdelnx")
    table.delete_entity("test", "nonexistent")  # unconditional, must not raise


def test_entity_get_by_key():
    svc = _svc()
    svc.create_table_if_not_exists("tbgetbykey")
    table = svc.get_table_client("tbgetbykey")
    table.create_entity(_entity("test", "1", "foo"))
    entity = table.get_entity("test", "1")
    assert entity["PartitionKey"] == "test"
    assert entity["RowKey"] == "1"
    assert entity["Name"] == "foo"


def test_entity_get_nonexistent_raises():
    svc = _svc()
    svc.create_table_if_not_exists("tbgetnx")
    table = svc.get_table_client("tbgetnx")
    with pytest.raises(ResourceNotFoundError):
        table.get_entity("test", "nonexistent")


# ---------------------------------------------------------------------------
# Service properties
# ---------------------------------------------------------------------------

def test_table_service_properties_get():
    svc = _svc()
    props = svc.get_service_properties()
    assert props is not None


def test_table_service_properties_set_and_get():
    svc = _svc()
    props = svc.get_service_properties()
    logging = props["analytics_logging"]
    original_read = logging.read
    logging.read = not original_read
    svc.set_service_properties(analytics_logging=logging)
    updated = svc.get_service_properties()
    assert updated["analytics_logging"].read == (not original_read)


# ---------------------------------------------------------------------------
# Access policies
# ---------------------------------------------------------------------------

def test_table_access_policy_set_and_get():
    svc = _svc()
    svc.create_table_if_not_exists("tbacltest")
    table = svc.get_table_client("tbacltest")
    table.set_table_access_policy(
        signed_identifiers={
            "some_id": TableAccessPolicy(
                start=datetime.now(tz=timezone.utc),
                expiry=datetime.now(tz=timezone.utc) + timedelta(hours=1),
                permission="raud",
            )
        }
    )
    acls = table.get_table_access_policy()
    assert "some_id" in acls


# ---------------------------------------------------------------------------
# Invalid key
# ---------------------------------------------------------------------------

def test_table_invalid_key_raises():
    invalid_key = base64.b64encode(b"invalid-key").decode()
    conn = TopazResourceHelpers.get_storage_connection_string(_ACCOUNT_NAME, invalid_key)
    svc = TableServiceClient.from_connection_string(conn)
    with pytest.raises(HttpResponseError):
        svc.create_table_if_not_exists("tbinvalidkey")


# ---------------------------------------------------------------------------
# Service stats
# ---------------------------------------------------------------------------

def test_table_service_stats_lrs_returns_403():
    svc = _svc()
    with pytest.raises((HttpResponseError, Exception)) as exc_info:
        svc.get_service_stats()
    # For LRS accounts, get_service_stats should fail with 403 or a connection error
    # (since LRS has no secondary replica)
    assert getattr(exc_info.value, "status_code", None) == 403 or "ServiceRequestError" in type(exc_info.value).__name__


def test_table_service_stats_ragrs_returns_live():
    import requests
    import xml.etree.ElementTree as ET

    credential = AzureLocalCredential(GLOBAL_ADMIN_ID)
    mgmt = StorageManagementClient(
        credential=credential,
        subscription_id=_SUBSCRIPTION_ID,
        base_url=_RM_BASE_URL,
        credential_scopes=[f"{_RM_BASE_URL}/.default"],
    )
    mgmt.storage_accounts.begin_create(
        _RESOURCE_GROUP,
        _RAGRS_ACCOUNT_NAME,
        StorageAccountCreateParameters(
            sku=Sku(name="Standard_RAGRS"),
            kind="StorageV2",
            location="westeurope",
        ),
    ).result()
    ragrs_key = mgmt.storage_accounts.list_keys(_RESOURCE_GROUP, _RAGRS_ACCOUNT_NAME)["keys"][0]["value"]

    # Call secondary endpoint directly using SharedKeyLite auth
    date_str = datetime.now(tz=timezone.utc).strftime("%a, %d %b %Y %H:%M:%S GMT")
    canonicalized_resource = f"/{_RAGRS_ACCOUNT_NAME}/\ncomp:stats\nrestype:service"
    string_to_sign = f"{date_str}\n{canonicalized_resource}"
    key_bytes = base64.b64decode(ragrs_key)
    sig = base64.b64encode(
        hmac.new(key_bytes, msg=string_to_sign.encode("utf-8"), digestmod=hashlib.sha256).digest()
    ).decode()

    secondary_url = (
        f"https://{_RAGRS_ACCOUNT_NAME}-secondary.table.storage.topaz.local.dev"
        f":{DEFAULT_TABLE_STORAGE_PORT}/?restype=service&comp=stats"
    )
    resp = requests.get(
        secondary_url,
        headers={
            "x-ms-date": date_str,
            "x-ms-version": "2019-02-02",
            "Authorization": f"SharedKeyLite {_RAGRS_ACCOUNT_NAME}:{sig}",
        },
        verify=False,
        timeout=10,
    )
    assert resp.status_code == 200, f"Expected 200, got {resp.status_code}: {resp.text}"
    root = ET.fromstring(resp.content)
    ns = {"s": "http://schemas.microsoft.com/windowsazure"}
    status = root.find(".//GeoReplication/Status")
    assert status is not None and status.text == "live"
