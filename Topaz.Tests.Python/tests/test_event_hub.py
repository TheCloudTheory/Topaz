"""E2E tests for Azure Event Hubs (namespaces and event hubs)."""

from __future__ import annotations

import pytest

from azure.mgmt.eventhub import EventHubManagementClient
from azure.mgmt.eventhub.models import EHNamespace, Eventhub

from topaz_sdk.helpers import DEFAULT_RESOURCE_MANAGER_PORT, TopazResourceHelpers
from topaz_sdk.identity import GLOBAL_ADMIN_ID, AzureLocalCredential
from topaz_sdk.client import TopazArmClient

_SUBSCRIPTION_ID = "b0000002-0000-0000-0000-000000000002"
_RESOURCE_GROUP = "rg-eventhub-test"
_NAMESPACE_NAME = "ns-test"
_RM_BASE_URL = f"https://topaz.local.dev:{DEFAULT_RESOURCE_MANAGER_PORT}"


@pytest.fixture(scope="module", autouse=True)
def eventhub_environment():
    credential = AzureLocalCredential(GLOBAL_ADMIN_ID)
    with TopazArmClient(credential) as client:
        client.delete_subscription(_SUBSCRIPTION_ID)
        client.create_subscription(_SUBSCRIPTION_ID, "sub-eventhub-test")
        client.create_resource_group(_SUBSCRIPTION_ID, _RESOURCE_GROUP)

    eh_client = _eventhub_client()
    try:
        eh_client.namespaces.begin_delete(_RESOURCE_GROUP, _NAMESPACE_NAME).result()
    except Exception:
        pass
    eh_client.namespaces.begin_create_or_update(
        _RESOURCE_GROUP,
        _NAMESPACE_NAME,
        EHNamespace(location="westeurope"),
    ).result()
    yield


def _eventhub_client() -> EventHubManagementClient:
    return EventHubManagementClient(
        credential=AzureLocalCredential(GLOBAL_ADMIN_ID),
        subscription_id=_SUBSCRIPTION_ID,
        base_url=_RM_BASE_URL,
        credential_scopes=[f"{_RM_BASE_URL}/.default"],
    )


def test_namespace_create_and_get():
    client = _eventhub_client()
    namespace_name = "eh-ns-test"

    try:
        client.namespaces.begin_delete(_RESOURCE_GROUP, namespace_name).result()
    except Exception:
        pass

    result = client.namespaces.begin_create_or_update(
        _RESOURCE_GROUP,
        namespace_name,
        EHNamespace(location="westeurope"),
    ).result()
    assert result.name == namespace_name

    read_back = client.namespaces.get(_RESOURCE_GROUP, namespace_name)
    assert read_back.name == namespace_name

    client.namespaces.begin_delete(_RESOURCE_GROUP, namespace_name).result()


def test_namespace_update_properties():
    client = _eventhub_client()
    namespace_name = "eh-ns-update-test"

    try:
        client.namespaces.begin_delete(_RESOURCE_GROUP, namespace_name).result()
    except Exception:
        pass

    client.namespaces.begin_create_or_update(
        _RESOURCE_GROUP,
        namespace_name,
        EHNamespace(location="westeurope"),
    ).result()

    result = client.namespaces.begin_create_or_update(
        _RESOURCE_GROUP,
        namespace_name,
        EHNamespace(
            location="westeurope",
            is_auto_inflate_enabled=True,
            disable_local_auth=True,
        ),
    ).result()
    assert result.name == namespace_name

    read_back = client.namespaces.get(_RESOURCE_GROUP, namespace_name)
    assert read_back.name == namespace_name
    assert read_back.is_auto_inflate_enabled is True
    assert read_back.disable_local_auth is True

    client.namespaces.begin_delete(_RESOURCE_GROUP, namespace_name).result()


def test_eventhub_create_and_get():
    client = _eventhub_client()
    hub_name = "test-eh"

    result = client.event_hubs.create_or_update(
        _RESOURCE_GROUP,
        _NAMESPACE_NAME,
        hub_name,
        Eventhub(),
    )
    assert result.name == hub_name

    read_back = client.event_hubs.get(_RESOURCE_GROUP, _NAMESPACE_NAME, hub_name)
    assert read_back.name == hub_name
    # Cleanup: namespace is recreated by the module fixture on the next run


# ---------------------------------------------------------------------------
# AMQP data-plane smoke test (Phase 1 AMQP baseline)
# ---------------------------------------------------------------------------

_AMQP_HUB_NAME = "amqp-smoke"
_AMQP_CONNECTION_STRING = TopazResourceHelpers.get_event_hub_connection_string(_NAMESPACE_NAME)


@pytest.fixture(scope="module", autouse=False)
def amqp_hub():
    """Creates the AMQP smoke event hub once per module run."""
    client = _eventhub_client()
    try:
        client.event_hubs.delete(_RESOURCE_GROUP, _NAMESPACE_NAME, _AMQP_HUB_NAME)
    except Exception:
        pass
    client.event_hubs.create_or_update(
        _RESOURCE_GROUP,
        _NAMESPACE_NAME,
        _AMQP_HUB_NAME,
        Eventhub(),
    )
    yield
    try:
        client.event_hubs.delete(_RESOURCE_GROUP, _NAMESPACE_NAME, _AMQP_HUB_NAME)
    except Exception:
        pass


def test_eventhub_amqp_message_sent_should_be_received(amqp_hub):
    """
    Phase 1 AMQP baseline: send one event and receive it back via AMQP.

    Without client-side frame-padding patches, azure-eventhub's internal
    _pyamqp decode layer raises IndexError when AMQPNetLite omits trailing
    null fields from performatives. This test documents that failure mode so
    Phase 2 (upgrade to AMQPNetLite 2.5.3) can verify whether the issue is
    resolved.
    """
    from azure.eventhub import EventHubProducerClient, EventHubConsumerClient, EventData

    received: list[str] = []
    errors: list[str] = []

    with EventHubProducerClient.from_connection_string(
        _AMQP_CONNECTION_STRING, eventhub_name=_AMQP_HUB_NAME
    ) as producer:
        batch = producer.create_batch(partition_id="0")
        batch.add(EventData("smoke-test"))
        producer.send_batch(batch)

    def on_event(partition_context, event):
        received.append(event.body_as_str())

    def on_error(partition_context, error):
        errors.append(str(error))

    with EventHubConsumerClient.from_connection_string(
        _AMQP_CONNECTION_STRING, "$Default", eventhub_name=_AMQP_HUB_NAME
    ) as consumer:
        consumer.receive(
            on_event=on_event,
            on_error=on_error,
            partition_id="0",
            starting_position="-1",
            max_wait_time=5,
        )

    assert len(errors) == 0, f"AMQP errors during receive: {errors}"
    assert len(received) == 1, f"Expected 1 event, got {len(received)}"
    assert received[0] == "smoke-test"
