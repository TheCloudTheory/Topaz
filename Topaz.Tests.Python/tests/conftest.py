"""
Shared pytest fixtures for Topaz Python E2E tests.

Each test module declares its own subscription UUID (distinct from the GUIDs
used by the .NET E2E suite) and uses a module-scoped fixture to delete and
re-create its isolated environment before the tests run.

The Topaz host and Python container are managed by the NUnit PythonFixture;
conftest only handles the Azure resource topology setup.
"""

import os
import pytest
import requests

from topaz_sdk import AzureLocalCredential, TopazArmClient, GLOBAL_ADMIN_ID


# ---------------------------------------------------------------------------
# Patch Key Vault ChallengeAuthPolicy to always skip challenge resource
# verification. Topaz returns resource="https://vault.azure.net" in
# WWW-Authenticate, which the SDK would reject because it doesn't match the
# topaz.local.dev domain.  The test clients already pass
# verify_challenge_resource=False explicitly; this patch is a safety net for
# any client created without that kwarg.
# ---------------------------------------------------------------------------
try:
    import importlib as _il

    def _make_kv_init_patch(orig_init):
        def _patched_init(self, *args, **kwargs):
            kwargs["verify_challenge_resource"] = False
            orig_init(self, *args, **kwargs)
        return _patched_init

    for _kv_mod_name in (
        "azure.keyvault.secrets._shared.challenge_auth_policy",
        "azure.keyvault.keys._shared.challenge_auth_policy",
        "azure.keyvault.certificates._shared.challenge_auth_policy",
    ):
        try:
            _kv_cap_mod = _il.import_module(_kv_mod_name)
            _KvCap = _kv_cap_mod.ChallengeAuthPolicy
            _KvCap.__init__ = _make_kv_init_patch(_KvCap.__init__)
        except Exception:
            pass
except Exception:
    pass


# ---------------------------------------------------------------------------
# Patch pyamqp Transfer frame decoding for AMQPNetLite compatibility.
# AMQPNetLite (used by Topaz) omits trailing null optional fields in every
# AMQP performative. pyamqp accesses performative fields by fixed index
# (e.g. frame[4] for Open.idle-time-out, frame[11] for Transfer payload),
# which raises IndexError when AMQPNetLite omits those fields.
# This patch pads every decoded performative to its full AMQP 1.0 field count.
#
# AMQP 1.0 field counts per performative type:
#   16=Open(10)  17=Begin(8)  18=Attach(14)  19=Flow(11)  20=Transfer(11+payload)
#   21=Disposition(6)  22=Detach(3)  23=End(1)  24=Close(1)
# ---------------------------------------------------------------------------
try:
    import azure.servicebus._pyamqp._decode as _pdecode

    _AMQP_FRAME_FIELD_COUNTS = {
        16: 10,  # Open
        17: 8,   # Begin
        18: 14,  # Attach
        19: 11,  # Flow
        20: 11,  # Transfer (payload appended separately at index 11)
        21: 6,   # Disposition
        22: 3,   # Detach
        23: 1,   # End
        24: 1,   # Close
    }

    _orig_decode_frame = _pdecode.decode_frame

    def _patched_decode_frame(data):
        frame_type, fields = _orig_decode_frame(data)
        expected = _AMQP_FRAME_FIELD_COUNTS.get(frame_type)
        if expected is not None:
            if frame_type == 20:
                # Transfer: original appends payload at fields[count]; extract it,
                # pad performative fields to 11, then re-attach payload at index 11.
                payload = fields[-1] if len(fields) > 0 else b""
                fields = list(fields[:-1])
                while len(fields) < expected:
                    fields.append(None)
                fields.append(payload)
            else:
                fields = list(fields)
                while len(fields) < expected:
                    fields.append(None)
        return frame_type, fields

    _pdecode.decode_frame = _patched_decode_frame

    # _transport.py imports decode_frame via `from ._decode import decode_frame`,
    # creating a local binding. We must also replace that local binding so
    # receive_frame() in the transport layer uses the patched version.
    import azure.servicebus._pyamqp._transport as _ptransport
    _ptransport.decode_frame = _patched_decode_frame
except Exception:
    pass

# ---------------------------------------------------------------------------
# Patch _incoming_close to handle 2-field Error (AMQPNetLite omits `info`).
# AMQPNetLite encodes Error with [condition, description] (no info).
# pyamqp's _incoming_close accesses frame[0][2] unconditionally.
# ---------------------------------------------------------------------------
try:
    import azure.servicebus._pyamqp._connection as _pconn
    from azure.servicebus._pyamqp.error import AMQPConnectionError

    _orig_incoming_close = _pconn.Connection._incoming_close

    def _patched_incoming_close(self, channel, frame):
        if frame and frame[0]:
            err = list(frame[0])
            while len(err) < 3:
                err.append(None)
            frame = list(frame)
            frame[0] = err
        _orig_incoming_close(self, channel, frame)

    _pconn.Connection._incoming_close = _patched_incoming_close
except Exception:
    pass

# ---------------------------------------------------------------------------
# Patch ManagementLink._on_send_complete to handle 2-field Rejected Error.
# AMQPNetLite's Rejected outcome Error also has only [condition, description].
# pyamqp accesses state["rejected"][0][2] unconditionally.
# ---------------------------------------------------------------------------
try:
    import azure.servicebus._pyamqp.management_link as _pmgmt
    from azure.servicebus._pyamqp.constants import SEND_DISPOSITION_REJECT

    _orig_on_send_complete = _pmgmt.ManagementLink._on_send_complete

    def _patched_on_send_complete(self, message_delivery, reason, state):
        if state and SEND_DISPOSITION_REJECT in state:
            rej = state[SEND_DISPOSITION_REJECT]
            if rej and len(rej) > 0:
                err = list(rej[0])
                while len(err) < 3:
                    err.append(None)
                state = dict(state)
                state[SEND_DISPOSITION_REJECT] = [err] + list(rej[1:])
        _orig_on_send_complete(self, message_delivery, reason, state)

    _pmgmt.ManagementLink._on_send_complete = _patched_on_send_complete
except Exception:
    pass

# ---------------------------------------------------------------------------
# Session-level sanity check: Topaz must be reachable before any test runs
# ---------------------------------------------------------------------------

@pytest.fixture(scope="session", autouse=True)
def topaz_reachable():
    credential = AzureLocalCredential(GLOBAL_ADMIN_ID)
    client = TopazArmClient(credential)
    assert client.check_ready(), (
        "Topaz host is not reachable at https://topaz.local.dev:8899/health. "
        "Ensure the Topaz container is running and REQUESTS_CA_BUNDLE is set."
    )


# ---------------------------------------------------------------------------
# Convenience factory — test modules call this to get a fresh arm client
# ---------------------------------------------------------------------------

def make_arm_client() -> TopazArmClient:
    return TopazArmClient(AzureLocalCredential(GLOBAL_ADMIN_ID))
