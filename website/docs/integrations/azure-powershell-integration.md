---
sidebar_position: 2
slug: /azure-powershell-integration
description: Configure the Az PowerShell module to connect to Topaz and run cmdlets against your local Azure emulator. Register the cloud environment, authenticate, and manage emulated resources without a real Azure subscription.
keywords: [azure powershell local, az module topaz, azure powershell emulator, local azure powershell, connect-azaccount topaz]
---

import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';

# Azure PowerShell integration

Topaz exposes a custom Azure cloud environment that the Az PowerShell module can register and authenticate against, letting you run `*-Az*` cmdlets locally without touching real Azure resources. This guide walks through the full setup end-to-end.

## Prerequisites

- PowerShell 7+ (`pwsh --version` to verify)
- Az module installed (`Install-Module -Name Az -Force -Scope CurrentUser -Repository PSGallery`)
- Topaz installed and the host running (see [Getting started](../intro.md))

## Step 1 — Trust the certificate

Az PowerShell uses the .NET `HttpClient` stack, which on most platforms respects the OS certificate store. If the Topaz certificate is already trusted at the OS level (for example, you ran `topaz start` and accepted the certificate prompt), Az PowerShell will work without additional configuration.

If you see `The SSL connection could not be established` errors, run the configuration script from the Topaz repository:

<Tabs groupId="os">
<TabItem value="macos" label="macOS">

```powershell
# From the Topaz repo root — requires sudo for the System keychain
pwsh ./install/configure-azure-powershell-cert.ps1
```

The script adds the Topaz certificate to the macOS System keychain via `security add-trusted-cert`.

</TabItem>
<TabItem value="linux" label="Linux / WSL">

```powershell
# From the Topaz repo root — requires sudo for update-ca-certificates
pwsh ./install/configure-azure-powershell-cert.ps1
```

The script copies the certificate to `/usr/local/share/ca-certificates/topaz.crt` and runs `update-ca-certificates`.

</TabItem>
<TabItem value="windows" label="Windows">

```powershell
# From the Topaz repo root — run as a regular user
pwsh ./install/configure-azure-powershell-cert.ps1
```

The script imports the certificate into `CurrentUser\Root`.

</TabItem>
</Tabs>

The script is idempotent — safe to run multiple times.

## Step 2 — Start the emulator

```bash
topaz start \
  --default-subscription 00000000-0000-0000-0000-000000000001 \
  --log-level Information
```

`--default-subscription` creates the subscription automatically so you don't need a separate command later.

Keep the emulator running in the background for the remaining steps.

## Step 3 — Register the Topaz cloud environment and authenticate

Run the configuration script. It registers Topaz as a named Az environment and authenticates using the built-in admin account:

```powershell
pwsh ./install/configure-azure-powershell-env.ps1
```

Expected output:

```
Registering Az environment 'Topaz'...
Environment registered.
Authenticating as 'topazadmin@topaz.local.dev'...
Connected to Topaz successfully.
```

### What the script does

The script calls `Add-AzEnvironment` once to register all Topaz endpoint URLs, then authenticates using `Connect-AzAccount` with the Resource Owner Password Credentials (ROPC) grant — the same grant that `az login --username` uses:

```powershell
Add-AzEnvironment `
    -Name                                     "Topaz" `
    -ResourceManagerUrl                       "https://topaz.local.dev:8899" `
    -ActiveDirectoryAuthority                 "https://topaz.local.dev:8899/" `
    -ActiveDirectoryServiceEndpointResourceId "https://topaz.local.dev:8899" `
    -GraphEndpointResourceId                  "https://topaz.local.dev:8899" `
    -GraphUrl                                 "https://topaz.local.dev:8899" `
    -StorageEndpointSuffix                    "storage.topaz.local.dev" `
    -AzureKeyVaultDnsSuffix                   "vault.topaz.local.dev" `
    -AzureKeyVaultServiceEndpointResourceId   "https://topaz.local.dev:8899"

$cred = New-Object PSCredential("topazadmin@topaz.local.dev",
    (ConvertTo-SecureString "admin" -AsPlainText -Force))

