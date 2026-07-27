"""E2E tests for Azure App Service Plans and Web Apps (Sites)."""

from __future__ import annotations

import uuid

import pytest

from azure.mgmt.web import WebSiteManagementClient
from azure.mgmt.web.models import (
    AppServicePlan,
    Site,
    SiteConfigResource,
    SkuDescription,
    StringDictionary,
)

from topaz_sdk.helpers import DEFAULT_RESOURCE_MANAGER_PORT
from topaz_sdk.identity import GLOBAL_ADMIN_ID, AzureLocalCredential
from topaz_sdk.client import TopazArmClient

_PLAN_SUBSCRIPTION_ID = "b0000006-0000-0000-0000-000000000006"
_SITE_SUBSCRIPTION_ID = "b0000007-0000-0000-0000-000000000007"
_RESOURCE_GROUP = "rg-appservice-test"
_SITE_PLAN_NAME = "plan-test-site"
_RM_BASE_URL = f"https://topaz.local.dev:{DEFAULT_RESOURCE_MANAGER_PORT}"
_AZURE_WEBSITES_DNS_SUFFIX = "azurewebsites.topaz.local.dev"


@pytest.fixture(scope="module", autouse=True)
def app_service_plan_environment():
    credential = AzureLocalCredential(GLOBAL_ADMIN_ID)
    with TopazArmClient(credential) as client:
        client.delete_subscription(_PLAN_SUBSCRIPTION_ID)
        client.create_subscription(_PLAN_SUBSCRIPTION_ID, "sub-plan-test")
        client.create_resource_group(_PLAN_SUBSCRIPTION_ID, _RESOURCE_GROUP)
    yield


@pytest.fixture(scope="module", autouse=True)
def app_service_site_environment():
    credential = AzureLocalCredential(GLOBAL_ADMIN_ID)
    with TopazArmClient(credential) as client:
        client.delete_subscription(_SITE_SUBSCRIPTION_ID)
        client.create_subscription(_SITE_SUBSCRIPTION_ID, "sub-site-test")
        client.create_resource_group(_SITE_SUBSCRIPTION_ID, _RESOURCE_GROUP)

    web_client = _web_client(_SITE_SUBSCRIPTION_ID)
    web_client.app_service_plans.begin_create_or_update(
        _RESOURCE_GROUP,
        _SITE_PLAN_NAME,
        AppServicePlan(
            location="westeurope",
            sku=SkuDescription(name="B1", tier="Basic", capacity=1),
        ),
    ).result()
    yield


def _web_client(subscription_id: str) -> WebSiteManagementClient:
    return WebSiteManagementClient(
        credential=AzureLocalCredential(GLOBAL_ADMIN_ID),
        subscription_id=subscription_id,
        base_url=_RM_BASE_URL,
        credential_scopes=[f"{_RM_BASE_URL}/.default"],
    )


def _plan_params() -> AppServicePlan:
    return AppServicePlan(
        location="westeurope",
        sku=SkuDescription(name="B1", tier="Basic", capacity=1),
    )


def _site_params(plan_id: str) -> Site:
    return Site(
        location="westeurope",
        kind="app",
        server_farm_id=plan_id,
    )


# ------------------------------------------------------------------
# App Service Plan tests
# ------------------------------------------------------------------


def test_plan_create_and_get():
    client = _web_client(_PLAN_SUBSCRIPTION_ID)
    plan_name = f"plan-create-{uuid.uuid4().hex[:8]}"

    result = client.app_service_plans.begin_create_or_update(
        _RESOURCE_GROUP, plan_name, _plan_params()
    ).result()
    assert result.name == plan_name


def test_plan_get():
    client = _web_client(_PLAN_SUBSCRIPTION_ID)
    plan_name = f"plan-get-{uuid.uuid4().hex[:8]}"

    client.app_service_plans.begin_create_or_update(
        _RESOURCE_GROUP, plan_name, _plan_params()
    ).result()

    result = client.app_service_plans.get(_RESOURCE_GROUP, plan_name)
    assert result.name == plan_name


def test_plan_delete():
    client = _web_client(_PLAN_SUBSCRIPTION_ID)
    plan_name = f"plan-delete-{uuid.uuid4().hex[:8]}"

    client.app_service_plans.begin_create_or_update(
        _RESOURCE_GROUP, plan_name, _plan_params()
    ).result()

    client.app_service_plans.delete(_RESOURCE_GROUP, plan_name)

    with pytest.raises(Exception):
        client.app_service_plans.get(_RESOURCE_GROUP, plan_name)


def test_plan_list_by_rg():
    client = _web_client(_PLAN_SUBSCRIPTION_ID)
    plan_name = f"plan-list-rg-{uuid.uuid4().hex[:8]}"

    client.app_service_plans.begin_create_or_update(
        _RESOURCE_GROUP, plan_name, _plan_params()
    ).result()

    plans = list(client.app_service_plans.list_by_resource_group(_RESOURCE_GROUP))
    names = [p.name for p in plans]
    assert plan_name in names


def test_plan_list_by_subscription():
    client = _web_client(_PLAN_SUBSCRIPTION_ID)
    plan_name = f"plan-list-sub-{uuid.uuid4().hex[:8]}"

    client.app_service_plans.begin_create_or_update(
        _RESOURCE_GROUP, plan_name, _plan_params()
    ).result()

    plans = list(client.app_service_plans.list())
    names = [p.name for p in plans]
    assert plan_name in names


