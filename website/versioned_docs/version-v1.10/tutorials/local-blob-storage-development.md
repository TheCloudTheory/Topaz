---
sidebar_position: 7
description: Use Topaz for Azure Blob Storage local development — create a storage account, upload and download blobs, manage containers, and connect the Azure SDK and Azure CLI without a real Azure subscription.
keywords: [azure blob storage local, blob storage local development, local blob storage emulator, topaz blob storage, azure storage emulator, azurite alternative, blob storage testing local]
---

import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';

# Local Blob Storage development with Topaz

In this tutorial, we will create a local Storage Account on Topaz, upload a blob, download it again, and connect to the storage account using the Azure SDK.

## What you will build

- A local Storage Account running on Topaz
- Containers and blobs managed via the Azure CLI
- .NET and Python snippets connecting to the local storage using `BlobServiceClient`
- A cleanup walkthrough

## Prerequisites

- Topaz installed and running (see [Getting started](../intro.md))
- DNS setup completed
- Topaz certificate trusted by your OS and tooling
- Azure CLI installed (`az --version`)
- Topaz cloud registered in Azure CLI (see [Azure CLI integration](../integrations/azure-cli-integration.md))

:::note[Before you start]
Topaz must be running and the Azure CLI pointed at it. See [Getting started](../intro.md) and [Azure CLI integration](../integrations/azure-cli-integration.md), then activate:

```bash
az cloud set -n Topaz
export AZURE_CORE_INSTANCE_DISCOVERY=false
az login
az account set --subscription 00000000-0000-0000-0000-000000000001
```
:::

## Step 1: Create a resource group and Storage Account

```bash
az group create \
  --name rg-local \
  --location westeurope

az storage account create \
  --name stlocaldev001 \
  --resource-group rg-local \
  --location westeurope \
  --sku Standard_LRS \
  --kind StorageV2
```

Topaz assigns the account a local hostname: `stlocaldev001.blob.storage.topaz.local.dev`, which resolves to `127.0.0.1` via the DNS setup you completed in the prerequisites.

## Step 2: Retrieve the storage account key

```bash
az storage account keys list \
  --account-name stlocaldev001 \
  --resource-group rg-local \
  --query "[0].value" \
  --output tsv
```

Store the key in a shell variable so you can reuse it in subsequent commands:

```bash
STORAGE_KEY=$(az storage account keys list \
  --account-name stlocaldev001 \
  --resource-group rg-local \
  --query "[0].value" \
  --output tsv)
```

## Step 3: Create a container and upload blobs

Create a container:

```bash
az storage container create \
  --name mycontainer \
  --account-name stlocaldev001 \
  --account-key "$STORAGE_KEY" \
  --blob-endpoint "https://stlocaldev001.blob.storage.topaz.local.dev:8891"
```

Upload a local file to the container:

```bash
echo "hello from topaz" > /tmp/hello.txt

az storage blob upload \
  --container-name mycontainer \
  --name hello.txt \
  --file /tmp/hello.txt \
  --account-name stlocaldev001 \
  --account-key "$STORAGE_KEY" \
  --blob-endpoint "https://stlocaldev001.blob.storage.topaz.local.dev:8891"
```

List blobs in the container:

```bash
az storage blob list \
  --container-name mycontainer \
  --account-name stlocaldev001 \
  --account-key "$STORAGE_KEY" \
  --blob-endpoint "https://stlocaldev001.blob.storage.topaz.local.dev:8891" \
  --output table
```

## Step 4: Download a blob

```bash
az storage blob download \
  --container-name mycontainer \
  --name hello.txt \
  --file /tmp/hello-downloaded.txt \
  --account-name stlocaldev001 \
  --account-key "$STORAGE_KEY" \
  --blob-endpoint "https://stlocaldev001.blob.storage.topaz.local.dev:8891"

cat /tmp/hello-downloaded.txt
```

Expected output: `hello from topaz`

## Step 5: Connect with the Azure SDK

<Tabs groupId="sdk-language">
<TabItem value="dotnet" label=".NET">

Install the Azure Storage Blobs client:

```bash
dotnet add package Azure.Storage.Blobs
```

Build a connection string pointing at Topaz and use `BlobServiceClient`:

```csharp
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

// Replace storageKey with the key retrieved in Step 4.
var connectionString =
    "DefaultEndpointsProtocol=https;" +
    "AccountName=stlocaldev001;" +
    $"AccountKey={storageKey};" +
    "BlobEndpoint=https://stlocaldev001.blob.storage.topaz.local.dev:8891/;";

var serviceClient = new BlobServiceClient(connectionString);

// Create a container
var containerClient = serviceClient.GetBlobContainerClient("sdk-container");
await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

// Upload text as a blob
var blobClient = containerClient.GetBlobClient("greeting.txt");
await blobClient.UploadAsync(BinaryData.FromString("Hello from the Azure SDK!"), overwrite: true);

// Download and print
var download = await blobClient.DownloadContentAsync();
Console.WriteLine(download.Value.Content.ToString()); // Hello from the Azure SDK!
```

