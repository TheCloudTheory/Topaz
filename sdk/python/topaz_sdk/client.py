"""
Low-level HTTP client for Topaz management-plane operations.

Python port of ``Topaz.ResourceManager.TopazArmClient``.  Wraps the Topaz
management REST surface that is not covered by ``azure-mgmt-resource`` —
primarily subscription and resource group lifecycle operations needed to set up
test fixtures.

SSL verification is controlled by the ``REQUESTS_CA_BUNDLE`` environment
variable (set automatically by the test fixture inside the Docker container).
"""

from __future__ import annotations

import json
import os
from typing import Any

import requests
from requests import Session

from topaz_sdk.helpers import DEFAULT_RESOURCE_MANAGER_PORT
from topaz_sdk.identity import AzureLocalCredential

_BASE_URL = f"https://topaz.local.dev:{DEFAULT_RESOURCE_MANAGER_PORT}/"


class TopazArmClient:
    """
    Thin HTTP client for Topaz management operations not in ``azure-mgmt-*``.

    Equivalent to ``Topaz.ResourceManager.TopazArmClient`` in C#.

    Uses ``requests`` under the hood.  SSL verification honours the
    ``REQUESTS_CA_BUNDLE`` environment variable automatically; no extra
    configuration is needed when the variable is set.

    Args:
        credential: A ``AzureLocalCredential`` (or any object with a
            compatible ``get_token()`` method) used to sign every request.
    """

    def __init__(self, credential: AzureLocalCredential) -> None:
        self._credential = credential
        self._session = Session()

    def _auth_header(self) -> str:
        token = self._credential.get_token()
        return f"Bearer {token.token}"

    def _post(self, path: str, payload: dict | None = None) -> requests.Response:
        headers = {"Authorization": self._auth_header()}
        body = json.dumps(payload, separators=(",", ":")) if payload else None
        response = self._session.post(
            _BASE_URL + path,
            data=body,
            headers={**headers, "Content-Type": "application/json"} if body else headers,
        )
        if response.status_code == 400:
            raise requests.HTTPError(response.text, response=response)
        response.raise_for_status()
        return response

    def _put(self, path: str, payload: dict | None = None) -> requests.Response:
        headers = {"Authorization": self._auth_header(), "Content-Type": "application/json"}
        body = json.dumps(payload or {}, separators=(",", ":"))
        response = self._session.put(_BASE_URL + path, data=body, headers=headers)
        if not response.ok:
            raise requests.HTTPError(response.text, response=response)
        return response

    def _delete(self, path: str) -> requests.Response:
        headers = {"Authorization": self._auth_header()}
        response = self._session.delete(_BASE_URL + path, headers=headers)
        if response.status_code not in (200, 204, 404):
            raise requests.HTTPError(response.text, response=response)
        return response

    def _get(self, path: str) -> requests.Response:
        headers = {"Authorization": self._auth_header()}
        response = self._session.get(_BASE_URL + path, headers=headers)
        response.raise_for_status()
        return response

    # ------------------------------------------------------------------
    # Subscription operations
    # ------------------------------------------------------------------

    def create_subscription(self, subscription_id: str, subscription_name: str) -> None:
        """Creates a subscription in the Topaz emulator."""
        self._post(
            f"subscriptions/{subscription_id}",
            {"SubscriptionName": subscription_name, "SubscriptionId": subscription_id},
        )

    def delete_subscription(self, subscription_id: str) -> None:
        """Deletes a subscription.  Silently succeeds if not found."""
        self._delete(f"subscriptions/{subscription_id}")

    # ------------------------------------------------------------------
    # Resource group operations
    # ------------------------------------------------------------------

    def create_resource_group(
        self,
        subscription_id: str,
        resource_group_name: str,
        location: str = "westeurope",
    ) -> None:
        """Creates or updates a resource group."""
        self._put(
            f"subscriptions/{subscription_id}/resourceGroups/{resource_group_name}",
            {"location": location},
        )

    def delete_resource_group(
        self, subscription_id: str, resource_group_name: str
    ) -> None:
        """Deletes a resource group.  Silently succeeds if not found."""
        self._delete(
            f"subscriptions/{subscription_id}/resourceGroups/{resource_group_name}"
        )

    # ------------------------------------------------------------------
    # Key Vault operations
    # ------------------------------------------------------------------

    def purge_key_vault(
        self, subscription_id: str, key_vault_name: str, location: str = "westeurope"
    ) -> None:
        """Purges a soft-deleted Key Vault."""
        self._post(
            f"subscriptions/{subscription_id}/providers/Microsoft.KeyVault"
            f"/locations/{location}/deletedVaults/{key_vault_name}/purge"
        )

    # ------------------------------------------------------------------
    # Health
    # ------------------------------------------------------------------

    def check_ready(self) -> bool:
        """Returns ``True`` when the Topaz host is up and healthy."""
        try:
            response = self._session.get(_BASE_URL + "health")
            return response.ok
        except requests.RequestException:
            return False

    def _patch(self, path: str, payload: dict | None = None) -> requests.Response:
        headers = {"Authorization": self._auth_header(), "Content-Type": "application/json"}
        body = json.dumps(payload or {}, separators=(",", ":"))
        response = self._session.patch(_BASE_URL + path, data=body, headers=headers)
        if not response.ok:
            raise requests.HTTPError(response.text, response=response)
        return response

    # ------------------------------------------------------------------
    # Subscription management
    # ------------------------------------------------------------------

    def update_subscription(
        self,
        subscription_id: str,
        subscription_name: str,
        tags: dict | None = None,
    ) -> None:
        """Updates subscription name and/or tags."""
        self._patch(
            f"subscriptions/{subscription_id}",
            {"SubscriptionName": subscription_name, "Tags": tags or {}},
        )

    def cancel_subscription(self, subscription_id: str) -> None:
        """Cancels (disables) a subscription."""
        self._post(
            f"subscriptions/{subscription_id}/providers/Microsoft.Subscription/cancel"
        )

    def enable_subscription(self, subscription_id: str) -> None:
        """Re-enables a previously cancelled subscription."""
        self._post(
            f"subscriptions/{subscription_id}/providers/Microsoft.Subscription/enable"
        )

    # ------------------------------------------------------------------
    # Management group operations
    # ------------------------------------------------------------------

    def create_management_group(self, group_id: str, display_name: str) -> None:
        """Creates a management group."""
        self._put(
            f"providers/Microsoft.Management/managementGroups/{group_id}",
            {"properties": {"displayName": display_name}},
        )

    def create_management_group_with_parent(
        self, group_id: str, display_name: str, parent_id: str
    ) -> None:
        """Creates a management group under a parent management group."""
        self._put(
            f"providers/Microsoft.Management/managementGroups/{group_id}",
            {
                "properties": {
                    "displayName": display_name,
                    "details": {
                        "parent": {
                            "id": f"/providers/Microsoft.Management/managementGroups/{parent_id}"
                        }
                    },
                }
            },
        )

    def get_management_group(self, group_id: str) -> dict:
        """Gets a management group by ID."""
        return self._get(
            f"providers/Microsoft.Management/managementGroups/{group_id}"
        ).json()

    def get_descendants(self, group_id: str) -> dict:
        """Gets all descendants of a management group."""
        return self._get(
            f"providers/Microsoft.Management/managementGroups/{group_id}/descendants"
        ).json()

    def get_entities(self) -> dict:
        """Gets all management group entities."""
        return self._get("providers/Microsoft.Management/getEntities").json()

    def associate_subscription_with_management_group(
        self, group_id: str, subscription_id: str
    ) -> dict:
        """Associates a subscription with a management group."""
        return self._put(
            f"providers/Microsoft.Management/managementGroups/{group_id}"
            f"/subscriptions/{subscription_id}"
        ).json()

    def disassociate_subscription_from_management_group(
        self, group_id: str, subscription_id: str
    ) -> None:
        """Removes a subscription from a management group."""
        self._delete(
            f"providers/Microsoft.Management/managementGroups/{group_id}"
            f"/subscriptions/{subscription_id}"
        )

    def get_subscription_under_management_group(
        self, group_id: str, subscription_id: str
    ) -> dict:
        """Gets the association between a management group and subscription."""
        return self._get(
            f"providers/Microsoft.Management/managementGroups/{group_id}"
            f"/subscriptions/{subscription_id}"
        ).json()

    # ------------------------------------------------------------------
    # Hierarchy settings
    # ------------------------------------------------------------------

    def create_or_update_hierarchy_settings(
        self,
        group_id: str,
        require_authorization_for_group_creation: bool | None = None,
        default_management_group: str | None = None,
    ) -> dict:
        """Creates or updates hierarchy settings for a management group."""
        props: dict = {}
        if require_authorization_for_group_creation is not None:
            props["requireAuthorizationForGroupCreation"] = require_authorization_for_group_creation
        if default_management_group is not None:
            props["defaultManagementGroup"] = default_management_group
        return self._put(
            f"providers/Microsoft.Management/managementGroups/{group_id}/settings/default",
            {"properties": props},
        ).json()

    def get_hierarchy_settings(self, group_id: str) -> dict:
        """Gets hierarchy settings for a management group."""
        return self._get(
            f"providers/Microsoft.Management/managementGroups/{group_id}/settings/default"
        ).json()

    def list_hierarchy_settings(self, group_id: str) -> dict:
        """Lists all hierarchy settings for a management group."""
        return self._get(
            f"providers/Microsoft.Management/managementGroups/{group_id}/settings"
        ).json()

    def update_hierarchy_settings(
        self,
        group_id: str,
        require_authorization_for_group_creation: bool | None = None,
        default_management_group: str | None = None,
    ) -> dict:
        """Updates hierarchy settings for a management group."""
        props: dict = {}
        if require_authorization_for_group_creation is not None:
            props["requireAuthorizationForGroupCreation"] = require_authorization_for_group_creation
        if default_management_group is not None:
            props["defaultManagementGroup"] = default_management_group
        return self._patch(
            f"providers/Microsoft.Management/managementGroups/{group_id}/settings/default",
            {"properties": props},
        ).json()

    def delete_hierarchy_settings(self, group_id: str) -> None:
        """Deletes hierarchy settings for a management group."""
        response = self._session.delete(
            _BASE_URL
            + f"providers/Microsoft.Management/managementGroups/{group_id}/settings/default",
            headers={"Authorization": self._auth_header()},
        )
        if not response.ok:
            raise requests.HTTPError(response.text, response=response)

    # ------------------------------------------------------------------
    # Scoped role assignments
    # ------------------------------------------------------------------

    def create_resource_group_role_assignment(
        self,
        subscription_id: str,
        resource_group_name: str,
        assignment_name: str,
        principal_id: str,
        role_definition_id: str,
    ) -> None:
        """Creates a role assignment scoped to a resource group."""
        self._put(
            f"subscriptions/{subscription_id}/resourceGroups/{resource_group_name}"
            f"/providers/Microsoft.Authorization/roleAssignments/{assignment_name}",
            {"properties": {"principalId": principal_id, "roleDefinitionId": role_definition_id}},
        )

    def create_resource_role_assignment(
        self,
        subscription_id: str,
        resource_group_name: str,
        provider_namespace: str,
        resource_type: str,
        resource_name: str,
        assignment_name: str,
        principal_id: str,
        role_definition_id: str,
    ) -> None:
        """Creates a role assignment scoped to a specific resource."""
        self._put(
            f"subscriptions/{subscription_id}/resourceGroups/{resource_group_name}"
            f"/providers/{provider_namespace}/{resource_type}/{resource_name}"
            f"/providers/Microsoft.Authorization/roleAssignments/{assignment_name}",
            {"properties": {"principalId": principal_id, "roleDefinitionId": role_definition_id}},
        )

    def create_management_group_role_assignment(
        self,
        management_group_id: str,
        assignment_name: str,
        principal_id: str,
        role_definition_id: str,
    ) -> None:
        """Creates a role assignment scoped to a management group."""
        self._put(
            f"providers/Microsoft.Management/managementGroups/{management_group_id}"
            f"/providers/Microsoft.Authorization/roleAssignments/{assignment_name}",
            {"properties": {"principalId": principal_id, "roleDefinitionId": role_definition_id}},
        )

    # ------------------------------------------------------------------
    # Context-manager support
    # ------------------------------------------------------------------

    def __enter__(self) -> "TopazArmClient":
        return self

    def __exit__(self, *args: Any) -> None:
        self._session.close()
