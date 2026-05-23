"""E2E tests for Azure Virtual Network, Subnet, NIC, NSG, and Public IP Address."""

from __future__ import annotations

import uuid

import pytest

from azure.mgmt.network import NetworkManagementClient
from azure.mgmt.network.models import (
    AddressSpace,
    NetworkInterface,
    NetworkInterfaceIPConfiguration,
    NetworkSecurityGroup,
    PublicIPAddress,
    Subnet,
    TagsObject,
    VirtualNetwork,
)

from topaz_sdk.helpers import DEFAULT_RESOURCE_MANAGER_PORT
from topaz_sdk.identity import GLOBAL_ADMIN_ID, AzureLocalCredential
from topaz_sdk.client import TopazArmClient

_SUBSCRIPTION_ID = "b0000005-0000-0000-0000-000000000005"
_RESOURCE_GROUP = "rg-network-test"
_VNET_NAME = "vnet-test"
_VNET_PREFIX = "10.0.0.0/16"
_SUBNET_NAME = "default"
_SUBNET_PREFIX = "10.0.0.0/24"
_RM_BASE_URL = f"https://topaz.local.dev:{DEFAULT_RESOURCE_MANAGER_PORT}"

# Populated by the module fixture for use in NIC tests
_subnet_id: str = ""


@pytest.fixture(scope="module", autouse=True)
def network_environment():
    global _subnet_id

    credential = AzureLocalCredential(GLOBAL_ADMIN_ID)
    with TopazArmClient(credential) as client:
        client.delete_subscription(_SUBSCRIPTION_ID)
        client.create_subscription(_SUBSCRIPTION_ID, "sub-network-test")
        client.create_resource_group(_SUBSCRIPTION_ID, _RESOURCE_GROUP)

    net_client = _network_client()
    vnet = net_client.virtual_networks.begin_create_or_update(
        _RESOURCE_GROUP,
        _VNET_NAME,
        VirtualNetwork(
            location="westeurope",
            address_space=AddressSpace(address_prefixes=[_VNET_PREFIX]),
            subnets=[Subnet(name=_SUBNET_NAME, address_prefix=_SUBNET_PREFIX)],
        ),
    ).result()

    for subnet in vnet.subnets or []:
        if subnet.name == _SUBNET_NAME:
            _subnet_id = subnet.id
            break

    yield


def _network_client() -> NetworkManagementClient:
    return NetworkManagementClient(
        credential=AzureLocalCredential(GLOBAL_ADMIN_ID),
        subscription_id=_SUBSCRIPTION_ID,
        base_url=_RM_BASE_URL,
        credential_scopes=[f"{_RM_BASE_URL}/.default"],
    )


# ------------------------------------------------------------------
# Virtual Network tests
# ------------------------------------------------------------------


def test_vnet_create():
    client = _network_client()
    vnet_name = f"vnet-create-{uuid.uuid4().hex[:8]}"

    result = client.virtual_networks.begin_create_or_update(
        _RESOURCE_GROUP,
        vnet_name,
        VirtualNetwork(
            location="westeurope",
            address_space=AddressSpace(address_prefixes=["10.10.0.0/16"]),
        ),
    ).result()
    assert result.name == vnet_name


def test_subnet_create():
    client = _network_client()
    subnet_name = f"subnet-create-{uuid.uuid4().hex[:8]}"

    result = client.subnets.begin_create_or_update(
        _RESOURCE_GROUP,
        _VNET_NAME,
        subnet_name,
        Subnet(address_prefix="10.0.1.0/24"),
    ).result()
    assert result.name == subnet_name


def test_subnet_get():
    client = _network_client()
    subnet_name = f"subnet-get-{uuid.uuid4().hex[:8]}"

    client.subnets.begin_create_or_update(
        _RESOURCE_GROUP,
        _VNET_NAME,
        subnet_name,
        Subnet(address_prefix="10.0.2.0/24"),
    ).result()

    result = client.subnets.get(_RESOURCE_GROUP, _VNET_NAME, subnet_name)
    assert result.name == subnet_name


def test_subnet_delete():
    client = _network_client()
    subnet_name = f"subnet-del-{uuid.uuid4().hex[:8]}"

    client.subnets.begin_create_or_update(
        _RESOURCE_GROUP,
        _VNET_NAME,
        subnet_name,
        Subnet(address_prefix="10.0.3.0/24"),
    ).result()

    client.subnets.begin_delete(_RESOURCE_GROUP, _VNET_NAME, subnet_name).result()

    with pytest.raises(Exception):
        client.subnets.get(_RESOURCE_GROUP, _VNET_NAME, subnet_name)


def test_subnet_list():
    client = _network_client()
    subnets = list(client.subnets.list(_RESOURCE_GROUP, _VNET_NAME))
    assert len(subnets) >= 1
    names = [s.name for s in subnets]
    assert _SUBNET_NAME in names


def test_check_ip_in_subnet():
    client = _network_client()
    result = client.virtual_networks.check_ip_address_availability(
        _RESOURCE_GROUP, _VNET_NAME, ip_address="10.0.0.4"
    )
    assert result.available is True


def test_check_ip_outside_subnet():
    client = _network_client()
    result = client.virtual_networks.check_ip_address_availability(
        _RESOURCE_GROUP, _VNET_NAME, ip_address="192.168.1.4"
    )
    assert result.available is False


# ------------------------------------------------------------------
# Network Interface tests
# ------------------------------------------------------------------


def test_nic_create():
    client = _network_client()
    nic_name = f"nic-create-{uuid.uuid4().hex[:8]}"

    result = client.network_interfaces.begin_create_or_update(
        _RESOURCE_GROUP,
        nic_name,
        NetworkInterface(
            location="westeurope",
            ip_configurations=[
                NetworkInterfaceIPConfiguration(
                    name="ipconfig1",
                    private_ip_allocation_method="Dynamic",
                    subnet=Subnet(id=_subnet_id),
                )
            ],
        ),
    ).result()
    assert result.name == nic_name


