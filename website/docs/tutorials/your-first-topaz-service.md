---
sidebar_position: 9
description: Your first complete Topaz workflow — create a storage account, upload a blob, and retrieve it. Completes in about 15 minutes with no prior Azure knowledge required.
keywords: [topaz getting started tutorial, azure emulator first steps, topaz beginner, local azure blob first tutorial]
---

# Your first Topaz service

In this tutorial, we will create an Azure Storage Account on Topaz, upload a blob to it, and retrieve it — all from the command line. By the end, you will have a complete local Azure workflow running on your machine.

No prior Azure knowledge is required. You will need Topaz installed and set up (see [Getting started](../intro.md)).

## What we will build

- A local Storage Account named `stfirstdemo`
- A blob container named `files`
- A text file uploaded and then downloaded back

:::note[Before you start]
Topaz must be installed, running, and the Azure CLI pointed at it. Follow [Getting started](../intro.md) and [Azure CLI integration](../integrations/azure-cli-integration.md) if you have not done this yet, then run:

```bash
az cloud set -n Topaz
export AZURE_CORE_INSTANCE_DISCOVERY=false
az login
az account set --subscription 00000000-0000-0000-0000-000000000001
```

Verify Topaz is ready:

```bash
topaz health
```

You should see `Status: Healthy` before proceeding.
:::

## Step 1: Create a resource group

Resources in Azure are organised into resource groups. Let's create one:

```bash
az group create \
  --name rg-first \
  --location westeurope
```

You should see a JSON response with `"provisioningState": "Succeeded"`. Notice that the resource group name and location are now registered in Topaz.

## Step 2: Create a Storage Account

```bash
az storage account create \
  --name stfirstdemo \
  --resource-group rg-first \
  --location westeurope \
  --sku Standard_LRS \
  --kind StorageV2
```

The command returns a JSON document describing the new storage account. Look for:

```json
"name": "stfirstdemo",
"provisioningState": "Succeeded"
```

Notice that Topaz assigned the account a local hostname: `stfirstdemo.blob.storage.topaz.local.dev`. That hostname resolves to `127.0.0.1` via the DNS setup you ran during installation.

## Step 3: Get the storage account key

```bash
STORAGE_KEY=$(az storage account keys list \
  --account-name stfirstdemo \
  --resource-group rg-first \
  --query "[0].value" \
  --output tsv)

echo "Key retrieved: ${STORAGE_KEY:0:8}..."
```

You should see a partial key printed, confirming the variable is set.

## Step 4: Create a container

```bash
az storage container create \
  --name files \
  --account-name stfirstdemo \
  --account-key "$STORAGE_KEY" \
  --blob-endpoint "https://stfirstdemo.blob.storage.topaz.local.dev:8891"
```

You should see:

```json
{
  "created": true
}
```

## Step 5: Upload a blob

Create a small text file and upload it:

```bash
echo "Hello from Topaz!" > hello.txt

az storage blob upload \
  --container-name files \
  --file hello.txt \
  --name hello.txt \
  --account-name stfirstdemo \
  --account-key "$STORAGE_KEY" \
  --blob-endpoint "https://stfirstdemo.blob.storage.topaz.local.dev:8891"
```

You should see:

```
Finished[#############################################################]  100.0000%
```

## Step 6: List the blobs

```bash
az storage blob list \
  --container-name files \
  --account-name stfirstdemo \
  --account-key "$STORAGE_KEY" \
  --blob-endpoint "https://stfirstdemo.blob.storage.topaz.local.dev:8891" \
  --output table
```

You should see:

```
Name       Blob Type    Blob Tier    Length    Content Type
---------  -----------  -----------  --------  --------------
hello.txt  BlockBlob    Hot          18        application/octet-stream
```

Notice that `hello.txt` is listed with a length of 18 bytes — the length of "Hello from Topaz!\n".

## Step 7: Download the blob

```bash
az storage blob download \
  --container-name files \
  --name hello.txt \
  --file downloaded.txt \
  --account-name stfirstdemo \
  --account-key "$STORAGE_KEY" \
  --blob-endpoint "https://stfirstdemo.blob.storage.topaz.local.dev:8891"

cat downloaded.txt
```

You should see:

```
Hello from Topaz!
```

## You've now built

You have a working local Azure Storage environment. You created a Storage Account with Terraform-compatible ARM control plane operations, stored a blob in it, and retrieved it — all running locally on your machine without a real Azure subscription.

The exact same `az storage` commands work against real Azure Storage with only the `--blob-endpoint` removed (Azure CLI uses the public endpoint by default when not specified).

## Next steps

- [Local Blob Storage development](./local-blob-storage-development.md) — deeper coverage: SDK clients, metadata, SAS tokens
- [Local Key Vault development](./local-key-vault-development.md) — store and retrieve secrets locally
- [How Topaz works](../concepts/how-topaz-works.md) — understand the DNS and TLS interception model