</TabItem>
<TabItem value="python" label="Python">

Install the Topaz Python SDK and the Azure Storage Blobs client:

```bash
pip install topaz-sdk azure-storage-blob azure-mgmt-storage
```

Use `TopazResourceHelpers.get_storage_connection_string()` to build the connection string, then interact with Blob Storage through the standard `azure-storage-blob` client:

```python
import os
from azure.mgmt.storage import StorageManagementClient
from azure.storage.blob import BlobServiceClient
from topaz_sdk import (
    AzureLocalCredential,
    TopazResourceHelpers,
    GLOBAL_ADMIN_ID,
    DEFAULT_RESOURCE_MANAGER_PORT,
)

SUBSCRIPTION_ID = "00000000-0000-0000-0000-000000000001"
RESOURCE_GROUP  = "rg-local"
ACCOUNT_NAME    = "stlocaldev001"
BASE_URL        = f"https://topaz.local.dev:{DEFAULT_RESOURCE_MANAGER_PORT}"

credential = AzureLocalCredential(GLOBAL_ADMIN_ID)

# Retrieve the storage account key via the management API
storage_client = StorageManagementClient(
    credential=credential,
    subscription_id=SUBSCRIPTION_ID,
    base_url=BASE_URL,
    credential_scopes=[f"{BASE_URL}/.default"],
)
keys = storage_client.storage_accounts.list_keys(RESOURCE_GROUP, ACCOUNT_NAME)
account_key = keys.keys[0].value

# Build a connection string pointing at the local Topaz endpoints
connection_string = TopazResourceHelpers.get_storage_connection_string(
    ACCOUNT_NAME, account_key
)

service_client = BlobServiceClient.from_connection_string(connection_string)

# Create a container
container_client = service_client.get_container_client("sdk-container")
container_client.create_container()

# Upload text as a blob
blob_client = container_client.get_blob_client("greeting.txt")
blob_client.upload_blob(b"Hello from the Topaz Python SDK!", overwrite=True)

# Download and print
download = blob_client.download_blob()
print(download.readall().decode())  # Hello from the Topaz Python SDK!
```

:::note[Certificate trust]

Set `REQUESTS_CA_BUNDLE` to the path of the Topaz certificate so the Python HTTP stack trusts TLS connections:

```bash
export REQUESTS_CA_BUNDLE=/path/to/topaz.crt
```

:::

</TabItem>
</Tabs>

:::tip[Switching to production]

The only difference from production is the connection string. Replace it with the real Azure storage connection string from the Azure portal and the rest of the code — SDK calls, container and blob operations — is identical.

:::

## Step 6: Working with blob metadata

<Tabs groupId="sdk-language">
<TabItem value="dotnet" label=".NET">

```csharp
var metadata = new Dictionary<string, string>
{
    { "env", "local" },
    { "version", "1" }
};
await blobClient.SetMetadataAsync(metadata);

var properties = await blobClient.GetPropertiesAsync();
foreach (var (key, value) in properties.Value.Metadata)
    Console.WriteLine($"{key} = {value}");
```

</TabItem>
<TabItem value="python" label="Python">

```python
blob_client.set_blob_metadata({"env": "local", "version": "1"})

properties = blob_client.get_blob_properties()
for key, value in properties.metadata.items():
    print(f"{key} = {value}")
```

</TabItem>
</Tabs>

## Step 7: List and delete containers

List all containers in the account:

```bash
az storage container list \
  --account-name stlocaldev001 \
  --account-key "$STORAGE_KEY" \
  --blob-endpoint "https://stlocaldev001.blob.storage.topaz.local.dev:8891" \
  --output table
```

Delete a container (and all its blobs) when you're done:

```bash
az storage container delete \
  --name mycontainer \
  --account-name stlocaldev001 \
  --account-key "$STORAGE_KEY" \
  --blob-endpoint "https://stlocaldev001.blob.storage.topaz.local.dev:8891"
```

## Common gotchas

| Symptom | Cause | Fix |
|---|---|---|
| `SSL certificate error` | Topaz certificate not trusted by the CLI or SDK | Trust the certificate as described in [Getting started](../intro.md) |
| `ResourceNotFound` on blob upload | Container does not exist yet | Create the container first (`az storage container create`) |
| `AuthenticationFailed` | Wrong account key passed to `--account-key` | Re-fetch the key with `az storage account keys list` |
| `DNS resolution failed` | DNS not configured, or wrong hostname | Verify the DNS setup and that the account name matches the subdomain exactly |

## You've now built

You have a working local Azure Blob Storage environment running on Topaz. You created a Storage Account, uploaded and downloaded blobs with the Azure CLI, and connected to the storage account using the Azure SDK — all without a real Azure subscription or internet access. The same SDK code and CLI commands work against real Azure with only the endpoint hostname changed.
