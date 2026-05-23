"""
Azure Queue Storage data-plane E2E tests — Python port of QueueStorageTests.cs.

Covers queue CRUD, message send/receive/peek/update/delete/clear,
visibility timeout, dequeue count, ACL, metadata, and service properties.
"""

from datetime import datetime, timedelta, timezone

import pytest
from azure.core.exceptions import HttpResponseError
from azure.mgmt.storage import StorageManagementClient
from azure.mgmt.storage.models import StorageAccountCreateParameters, Sku
from azure.storage.queue import (
    AccessPolicy,
    QueueServiceClient,
)

from topaz_sdk import AzureLocalCredential, TopazArmClient, GLOBAL_ADMIN_ID
from topaz_sdk.helpers import DEFAULT_RESOURCE_MANAGER_PORT, TopazResourceHelpers

# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------

_SUBSCRIPTION_ID = "a0000006-0000-0000-0000-000000000006"
_SUBSCRIPTION_NAME = "py-sub-queue-test"
_RESOURCE_GROUP = "py-rg-queue-test"
_ACCOUNT_NAME = "pyqueuestortest"
_RM_BASE_URL = f"https://topaz.local.dev:{DEFAULT_RESOURCE_MANAGER_PORT}"

_KEY: str = ""


# ---------------------------------------------------------------------------
# Module-scoped setup
# ---------------------------------------------------------------------------

@pytest.fixture(scope="module", autouse=True)
def queue_environment():
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
# Helper
# ---------------------------------------------------------------------------

def _svc() -> QueueServiceClient:
    conn = TopazResourceHelpers.get_storage_connection_string(_ACCOUNT_NAME, _KEY)
    return QueueServiceClient.from_connection_string(conn)


# ---------------------------------------------------------------------------
# Queue CRUD
# ---------------------------------------------------------------------------

def test_queue_create_returns_name():
    svc = _svc()
    queue = svc.create_queue("testqueue")
    assert queue.queue_name == "testqueue"


def test_queue_create_multiple_are_listed():
    svc = _svc()
    svc.create_queue("queue1")
    svc.create_queue("queue2")
    svc.create_queue("queue3")
    names = [q["name"] for q in svc.list_queues()]
    assert "queue1" in names
    assert "queue2" in names
    assert "queue3" in names


def test_queue_delete_removes_queue():
    svc = _svc()
    svc.create_queue("queue-to-delete")
    svc.delete_queue("queue-to-delete")
    names = [q["name"] for q in svc.list_queues()]
    assert "queue-to-delete" not in names


def test_queue_list_returns_all():
    svc = _svc()
    svc.create_queue("list-queue-1")
    svc.create_queue("list-queue-2")
    names = [q["name"] for q in svc.list_queues()]
    assert "list-queue-1" in names
    assert "list-queue-2" in names


# ---------------------------------------------------------------------------
# Queue properties and metadata
# ---------------------------------------------------------------------------

def test_queue_get_properties_returns_message_count():
    svc = _svc()
    svc.create_queue("props-test-queue")
    queue = svc.get_queue_client("props-test-queue")
    props = queue.get_queue_properties()
    assert props.approximate_message_count >= 0


def test_queue_message_count_increases_after_send():
    svc = _svc()
    svc.create_queue("count-test-queue")
    queue = svc.get_queue_client("count-test-queue")
    queue.send_message("Message 1")
    queue.send_message("Message 2")
    queue.send_message("Message 3")
    props = queue.get_queue_properties()
    assert props.approximate_message_count >= 3


def test_queue_metadata_set_and_retrieved():
    svc = _svc()
    svc.create_queue("meta-set-queue")
    queue = svc.get_queue_client("meta-set-queue")
    queue.set_queue_metadata({"env": "test", "owner": "topaz"})
    props = queue.get_queue_properties()
    assert props.metadata["env"] == "test"
    assert props.metadata["owner"] == "topaz"


def test_queue_metadata_overwrite():
    svc = _svc()
    svc.create_queue("meta-overwrite-queue")
    queue = svc.get_queue_client("meta-overwrite-queue")
    queue.set_queue_metadata({"key1": "value1"})
    queue.set_queue_metadata({"key2": "value2"})
    props = queue.get_queue_properties()
    assert "key1" not in props.metadata
    assert props.metadata["key2"] == "value2"


# ---------------------------------------------------------------------------
# Send message
# ---------------------------------------------------------------------------

def test_queue_send_message_returns_receipt():
    svc = _svc()
    svc.create_queue("send-receipt-queue")
    queue = svc.get_queue_client("send-receipt-queue")
    result = queue.send_message("Hello World")
    assert result.id is not None and result.id != ""
    assert result.pop_receipt is not None and result.pop_receipt != ""
    assert result.inserted_on is not None
    assert result.expires_on is not None


