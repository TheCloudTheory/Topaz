---
sidebar_position: 6
slug: /azcopy-integration
description: Use azcopy with Topaz to upload blobs from your local machine and copy blobs between emulated storage accounts — all without a real Azure subscription.
keywords: [azcopy topaz, azcopy local, azcopy emulator, blob copy local, azcopy storage account]
---

import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';

# How to use azcopy with Topaz

This guide shows you how to configure [azcopy](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azcopy-v10) to work against Topaz's emulated blob storage.

Two scenarios are covered:

1. **Upload a local file** to an emulated storage account blob container.
2. **Copy a blob between two emulated storage accounts** (server-to-server / S2S copy).

## Prerequisites

- [azcopy v10](https://github.com/Azure/azure-storage-azcopy/releases) installed
- Topaz installed and running, with the certificate trusted at the OS level (see [Getting started](../intro.md))
- The Topaz wildcard certificate trusted by azcopy (see below)

## Step 1 — Trust the Topaz certificate

azcopy uses Go's TLS stack, which reads from the **OS certificate store** (not the Azure CLI Python bundle). Trust the Topaz certificate once at the OS level:

<Tabs groupId="os">
<TabItem value="macos" label="macOS">

```bash
# From the Topaz repository root
./install/trust-cert-macos.sh
```

</TabItem>
<TabItem value="linux" label="Linux / WSL">

```bash
# Ubuntu/Debian
sudo cp certificate/topaz.crt /usr/local/share/ca-certificates/topaz.crt
sudo update-ca-certificates

# RHEL/CentOS/Fedora
sudo cp certificate/topaz.crt /etc/pki/ca-trust/source/anchors/topaz.crt
sudo update-ca-trust
```

</TabItem>
</Tabs>

## Step 2 — Create a storage account and generate a SAS token

Use the Azure CLI (or the Topaz CLI) to set up the resources. The examples below use `az`.

```bash
# Create a resource group and storage account
az group create -n rg-demo -l westeurope
az storage account create -n topazdemo -g rg-demo -l westeurope --sku Standard_LRS

# Retrieve the account key
KEY=$(az storage account keys list -n topazdemo -g rg-demo --query '[0].value' -o tsv)

# Create a container
az storage container create -n uploads \
  --connection-string "DefaultEndpointsProtocol=https;AccountName=topazdemo;AccountKey=$KEY;BlobEndpoint=https://topazdemo.blob.storage.topaz.local.dev:8891;"

# Generate a container-level SAS token (write + create permissions)
SAS=$(az storage container generate-sas -n uploads \
  --permissions rwdl \
  --expiry 2099-01-01T00:00:00Z \
  --connection-string "DefaultEndpointsProtocol=https;AccountName=topazdemo;AccountKey=$KEY;BlobEndpoint=https://topazdemo.blob.storage.topaz.local.dev:8891;" \
  -o tsv)
```

## Scenario 1 — Upload a local file

```bash
azcopy copy /path/to/file.txt \
  "https://topazdemo.blob.storage.topaz.local.dev:8891/uploads/file.txt?$SAS" \
  --trusted-microsoft-suffixes=blob.storage.topaz.local.dev
```

Verify the upload:

```bash
az storage blob list -c uploads \
  --connection-string "DefaultEndpointsProtocol=https;AccountName=topazdemo;AccountKey=$KEY;BlobEndpoint=https://topazdemo.blob.storage.topaz.local.dev:8891;" \
  --output table
```

## Scenario 2 — Copy a blob between two storage accounts

This scenario uses azcopy's **server-to-server (S2S) copy**, where the data is moved entirely within Topaz without streaming through the client.

```bash
# Create a second storage account in a different region
az storage account create -n topazdest -g rg-demo -l eastus --sku Standard_LRS
KEY2=$(az storage account keys list -n topazdest -g rg-demo --query '[0].value' -o tsv)

az storage container create -n archive \
  --connection-string "DefaultEndpointsProtocol=https;AccountName=topazdest;AccountKey=$KEY2;BlobEndpoint=https://topazdest.blob.storage.topaz.local.dev:8891;"

# Generate SAS tokens for both containers
SAS_SRC=$(az storage container generate-sas -n uploads \
  --permissions rl \
  --expiry 2099-01-01T00:00:00Z \
  --connection-string "DefaultEndpointsProtocol=https;AccountName=topazdemo;AccountKey=$KEY;BlobEndpoint=https://topazdemo.blob.storage.topaz.local.dev:8891;" \
  -o tsv)

SAS_DST=$(az storage container generate-sas -n archive \
  --permissions rwdl \
  --expiry 2099-01-01T00:00:00Z \
  --connection-string "DefaultEndpointsProtocol=https;AccountName=topazdest;AccountKey=$KEY2;BlobEndpoint=https://topazdest.blob.storage.topaz.local.dev:8891;" \
  -o tsv)

# Server-to-server copy
azcopy copy \
  "https://topazdemo.blob.storage.topaz.local.dev:8891/uploads/file.txt?$SAS_SRC" \
  "https://topazdest.blob.storage.topaz.local.dev:8891/archive/file.txt?$SAS_DST" \
  --trusted-microsoft-suffixes=blob.storage.topaz.local.dev
```

:::note
`--trusted-microsoft-suffixes` tells azcopy to treat `blob.storage.topaz.local.dev` as an Azure Storage endpoint. Without it, azcopy will not recognise the custom domain and may fall back to an incompatible code path.
:::

## Known limitations

### `x-ms-requires-sync` not sent by azcopy

When performing a blob-to-blob S2S copy, azcopy v10 internally calls the Go SDK's synchronous `CopyFromURL()` method (which expects HTTP 201) but omits the `x-ms-requires-sync: true` request header that the REST spec requires for that path. Real Azure returns 201 regardless; Topaz detects the azcopy User-Agent and matches that behaviour.
