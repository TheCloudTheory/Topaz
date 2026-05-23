"""
Local credential implementations for the Topaz Azure emulator.

These are Python ports of ``AzureLocalCredential`` and
``ManagedIdentityLocalCredential`` from ``Topaz.Identity``.  Both classes
implement the ``azure.core.credentials.TokenCredential`` protocol and generate
locally-signed JWTs that Topaz accepts without contacting real Azure AD.

The JWT is signed with the same symmetric key, issuer, audience and claims as
``Topaz.Identity.JwtHelper`` — the key material here must stay in sync with
that C# file.
"""

from __future__ import annotations

from datetime import datetime, timedelta, timezone

import jwt
from azure.core.credentials import AccessToken, TokenCredential

# ---------------------------------------------------------------------------
# Constants — must stay in sync with Topaz.Identity.JwtHelper and
# Topaz.Identity.Globals
# ---------------------------------------------------------------------------

GLOBAL_ADMIN_ID: str = "00000000-0000-0000-0000-000000000000"

_TENANT_ID: str = "50717675-3E5E-4A1E-8CB5-C62D8BE8CA48"
_ISSUER: str = "https://topaz.local.dev:8899"
_AUDIENCE: str = "https://topaz.local.dev:8899"

# C# source: "yD1sMV1W..."u8.ToArray()  — the u8 suffix gives the UTF-8 bytes
# of the string literal, not a base64-decode.
_SECRET_KEY: bytes = (
    b"yD1sMV1WcwVjSfNUxxLNfVHn5sbqD056LwOnkXCkIDnWkXcrg95plLQ3T1tvinLAnuNNiRRZrKyUvs6YzZnJ/A=="
)


# ---------------------------------------------------------------------------
# Internal helper
# ---------------------------------------------------------------------------

def _generate_jwt(object_id: str, preferred_username: str | None = None) -> str:
    now = datetime.now(timezone.utc)
    payload: dict = {
        "sub": object_id,
        "oid": object_id,
        "appid": object_id,
        "azp": object_id,
        "tid": _TENANT_ID,
        "iss": _ISSUER,
        "aud": _AUDIENCE,
        "nbf": int(now.timestamp()),
        "iat": int(now.timestamp()),
        "exp": int((now + timedelta(hours=1)).timestamp()),
    }
    if preferred_username:
        payload["preferred_username"] = preferred_username
    return jwt.encode(payload, _SECRET_KEY, algorithm="HS256")


# ---------------------------------------------------------------------------
# Public credential classes
# ---------------------------------------------------------------------------

class AzureLocalCredential(TokenCredential):
    """
    A ``TokenCredential`` that generates locally-signed JWTs for Topaz.

    Drop-in replacement for ``DefaultAzureCredential`` when connecting to a
    local Topaz host.  No network calls are made; the token is generated
    deterministically from the provided ``object_id``.

    Equivalent to ``Topaz.Identity.AzureLocalCredential`` in C#.

    Args:
        object_id: The principal object ID to embed in the token.  Use
            ``GLOBAL_ADMIN_ID`` (``"00000000-0000-0000-0000-000000000000"``)
            to bypass RBAC checks, or a specific UUID to represent a named
            principal.
        preferred_username: Optional UPN to embed in the token (used by some
            Entra ID endpoints).
    """

    def __init__(self, object_id: str, preferred_username: str | None = None) -> None:
        self._object_id = object_id
        self._preferred_username = preferred_username

    def get_token(self, *scopes: str, **kwargs) -> AccessToken:
        token = _generate_jwt(self._object_id, self._preferred_username)
        expires_on = int((datetime.now(timezone.utc) + timedelta(hours=1)).timestamp())
        return AccessToken(token, expires_on)


class ManagedIdentityLocalCredential(TokenCredential):
    """
    A ``TokenCredential`` that emulates a managed identity for Topaz.

    Use this when your code is written to authenticate via managed identity
    (``ManagedIdentityCredential``) and you want to emulate that principal
    locally.

    Equivalent to ``Topaz.Identity.ManagedIdentityLocalCredential`` in C#.

    Args:
        principal_id: UUID of the managed identity to emulate.
    """

    def __init__(self, principal_id: str) -> None:
        self._principal_id = principal_id

    def get_token(self, *scopes: str, **kwargs) -> AccessToken:
        token = _generate_jwt(self._principal_id)
        expires_on = int((datetime.now(timezone.utc) + timedelta(hours=1)).timestamp())
        return AccessToken(token, expires_on)