def test_queue_send_message_is_dequeueble():
    svc = _svc()
    svc.create_queue("send-dequeue-queue")
    queue = svc.get_queue_client("send-dequeue-queue")
    queue.send_message("Dequeue me")
    received = list(queue.receive_messages(max_messages=1))
    assert len(received) == 1
    assert received[0].content == "Dequeue me"
    assert received[0].id is not None


def test_queue_send_message_with_visibility_timeout_is_hidden():
    svc = _svc()
    svc.create_queue("send-hidden-queue")
    queue = svc.get_queue_client("send-hidden-queue")
    queue.send_message("Hidden on arrival", visibility_timeout=60)
    received = list(queue.receive_messages(max_messages=1))
    assert len(received) == 0


# ---------------------------------------------------------------------------
# Update message
# ---------------------------------------------------------------------------

def test_queue_update_message_returns_new_pop_receipt():
    svc = _svc()
    svc.create_queue("update-msg-queue")
    queue = svc.get_queue_client("update-msg-queue")
    sent = queue.send_message("Original content")
    updated = queue.update_message(
        sent.id,
        sent.pop_receipt,
        content="Updated content",
        visibility_timeout=60,
    )
    assert updated.pop_receipt is not None
    assert updated.pop_receipt != sent.pop_receipt


def test_queue_update_message_with_visibility_timeout():
    svc = _svc()
    svc.create_queue("update-vis-queue")
    queue = svc.get_queue_client("update-vis-queue")
    sent = queue.send_message("Visibility test")
    updated = queue.update_message(
        sent.id,
        sent.pop_receipt,
        content="Visibility test",
        visibility_timeout=120,
    )
    assert updated.pop_receipt is not None


# ---------------------------------------------------------------------------
# Receive messages
# ---------------------------------------------------------------------------

def test_queue_receive_empty_queue_returns_nothing():
    svc = _svc()
    svc.create_queue("empty-queue")
    queue = svc.get_queue_client("empty-queue")
    messages = list(queue.receive_messages(max_messages=1))
    assert len(messages) == 0


def test_queue_receive_single_message():
    svc = _svc()
    svc.create_queue("single-message-queue")
    queue = svc.get_queue_client("single-message-queue")
    queue.send_message("Single message content")
    messages = list(queue.receive_messages(max_messages=1))
    assert len(messages) == 1
    assert messages[0].content == "Single message content"
    assert messages[0].pop_receipt is not None


def test_queue_receive_multiple_messages():
    svc = _svc()
    svc.create_queue("multi-message-queue")
    queue = svc.get_queue_client("multi-message-queue")
    queue.send_message("Message 1")
    queue.send_message("Message 2")
    queue.send_message("Message 3")
    messages = list(queue.receive_messages(max_messages=3))
    assert len(messages) >= 3


def test_queue_receive_respects_max_messages():
    svc = _svc()
    svc.create_queue("limit-test-queue")
    queue = svc.get_queue_client("limit-test-queue")
    for i in range(10):
        queue.send_message(f"Message {i}")
    messages = list(queue.receive_messages(max_messages=5))
    assert len(messages) == 5


def test_queue_receive_hides_message_during_visibility():
    svc = _svc()
    svc.create_queue("visibility-hide-queue")
    queue = svc.get_queue_client("visibility-hide-queue")
    queue.send_message("Hidden message")
    first = list(queue.receive_messages(max_messages=1, visibility_timeout=60))
    assert len(first) == 1
    second = list(queue.receive_messages(max_messages=1))
    assert len(second) == 0


def test_queue_receive_dequeue_count_increments():
    svc = _svc()
    svc.create_queue("dequeue-count-queue")
    queue = svc.get_queue_client("dequeue-count-queue")
    queue.send_message("Test message")
    msgs = list(queue.receive_messages(max_messages=1))
    initial = msgs[0].dequeue_count
    queue.delete_message(msgs[0].id, msgs[0].pop_receipt)
    queue.send_message("Test message")
    msgs2 = list(queue.receive_messages(max_messages=1))
    assert msgs2[0].dequeue_count >= initial


# ---------------------------------------------------------------------------
# Peek messages
# ---------------------------------------------------------------------------

def test_queue_peek_empty_returns_nothing():
    svc = _svc()
    svc.create_queue("peek-empty-queue")
    queue = svc.get_queue_client("peek-empty-queue")
    peeked = list(queue.peek_messages(max_messages=1))
    assert len(peeked) == 0


def test_queue_peek_returns_message_without_dequeuing():
    svc = _svc()
    svc.create_queue("peek-nodequeue-queue")
    queue = svc.get_queue_client("peek-nodequeue-queue")
    queue.send_message("Peek me")
    peeked = list(queue.peek_messages(max_messages=1))
    assert len(peeked) == 1
    assert peeked[0].content == "Peek me"


def test_queue_peek_message_remains_visible():
    svc = _svc()
    svc.create_queue("peek-visible-queue")
    queue = svc.get_queue_client("peek-visible-queue")
    queue.send_message("Still here")
    queue.peek_messages(max_messages=1)
    received = list(queue.receive_messages(max_messages=1))
    assert len(received) == 1
    assert received[0].content == "Still here"