# ------------------------------------------------------------------
# App Service Site (Web App) tests
# ------------------------------------------------------------------


def _get_plan_id() -> str:
    client = _web_client(_SITE_SUBSCRIPTION_ID)
    plan = client.app_service_plans.get(_RESOURCE_GROUP, _SITE_PLAN_NAME)
    return plan.id


def test_site_create_and_get():
    client = _web_client(_SITE_SUBSCRIPTION_ID)
    site_name = f"test-site-create-{uuid.uuid4().hex[:6]}"

    result = client.web_apps.begin_create_or_update(
        _RESOURCE_GROUP, site_name, _site_params(_get_plan_id())
    ).result()
    assert result.name == site_name
    assert result.default_host_name == f"{site_name}.{_AZURE_WEBSITES_DNS_SUFFIX}"


def test_site_get():
    client = _web_client(_SITE_SUBSCRIPTION_ID)
    site_name = f"test-site-get-{uuid.uuid4().hex[:6]}"

    client.web_apps.begin_create_or_update(
        _RESOURCE_GROUP, site_name, _site_params(_get_plan_id())
    ).result()

    result = client.web_apps.get(_RESOURCE_GROUP, site_name)
    assert result.name == site_name
    assert result.default_host_name == f"{site_name}.{_AZURE_WEBSITES_DNS_SUFFIX}"


def test_site_delete():
    client = _web_client(_SITE_SUBSCRIPTION_ID)
    site_name = f"test-site-del-{uuid.uuid4().hex[:6]}"

    client.web_apps.begin_create_or_update(
        _RESOURCE_GROUP, site_name, _site_params(_get_plan_id())
    ).result()

    client.web_apps.delete(_RESOURCE_GROUP, site_name)

    with pytest.raises(Exception):
        client.web_apps.get(_RESOURCE_GROUP, site_name)


def test_site_list_by_rg():
    client = _web_client(_SITE_SUBSCRIPTION_ID)
    site_name = f"test-site-lrg-{uuid.uuid4().hex[:6]}"

    client.web_apps.begin_create_or_update(
        _RESOURCE_GROUP, site_name, _site_params(_get_plan_id())
    ).result()

    sites = list(client.web_apps.list_by_resource_group(_RESOURCE_GROUP))
    names = [s.name for s in sites]
    assert site_name in names


def test_site_list_by_subscription():
    client = _web_client(_SITE_SUBSCRIPTION_ID)
    site_name = f"test-site-lsub-{uuid.uuid4().hex[:6]}"

    client.web_apps.begin_create_or_update(
        _RESOURCE_GROUP, site_name, _site_params(_get_plan_id())
    ).result()

    sites = list(client.web_apps.list())
    names = [s.name for s in sites]
    assert site_name in names


def test_site_config_get():
    client = _web_client(_SITE_SUBSCRIPTION_ID)
    site_name = f"test-site-cfg-{uuid.uuid4().hex[:6]}"

    client.web_apps.begin_create_or_update(
        _RESOURCE_GROUP, site_name, _site_params(_get_plan_id())
    ).result()

    config = client.web_apps.get_configuration(_RESOURCE_GROUP, site_name)
    assert config is not None


def test_site_config_update():
    client = _web_client(_SITE_SUBSCRIPTION_ID)
    site_name = f"test-site-cfgu-{uuid.uuid4().hex[:6]}"

    client.web_apps.begin_create_or_update(
        _RESOURCE_GROUP, site_name, _site_params(_get_plan_id())
    ).result()

    result = client.web_apps.create_or_update_configuration(
        _RESOURCE_GROUP,
        site_name,
        SiteConfigResource(always_on=True),
    )
    assert result.always_on is True


def test_site_app_settings_update():
    client = _web_client(_SITE_SUBSCRIPTION_ID)
    site_name = f"test-site-aset-{uuid.uuid4().hex[:6]}"

    client.web_apps.begin_create_or_update(
        _RESOURCE_GROUP, site_name, _site_params(_get_plan_id())
    ).result()

    result = client.web_apps.update_application_settings(
        _RESOURCE_GROUP,
        site_name,
        StringDictionary(properties={"MY_SETTING": "my_value"}),
    )
    assert result.properties is not None
    assert result.properties.get("MY_SETTING") == "my_value"


def test_site_app_settings_list():
    client = _web_client(_SITE_SUBSCRIPTION_ID)
    site_name = f"test-site-alist-{uuid.uuid4().hex[:6]}"

    client.web_apps.begin_create_or_update(
        _RESOURCE_GROUP, site_name, _site_params(_get_plan_id())
    ).result()

    client.web_apps.update_application_settings(
        _RESOURCE_GROUP,
        site_name,
        StringDictionary(properties={"KEY_A": "val_a", "KEY_B": "val_b"}),
    )

    settings = client.web_apps.list_application_settings(_RESOURCE_GROUP, site_name)
    assert settings.properties is not None
    assert settings.properties.get("KEY_A") == "val_a"
    assert settings.properties.get("KEY_B") == "val_b"
