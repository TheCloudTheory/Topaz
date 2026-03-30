---
sidebar_position: 4
---

import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';

# Azure CLI integration

Topaz exposes a custom Azure cloud environment that Azure CLI can register and authenticate against, letting you use `az` commands locally without touching real Azure resources. This guide walks through the full setup end-to-end.

## Prerequisites

- Azure CLI installed (`az --version` to verify)
- Topaz installed and the certificate trusted at the OS level (see [Getting started](./intro.md))
- A Microsoft Entra ID tenant you can log into (a free/personal tenant works fine)

:::tip[Use a dedicated test tenant]

Creating a dedicated Entra ID tenant for local development disables stricter security policies like Conditional Access, which can otherwise interrupt the `az login` flow. Head to [portal.azure.com](https://portal.azure.com) → *Manage Microsoft Entra ID* → *Create a tenant* to create a free one.

:::

## Step 1 — Trust the certificate in Azure CLI

Azure CLI ships with its own Python-based certificate bundle and does **not** automatically pick up certificates trusted at the OS level. Until the Topaz certificate is added to that bundle, `az` commands will fail with:

```
SSLError: [SSL: CERTIFICATE_VERIFY_FAILED] certificate verify failed: unable to get local issuer certificate
```

### Automated (recommended)

Run the configuration script from the Topaz repository. It detects your OS and architecture, backs up the existing bundle, and appends the Topaz certificate:

<Tabs groupId="os">
<TabItem value="macos" label="macOS">

```bash
# From the Topaz repo root
sudo bash install/configure-azure-cli-cert.sh
```

The script looks for the Azure CLI bundle at:
- **Intel**: `/usr/local/Cellar/azure-cli/*/libexec/lib/python*/site-packages/certifi/cacert.pem`
- **Apple Silicon**: `/opt/homebrew/Cellar/azure-cli/*/libexec/lib/python*/site-packages/certifi/cacert.pem`

</TabItem>
<TabItem value="linux" label="Linux / WSL">

```bash
# From the Topaz repo root
sudo bash install/configure-azure-cli-cert.sh
```

The script looks for the Azure CLI bundle at:
- **Ubuntu/Debian**: `/opt/az/lib/python*/site-packages/certifi/cacert.pem`
- **RHEL/CentOS/SUSE**: `/usr/lib64/az/lib/python*/site-packages/certifi/cacert.pem`

</TabItem>
</Tabs>

The script is idempotent — safe to run multiple times. It will prompt you before reinstalling an already-present certificate. A timestamped backup of the original bundle is created, and the script prints instructions for restoring it if needed.

### Manual

If you prefer not to run the script, follow the [official Azure CLI guide](https://learn.microsoft.com/en-gb/cli/azure/use-azure-cli-successfully-troubleshooting?view=azure-cli-latest#work-behind-a-proxy) to set `REQUESTS_CA_BUNDLE` to a bundle file that includes the Topaz certificate.

## Step 2 — Start the emulator with a tenant ID

Azure CLI needs to resolve Entra ID metadata endpoints when you log in. Topaz intercepts these, but only when started with the `--tenant-id` option pointing at your real Entra ID tenant:

```bash
topaz start \
  --tenant-id <your-entra-tenant-id> \
  --default-subscription 00000000-0000-0000-0000-000000000001 \
  --log-level Information
```

`--default-subscription` is optional but recommended — it creates the subscription automatically so you don't need a separate CLI command.

Keep the emulator running in the background for the remaining steps.

## Step 3 — Register the Topaz cloud environment

Azure CLI supports custom cloud endpoints (as used by Azure Stack). Topaz registers itself the same way. Download the `cloud.json` configuration file and register it:

```bash
# Download the cloud configuration
curl -fsSL https://raw.githubusercontent.com/TheCloudTheory/Topaz/refs/heads/main/cloud.json \
  -o cloud.json

# Register the cloud and switch to it
az cloud register -n Topaz --cloud-config @"cloud.json"
az cloud set -n Topaz
```

Expected output:
```
Switched active cloud to 'Topaz'.
Use 'az login' to log in to this cloud.
Use 'az account set' to set the active subscription.
```

## Step 4 — Log in

Topaz's Entra ID endpoint is not in the standard Azure instance discovery list, so you must disable instance discovery before logging in. **Remember to re-enable it when you switch back to real Azure.**

```bash
export AZURE_CORE_INSTANCE_DISCOVERY=false
az login
```

A browser window will open for the standard Microsoft login flow. After authentication you should see your local subscription listed:

```
[Tenant and subscription selection]

No     Subscription name    Subscription ID                       Tenant
-----  -------------------  ------------------------------------  -----------------------
[1] *  dev-local            00000000-0000-0000-0000-000000000001  Topaz Cloud Environment
```

:::tip[Headless / WSL environments]

If no browser is available (e.g. in WSL without a desktop), use device code flow:

```bash
az login --use-device-code
```

You'll receive a code to enter at `https://microsoft.com/devicelogin` from any browser.

:::

## Step 5 — Verify and use

Confirm Azure CLI is talking to Topaz:

```bash
az account list
az account show
```

Now use `az` commands as normal. For example:

<Tabs groupId="service">
<TabItem value="rg" label="Resource Groups">

```bash
az group create --name "rg-local" --location "westeurope"
az group list
az group delete --name "rg-local" --yes
```

</TabItem>
<TabItem value="keyvault" label="Key Vault">

```bash
az keyvault create \
  --name "kv-local" \
  --resource-group "rg-local" \
  --location "westeurope"

az keyvault secret set \
  --vault-name "kv-local" \
  --name "my-secret" \
  --value "hello-topaz"

az keyvault secret show \
  --vault-name "kv-local" \
  --name "my-secret"
```

</TabItem>
<TabItem value="storage" label="Storage">

```bash
az storage account create \
  --name "stlocal001" \
  --resource-group "rg-local" \
  --location "westeurope" \
  --sku Standard_LRS

az storage container create \
  --name "my-container" \
  --account-name "stlocal001"
```

</TabItem>
<TabItem value="servicebus" label="Service Bus">

```bash
az servicebus namespace create \
  --name "sb-local" \
  --resource-group "rg-local" \
  --location "westeurope"

az servicebus queue create \
  --name "my-queue" \
  --namespace-name "sb-local" \
  --resource-group "rg-local"
```

</TabItem>
</Tabs>

## Switching back to real Azure

When you're done with local development, switch Azure CLI back to the public cloud and re-enable instance discovery:

```bash
az cloud set -n AzureCloud
export AZURE_CORE_INSTANCE_DISCOVERY=true
az login
```

Resources created in Topaz are unaffected — they remain available the next time you switch back and start the emulator.

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `CERTIFICATE_VERIFY_FAILED` | Azure CLI bundle not updated | Re-run `configure-azure-cli-cert.sh` |
| `az login` hangs / no browser | Running in WSL headless | Use `az login --use-device-code` |
| `InteractionRequiredAuthError` | Conditional Access policy on tenant | Use a dedicated test tenant (see Prerequisites) |
| `az` commands return 404 | Wrong cloud active | Run `az cloud show` to confirm `Topaz` is selected |
| Subscription not found | No subscription created | Add `--default-subscription` to `topaz start` |

