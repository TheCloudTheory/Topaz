"""
Endpoint URL and connection string helpers for every Topaz service.

Python port of ``Topaz.ResourceManager.TopazResourceHelpers`` and the
port constants from ``Topaz.Shared.GlobalSettings``.
"""

from __future__ import annotations

# ---------------------------------------------------------------------------
# Port constants — must stay in sync with Topaz.Shared.GlobalSettings
# ---------------------------------------------------------------------------

DEFAULT_EVENT_HUB_AMQP_PORT: int = 8888
DEFAULT_SERVICE_BUS_AMQP_PORT: int = 8889
ADDITIONAL_SERVICE_BUS_PORT: int = 8887
DEFAULT_TABLE_STORAGE_PORT: int = 8891
DEFAULT_BLOB_STORAGE_PORT: int = 8891
DEFAULT_QUEUE_STORAGE_PORT: int = 8891
DEFAULT_FILE_STORAGE_PORT: int = 8891
DEFAULT_EVENT_HUB_PORT: int = 8897
DEFAULT_KEY_VAULT_PORT: int = 8898
DEFAULT_RESOURCE_MANAGER_PORT: int = 8899
CONTAINER_REGISTRY_PORT: int = 8892
AMQP_TLS_CONNECTION_PORT: int = 5671

KEY_VAULT_DNS_SUFFIX: str = "vault.topaz.local.dev"
AZURE_WEBSITES_DNS_SUFFIX: str = "azurewebsites.topaz.local.dev"


class TopazResourceHelpers:
    """Static helpers that return correct endpoint URLs and connection strings."""

    @staticmethod
    def get_container_registry_login_server(registry_name: str) -> str:
        """Returns the Container Registry login server host:port string."""
        return f"{registry_name.lower()}.cr.topaz.local.dev:{CONTAINER_REGISTRY_PORT}"

    @staticmethod
    def get_key_vault_endpoint(vault_name: str) -> str:
        """
        Returns the Key Vault HTTPS endpoint for the given vault name.

        Example: ``"https://my-vault.vault.topaz.local.dev:8898"``
        """
        return f"https://{vault_name.lower()}.{KEY_VAULT_DNS_SUFFIX}:{DEFAULT_KEY_VAULT_PORT}"

    @staticmethod
    def get_storage_connection_string(storage_account_name: str, account_key: str) -> str:
        """
        Returns a full Azure Storage connection string pointing at the local
        Topaz emulator endpoints for Blob, Queue, and Table Storage.
        """
        blob = (
            f"https://{storage_account_name}.blob.storage.topaz.local.dev"
            f":{DEFAULT_BLOB_STORAGE_PORT}/"
        )
        blob_secondary = (
            f"https://{storage_account_name}-secondary.blob.storage.topaz.local.dev"
            f":{DEFAULT_BLOB_STORAGE_PORT}/"
        )
        queue = (
            f"https://{storage_account_name}.queue.storage.topaz.local.dev"
            f":{DEFAULT_QUEUE_STORAGE_PORT}/"
        )
        queue_secondary = (
            f"https://{storage_account_name}-secondary.queue.storage.topaz.local.dev"
            f":{DEFAULT_QUEUE_STORAGE_PORT}/"
        )
        table = (
            f"https://{storage_account_name}.table.storage.topaz.local.dev"
            f":{DEFAULT_TABLE_STORAGE_PORT}"
        )
        table_secondary = (
            f"https://{storage_account_name}-secondary.table.storage.topaz.local.dev"
            f":{DEFAULT_TABLE_STORAGE_PORT}"
        )
        return (
            f"DefaultEndpointsProtocol=http;"
            f"AccountName={storage_account_name};"
            f"AccountKey={account_key};"
            f"BlobEndpoint={blob};"
            f"BlobSecondaryEndpoint={blob_secondary};"
            f"QueueEndpoint={queue};"
            f"QueueSecondaryEndpoint={queue_secondary};"
            f"TableEndpoint={table};"
            f"TableSecondaryEndpoint={table_secondary};"
        )

    @staticmethod
    def get_blob_service_uri(storage_account_name: str) -> str:
        """Returns the Blob service HTTPS URI for the given storage account."""
        return (
            f"https://{storage_account_name}.blob.storage.topaz.local.dev"
            f":{DEFAULT_BLOB_STORAGE_PORT}/"
        )

    @staticmethod
    def get_queue_service_uri(storage_account_name: str) -> str:
        """Returns the Queue service HTTPS URI for the given storage account."""
        return (
            f"https://{storage_account_name}.queue.storage.topaz.local.dev"
            f":{DEFAULT_QUEUE_STORAGE_PORT}/"
        )

    @staticmethod
    def get_table_service_uri(storage_account_name: str) -> str:
        """Returns the Table service HTTPS URI for the given storage account."""
        return (
            f"https://{storage_account_name}.table.storage.topaz.local.dev"
            f":{DEFAULT_TABLE_STORAGE_PORT}/"
        )

    @staticmethod
    def get_service_bus_connection_string(namespace_name: str) -> str:
        """Returns the Service Bus AMQP connection string (non-TLS, dev emulator)."""
        return (
            f"Endpoint=sb://{namespace_name}.servicebus.topaz.local.dev"
            f":{DEFAULT_SERVICE_BUS_AMQP_PORT};"
            f"SharedAccessKeyName=RootManageSharedAccessKey;"
            f"SharedAccessKey=SAS_KEY_VALUE;"
            f"UseDevelopmentEmulator=true;"
        )

    @staticmethod
    def get_service_bus_connection_string_with_tls(namespace_name: str) -> str:
        """Returns the Service Bus AMQP connection string over TLS (port 5671)."""
        return (
            f"Endpoint=sb://{namespace_name}.servicebus.topaz.local.dev"
            f":{AMQP_TLS_CONNECTION_PORT};"
            f"SharedAccessKeyName=RootManageSharedAccessKey;"
            f"SharedAccessKey=SAS_KEY_VALUE;"
        )

    @staticmethod
    def get_service_bus_connection_string_for_management(namespace_name: str) -> str:
        """Returns the Service Bus connection string for management operations."""
        return (
            f"Endpoint=sb://{namespace_name}.servicebus.topaz.local.dev"
            f":{ADDITIONAL_SERVICE_BUS_PORT};"
            f"SharedAccessKeyName=RootManageSharedAccessKey;"
            f"SharedAccessKey=SAS_KEY_VALUE;"
        )

    @staticmethod
    def get_event_hub_connection_string(namespace_name: str) -> str:
        """Returns the Event Hub AMQP connection string (dev emulator)."""
        return (
            f"Endpoint=sb://{namespace_name}.eventhub.topaz.local.dev"
            f":{DEFAULT_EVENT_HUB_AMQP_PORT};"
            f"SharedAccessKeyName=RootManageSharedAccessKey;"
            f"SharedAccessKey=SAS_KEY_VALUE;"
            f"UseDevelopmentEmulator=true;"
        )

    @staticmethod
    def get_container_registry_login_server(registry_name: str) -> str:
        """Returns the Container Registry login server host:port string."""
        return f"{registry_name}.cr.topaz.local.dev:{CONTAINER_REGISTRY_PORT}"

    @staticmethod
    def get_web_site_default_host_name(site_name: str) -> str:
        """Returns the default hostname for an App Service site."""
        return f"{site_name}.{AZURE_WEBSITES_DNS_SUFFIX}"
