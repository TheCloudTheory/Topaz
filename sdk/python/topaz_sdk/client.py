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

    # ------------------------------------------------------------------
    # Context-manager support
    # ------------------------------------------------------------------

    def __enter__(self) -> "TopazArmClient":
        return self

    def __exit__(self, *args: Any) -> None:
        self._session.close()