def test_nic_delete():
    client = _network_client()
    nic_name = f"nic-delete-{uuid.uuid4().hex[:8]}"

    client.network_interfaces.begin_create_or_update(
        _RESOURCE_GROUP,
        nic_name,
        NetworkInterface(
            location="westeurope",
            ip_configurations=[
                NetworkInterfaceIPConfiguration(
                    name="ipconfig1",
                    private_ip_allocation_method="Dynamic",
                    subnet=Subnet(id=_subnet_id),
                )
            ],
        ),
    ).result()

    client.network_interfaces.begin_delete(_RESOURCE_GROUP, nic_name).result()

    with pytest.raises(Exception):
        client.network_interfaces.get(_RESOURCE_GROUP, nic_name)


def test_nic_list():
    client = _network_client()
    nic_name = f"nic-list-{uuid.uuid4().hex[:8]}"

    client.network_interfaces.begin_create_or_update(
        _RESOURCE_GROUP,
        nic_name,
        NetworkInterface(
            location="westeurope",
            ip_configurations=[
                NetworkInterfaceIPConfiguration(
                    name="ipconfig1",
                    private_ip_allocation_method="Dynamic",
                    subnet=Subnet(id=_subnet_id),
                )
            ],
        ),
    ).result()

    nics = list(client.network_interfaces.list(_RESOURCE_GROUP))
    names = [n.name for n in nics]
    assert nic_name in names


# ------------------------------------------------------------------
# Network Security Group tests
# ------------------------------------------------------------------


def test_nsg_create():
    client = _network_client()
    nsg_name = f"nsg-create-{uuid.uuid4().hex[:8]}"

    result = client.network_security_groups.begin_create_or_update(
        _RESOURCE_GROUP,
        nsg_name,
        NetworkSecurityGroup(location="westeurope"),
    ).result()
    assert result.name == nsg_name


def test_nsg_delete():
    client = _network_client()
    nsg_name = f"nsg-delete-{uuid.uuid4().hex[:8]}"

    client.network_security_groups.begin_create_or_update(
        _RESOURCE_GROUP,
        nsg_name,
        NetworkSecurityGroup(location="westeurope"),
    ).result()

    client.network_security_groups.begin_delete(_RESOURCE_GROUP, nsg_name).result()

    with pytest.raises(Exception):
        client.network_security_groups.get(_RESOURCE_GROUP, nsg_name)


def test_nsg_list_by_rg():
    client = _network_client()
    nsg_name = f"nsg-list-rg-{uuid.uuid4().hex[:8]}"

    client.network_security_groups.begin_create_or_update(
        _RESOURCE_GROUP,
        nsg_name,
        NetworkSecurityGroup(location="westeurope"),
    ).result()

    nsgs = list(client.network_security_groups.list(_RESOURCE_GROUP))
    names = [n.name for n in nsgs]
    assert nsg_name in names


def test_nsg_list_by_subscription():
    client = _network_client()
    nsg_name = f"nsg-list-sub-{uuid.uuid4().hex[:8]}"

    client.network_security_groups.begin_create_or_update(
        _RESOURCE_GROUP,
        nsg_name,
        NetworkSecurityGroup(location="westeurope"),
    ).result()

    nsgs = list(client.network_security_groups.list_all())
    names = [n.name for n in nsgs]
    assert nsg_name in names


def test_nsg_update_tags():
    client = _network_client()
    nsg_name = f"nsg-tags-{uuid.uuid4().hex[:8]}"

    client.network_security_groups.begin_create_or_update(
        _RESOURCE_GROUP,
        nsg_name,
        NetworkSecurityGroup(location="westeurope"),
    ).result()

    result = client.network_security_groups.update_tags(
        _RESOURCE_GROUP,
        nsg_name,
        TagsObject(tags={"env": "test"}),
    )
    assert result.tags is not None
    assert result.tags.get("env") == "test"


# ------------------------------------------------------------------
# Public IP Address tests
# ------------------------------------------------------------------


def test_public_ip_create():
    client = _network_client()
    pip_name = f"pip-create-{uuid.uuid4().hex[:8]}"

    result = client.public_ip_addresses.begin_create_or_update(
        _RESOURCE_GROUP,
        pip_name,
        PublicIPAddress(
            location="westeurope",
            public_ip_allocation_method="Static",
            public_ip_address_version="IPv4",
        ),
    ).result()
    assert result.name == pip_name


def test_public_ip_delete():
    client = _network_client()
    pip_name = f"pip-delete-{uuid.uuid4().hex[:8]}"

    client.public_ip_addresses.begin_create_or_update(
        _RESOURCE_GROUP,
        pip_name,
        PublicIPAddress(
            location="westeurope",
            public_ip_allocation_method="Static",
            public_ip_address_version="IPv4",
        ),
    ).result()

    client.public_ip_addresses.begin_delete(_RESOURCE_GROUP, pip_name).result()

    with pytest.raises(Exception):
        client.public_ip_addresses.get(_RESOURCE_GROUP, pip_name)


def test_public_ip_list():
    client = _network_client()
    pip_name = f"pip-list-{uuid.uuid4().hex[:8]}"

    client.public_ip_addresses.begin_create_or_update(
        _RESOURCE_GROUP,
        pip_name,
        PublicIPAddress(
            location="westeurope",
            public_ip_allocation_method="Static",
            public_ip_address_version="IPv4",
        ),
    ).result()

    pips = list(client.public_ip_addresses.list(_RESOURCE_GROUP))
    names = [p.name for p in pips]
    assert pip_name in names