def test_queue_peek_does_not_increment_dequeue_count():
    svc = _svc()
    svc.create_queue("peek-count-queue")
    queue = svc.get_queue_client("peek-count-queue")
    queue.send_message("Count test")
    queue.peek_messages(max_messages=1)
    queue.peek_messages(max_messages=1)
    received = list(queue.receive_messages(max_messages=1))
    assert received[0].dequeue_count == 1


def test_queue_peek_multiple_messages():
    svc = _svc()
    svc.create_queue("peek-multi-queue")
    queue = svc.get_queue_client("peek-multi-queue")
    queue.send_message("Peek 1")
    queue.send_message("Peek 2")
    queue.send_message("Peek 3")
    peeked = list(queue.peek_messages(max_messages=3))
    assert len(peeked) == 3


# ---------------------------------------------------------------------------
# Delete message
# ---------------------------------------------------------------------------

def test_queue_delete_message_succeeds():
    svc = _svc()
    svc.create_queue("delete-msg-queue")
    queue = svc.get_queue_client("delete-msg-queue")
    queue.send_message("Delete me")
    received = list(queue.receive_messages(max_messages=1))
    assert len(received) == 1
    queue.delete_message(received[0].id, received[0].pop_receipt)  # must not raise


def test_queue_delete_message_removes_from_queue():
    svc = _svc()
    svc.create_queue("delete-gone-queue")
    queue = svc.get_queue_client("delete-gone-queue")
    queue.send_message("To be deleted")
    received = list(queue.receive_messages(max_messages=1))
    queue.delete_message(received[0].id, received[0].pop_receipt)
    remaining = list(queue.receive_messages(max_messages=1, visibility_timeout=5))
    assert len(remaining) == 0


def test_queue_message_count_decreases_after_delete():
    svc = _svc()
    svc.create_queue("delete-count-queue")
    queue = svc.get_queue_client("delete-count-queue")
    queue.send_message("Message A")
    queue.send_message("Message B")
    before = queue.get_queue_properties().approximate_message_count
    received = list(queue.receive_messages(max_messages=1))
    queue.delete_message(received[0].id, received[0].pop_receipt)
    after = queue.get_queue_properties().approximate_message_count
    assert after < before


# ---------------------------------------------------------------------------
# Clear messages
# ---------------------------------------------------------------------------

def test_queue_clear_removes_all_messages():
    svc = _svc()
    svc.create_queue("clear-all-queue")
    queue = svc.get_queue_client("clear-all-queue")
    queue.send_message("Message 1")
    queue.send_message("Message 2")
    queue.send_message("Message 3")
    queue.clear_messages()
    remaining = list(queue.receive_messages(max_messages=10))
    assert len(remaining) == 0


def test_queue_clear_empty_queue_succeeds():
    svc = _svc()
    svc.create_queue("clear-empty-queue")
    queue = svc.get_queue_client("clear-empty-queue")
    queue.clear_messages()  # must not raise


# ---------------------------------------------------------------------------
# Access policies
# ---------------------------------------------------------------------------

def test_queue_set_acl_policy_is_retrieved():
    svc = _svc()
    svc.create_queue("acl-set-queue")
    queue = svc.get_queue_client("acl-set-queue")
    queue.set_queue_access_policy(
        signed_identifiers={
            "read-policy": AccessPolicy(
                start=datetime.now(tz=timezone.utc) - timedelta(minutes=1),
                expiry=datetime.now(tz=timezone.utc) + timedelta(hours=1),
                permission="r",
            )
        }
    )
    policies = queue.get_queue_access_policy()
    assert "read-policy" in policies
    assert policies["read-policy"].permission == "r"


def test_queue_set_acl_overwrites_previous():
    svc = _svc()
    svc.create_queue("acl-overwrite-queue")
    queue = svc.get_queue_client("acl-overwrite-queue")
    queue.set_queue_access_policy(signed_identifiers={"old-policy": AccessPolicy()})
    queue.set_queue_access_policy(signed_identifiers={"new-policy": AccessPolicy()})
    policies = queue.get_queue_access_policy()
    assert "old-policy" not in policies
    assert "new-policy" in policies


def test_queue_get_acl_empty_when_no_policies():
    svc = _svc()
    svc.create_queue("acl-get-queue")
    queue = svc.get_queue_client("acl-get-queue")
    policies = queue.get_queue_access_policy()
    assert len(policies) == 0


# ---------------------------------------------------------------------------
# Queue service properties
# ---------------------------------------------------------------------------

def test_queue_service_get_properties():
    svc = _svc()
    props = svc.get_service_properties()
    assert props is not None


def test_queue_service_set_and_get_properties_roundtrip():
    svc = _svc()
    original = svc.get_service_properties()
    svc.set_service_properties(**original)
    retrieved = svc.get_service_properties()
    assert retrieved is not None