Connect-AzAccount `
    -Environment "Topaz" `
    -Credential  $cred `
    -TenantId    "50717675-3E5E-4A1E-8CB5-C62D8BE8CA48"
```

### Manual — custom user account

If you created your own user in Topaz, substitute their UPN and password:

```powershell
$cred = New-Object PSCredential("<upn>@topaz.local.dev",
    (ConvertTo-SecureString "<password>" -AsPlainText -Force))

Connect-AzAccount `
    -Environment "Topaz" `
    -Credential  $cred `
    -TenantId    "50717675-3E5E-4A1E-8CB5-C62D8BE8CA48"
```

:::note[Context autosave and keyring]

On Linux, Az PowerShell can attempt to persist tokens via the OS keyring daemon (`libsecret`). In environments without a keyring (containers, CI, WSL without a desktop session) this causes `Connect-AzAccount` to hang indefinitely.

Disable autosave **before** connecting:

```powershell
Disable-AzContextAutosave | Out-Null
```

This only affects the current session — tokens are kept in memory and lost when PowerShell exits. If you need the context across multiple processes in the same session (for example, in a test harness), use `Save-AzContext` / `Import-AzContext` to persist to and restore from a file explicitly.

:::

## Step 4 — Verify and use

Confirm the Az module is talking to Topaz:

```powershell
Get-AzSubscription
Get-AzContext
```

Now use Az cmdlets as normal. For example:

<Tabs groupId="service">
<TabItem value="rg" label="Resource Groups">

```powershell
New-AzResourceGroup -Name "rg-local" -Location "westeurope"
Get-AzResourceGroup
Remove-AzResourceGroup -Name "rg-local" -Force
```

</TabItem>
<TabItem value="keyvault" label="Key Vault">

```powershell
New-AzKeyVault `
    -Name "kv-local" `
    -ResourceGroupName "rg-local" `
    -Location "westeurope"

Set-AzKeyVaultSecret `
    -VaultName "kv-local" `
    -Name "my-secret" `
    -SecretValue (ConvertTo-SecureString "hello-topaz" -AsPlainText -Force)

Get-AzKeyVaultSecret `
    -VaultName "kv-local" `
    -Name "my-secret" `
    -AsPlainText
```

</TabItem>
<TabItem value="storage" label="Storage">

```powershell
New-AzStorageAccount `
    -Name "stlocal001" `
    -ResourceGroupName "rg-local" `
    -Location "westeurope" `
    -SkuName Standard_LRS

New-AzStorageContainer `
    -Name "my-container" `
    -Context (New-AzStorageContext -StorageAccountName "stlocal001")
```

</TabItem>
<TabItem value="servicebus" label="Service Bus">

```powershell
New-AzServiceBusNamespace `
    -Name "sb-local" `
    -ResourceGroupName "rg-local" `
    -Location "westeurope"

New-AzServiceBusQueue `
    -Name "my-queue" `
    -NamespaceName "sb-local" `
    -ResourceGroupName "rg-local"
```

</TabItem>
</Tabs>

## Switching back to real Azure

```powershell
Set-AzContext -Environment AzureCloud
Connect-AzAccount
```

Resources created in Topaz are unaffected — they remain available the next time you switch back and start the emulator.

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `The SSL connection could not be established` | Topaz cert not trusted | Run `configure-azure-powershell-cert.ps1` |
| `Connect-AzAccount` hangs indefinitely | Keyring daemon absent (Linux / container) | Run `Disable-AzContextAutosave \| Out-Null` before connecting |
| `AuthenticationFailedException` | Wrong UPN or password | Ensure the UPN includes the full domain: `user@topaz.local.dev` |
| `Get-AzResourceGroup` returns nothing | Wrong environment active | Run `(Get-AzContext).Environment.Name` — should be `Topaz` |
| Subscription not found after login | No subscription created | Add `--default-subscription` to `topaz start` |
| `InvalidOperation: the provided credentials...` | MSAL hitting real AAD for instance discovery | Ensure `-TenantId` matches Topaz's tenant (`50717675-3E5E-4A1E-8CB5-C62D8BE8CA48`) |
