param(
    [string] $ResourceManagerUrl = "https://topaz.local.dev:8899",
    [string] $TenantId           = "50717675-3E5E-4A1E-8CB5-C62D8BE8CA48"
)

$ErrorActionPreference = "Stop"

# Az modules are pre-installed in the Docker image.
Import-Module Az.Accounts -Force

# Keep context in-memory only — enabling autosave triggers MSAL to persist tokens
# via the OS keyring (libsecret), which hangs in Docker containers without a keyring daemon.
Disable-AzContextAutosave | Out-Null

# Re-register the Topaz custom environment (idempotent, no network call).
# Required because each new pwsh process starts without any custom environment definitions.
if (-not (Get-AzEnvironment -Name "Topaz" -ErrorAction SilentlyContinue))
{
    $null = Add-AzEnvironment `
        -Name                                    "Topaz" `
        -ResourceManagerUrl                      $ResourceManagerUrl `
        -ActiveDirectoryAuthority                "$ResourceManagerUrl/" `
        -ActiveDirectoryServiceEndpointResourceId $ResourceManagerUrl `
        -GraphEndpointResourceId                 $ResourceManagerUrl `
        -GraphUrl                                $ResourceManagerUrl `
        -StorageEndpointSuffix                   "storage.topaz.local.dev" `
        -AzureKeyVaultDnsSuffix                  "vault.topaz.local.dev" `
        -AzureKeyVaultServiceEndpointResourceId  $ResourceManagerUrl
}

# Restore the previously established context from the file saved by setup-az-environment.ps1.
# This is a file read — no Connect-AzAccount, no network call, no MSAL warmup.
Import-AzContext -Path /tmp/az-context.json | Out-Null
