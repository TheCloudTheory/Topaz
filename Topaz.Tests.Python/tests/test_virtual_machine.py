"""E2E tests for Azure Virtual Machines."""

from __future__ import annotations

import uuid

import pytest

from azure.mgmt.compute import ComputeManagementClient
from azure.mgmt.compute.models import (
    HardwareProfile,
    ImageReference,
    NetworkProfile,
    NetworkInterfaceReference,
    OSProfile,
    StorageProfile,
    VirtualMachine,
)
from azure.mgmt.network import NetworkManagementClient
from azure.mgmt.network.models import (
    AddressSpace,
    NetworkInterface,
    NetworkInterfaceIPConfiguration,
    Subnet,
    VirtualNetwork,
)

from topaz_sdk.helpers import DEFAULT_RESOURCE_MANAGER_PORT
from topaz_sdk.identity import GLOBAL_ADMIN_ID, AzureLocalCredential
from topaz_sdk.client import TopazArmClient

_SUBSCRIPTION_ID = "b0000004-0000-0000-0000-000000000004"
_RESOURCE_GROUP = "rg-vm-test"
_VNET_NAME = "vnet-test-vm"
_NIC_NAME = "nic-test-vm"
_RM_BASE_URL = f"https://topaz.local.dev:{DEFAULT_RESOURCE_MANAGER_PORT}"

# Populated by the module fixture
_nic_id: str = ""


@pytest.fixture(scope="module", autouse=True)
def vm_environment():
    global _nic_id

    credential = AzureLocalCredential(GLOBAL_ADMIN_ID)
    with TopazArmClient(credential) as client:
        client.delete_subscription(_SUBSCRIPTION_ID)
        client.create_subscription(_SUBSCRIPTION_ID, "sub-vm-test")
        client.create_resource_group(_SUBSCRIPTION_ID, _RESOURCE_GROUP)

    net_client = _network_client()
    vnet = net_client.virtual_networks.begin_create_or_update(
        _RESOURCE_GROUP,
        _VNET_NAME,
        VirtualNetwork(
            location="westeurope",
            address_space=AddressSpace(address_prefixes=["10.20.0.0/16"]),
            subnets=[Subnet(name="default", address_prefix="10.20.0.0/24")],
        ),
    ).result()

    subnet_id = ""
    for subnet in vnet.subnets or []:
        if subnet.name == "default":
            subnet_id = subnet.id
            break

    nic = net_client.network_interfaces.begin_create_or_update(
        _RESOURCE_GROUP,
        _NIC_NAME,
        NetworkInterface(
            location="westeurope",
            ip_configurations=[
                NetworkInterfaceIPConfiguration(
                    name="ipconfig1",
                    private_ip_allocation_method="Dynamic",
                    subnet=Subnet(id=subnet_id),
                )
            ],
        ),
    ).result()
    _nic_id = nic.id

    yield


def _network_client() -> NetworkManagementClient:
    return NetworkManagementClient(
        credential=AzureLocalCredential(GLOBAL_ADMIN_ID),
        subscription_id=_SUBSCRIPTION_ID,
        base_url=_RM_BASE_URL,
        credential_scopes=[f"{_RM_BASE_URL}/.default"],
    )


def _compute_client() -> ComputeManagementClient:
    return ComputeManagementClient(
        credential=AzureLocalCredential(GLOBAL_ADMIN_ID),
        subscription_id=_SUBSCRIPTION_ID,
        base_url=_RM_BASE_URL,
        credential_scopes=[f"{_RM_BASE_URL}/.default"],
    )


def _minimal_vm_params(vm_name: str) -> VirtualMachine:
    return VirtualMachine(
        location="westeurope",
        hardware_profile=HardwareProfile(vm_size="Standard_D2s_v3"),
        os_profile=OSProfile(
            computer_name=vm_name,
            admin_username="adminuser",
            admin_password="Admin1234!@#",
        ),
        storage_profile=StorageProfile(
            image_reference=ImageReference(
                publisher="Canonical",
                offer="0001-com-ubuntu-server-jammy",
                sku="22_04-lts",
                version="latest",
            )
        ),
        network_profile=NetworkProfile(
            network_interfaces=[
                NetworkInterfaceReference(id=_nic_id, primary=True)
            ]
        ),
    )


def test_vm_create():
    client = _compute_client()
    vm_name = f"vm-create-{uuid.uuid4().hex[:8]}"

    result = client.virtual_machines.begin_create_or_update(
        _RESOURCE_GROUP, vm_name, _minimal_vm_params(vm_name)
    ).result()
    assert result.name == vm_name
    assert result.location == "westeurope"


def test_vm_delete():
    client = _compute_client()
    vm_name = f"vm-delete-{uuid.uuid4().hex[:8]}"

    client.virtual_machines.begin_create_or_update(
        _RESOURCE_GROUP, vm_name, _minimal_vm_params(vm_name)
    ).result()

    client.virtual_machines.begin_delete(_RESOURCE_GROUP, vm_name).result()

    with pytest.raises(Exception):
        client.virtual_machines.get(_RESOURCE_GROUP, vm_name)


def test_vm_update_tags():
    client = _compute_client()
    vm_name = f"vm-tags-{uuid.uuid4().hex[:8]}"

    client.virtual_machines.begin_create_or_update(
        _RESOURCE_GROUP, vm_name, _minimal_vm_params(vm_name)
    ).result()

    updated_params = _minimal_vm_params(vm_name)
    updated_params.tags = {"env": "test", "project": "topaz"}
    updated = client.virtual_machines.begin_create_or_update(
        _RESOURCE_GROUP, vm_name, updated_params
    ).result()
    assert updated.tags is not None
    assert updated.tags.get("env") == "test"
    assert updated.tags.get("project") == "topaz"


def test_vm_list_by_rg():
    client = _compute_client()
    vm_name = f"vm-list-rg-{uuid.uuid4().hex[:8]}"

    client.virtual_machines.begin_create_or_update(
        _RESOURCE_GROUP, vm_name, _minimal_vm_params(vm_name)
    ).result()

    vms = list(client.virtual_machines.list(_RESOURCE_GROUP))
    names = [v.name for v in vms]
    assert vm_name in names


def test_vm_list_by_subscription():
    client = _compute_client()
    vm_name = f"vm-list-sub-{uuid.uuid4().hex[:8]}"

    client.virtual_machines.begin_create_or_update(
        _RESOURCE_GROUP, vm_name, _minimal_vm_params(vm_name)
    ).result()

    vms = list(client.virtual_machines.list_all())
    names = [v.name for v in vms]
    assert vm_name in names
