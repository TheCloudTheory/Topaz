"""
Service Bus E2E tests — Python port of Topaz.Tests/E2E/ServiceBusTests.cs.

Sends a message to a queue and asserts it is received over the non-TLS
AMQP connection (port 8889).
"""

import asyncio
import pytest
from azure.servicebus import ServiceBusClient, ServiceBusMessage, ServiceBusReceiveMode

from topaz_sdk import TopazArmClient, AzureLocalCredential, TopazResourceHelpers, GLOBAL_ADMIN_ID
from topaz_sdk.helpers import DEFAULT_RESOURCE_MANAGER_PORT

# ---------------------------------------------------------------------------
# Module-level constants
# ---------------------------------------------------------------------------

_SUBSCRIPTION_ID = "a0000002-0000-0000-0000-000000000002"
_SUBSCRIPTION_NAME = "py-sub-sb-test"
_RESOURCE_GROUP = "py-rg-sb-test"
_NAMESPACE_NAME = "py-sb-test"
_QUEUE_NAME = "py-queue-test"

_CONNECTION_STRING = TopazResourceHelpers.get_service_bus_connection_string(_NAMESPACE_NAME)


# ---------------------------------------------------------------------------
# Module-scoped setup: subscription, RG, Service Bus namespace, and queue
# ---------------------------------------------------------------------------

@pytest.fixture(scope="module", autouse=True)
def sb_environment():
    credential = AzureLocalCredential(GLOBAL_ADMIN_ID)

    with TopazArmClient(credential) as client:
        client.delete_subscription(_SUBSCRIPTION_ID)
        client.create_subscription(_SUBSCRIPTION_ID, _SUBSCRIPTION_NAME)
        client.create_resource_group(_SUBSCRIPTION_ID, _RESOURCE_GROUP)

    from azure.mgmt.servicebus import ServiceBusManagementClient
    from azure.mgmt.servicebus.models import SBNamespace, SBSku, SkuName, SkuTier, SBQueue

    sb_mgmt = ServiceBusManagementClient(
        credential=credential,
        subscription_id=_SUBSCRIPTION_ID,
        base_url=f"https://topaz.local.dev:{DEFAULT_RESOURCE_MANAGER_PORT}",
        credential_scopes=[f"https://topaz.local.dev:{DEFAULT_RESOURCE_MANAGER_PORT}/.default"],
    )

    sb_mgmt.namespaces.begin_create_or_update(
        _RESOURCE_GROUP,
        _NAMESPACE_NAME,
        SBNamespace(
            location="westeurope",
            sku=SBSku(name=SkuName.STANDARD, tier=SkuTier.STANDARD),
        ),
    ).result()

    sb_mgmt.queues.create_or_update(
        _RESOURCE_GROUP,
        _NAMESPACE_NAME,
        _QUEUE_NAME,
        SBQueue(),
    )

    yield


# ---------------------------------------------------------------------------
# Shared send-and-receive helper
# ---------------------------------------------------------------------------

def _run_send_receive(connection_string: str) -> list[str]:
    received: list[str] = []

    with ServiceBusClient.from_connection_string(connection_string) as client:
        with client.get_queue_sender(_QUEUE_NAME) as sender:
            sender.send_messages(ServiceBusMessage("test message"))

        with client.get_queue_receiver(
            _QUEUE_NAME,
            receive_mode=ServiceBusReceiveMode.RECEIVE_AND_DELETE,
            max_wait_time=5,
        ) as receiver:
            for msg in receiver:
                received.append(str(msg))

    return received


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------

def test_message_sent_to_queue_should_be_received():
    messages = _run_send_receive(_CONNECTION_STRING)

    assert len(messages) == 1
    assert messages[0] == "test message"
