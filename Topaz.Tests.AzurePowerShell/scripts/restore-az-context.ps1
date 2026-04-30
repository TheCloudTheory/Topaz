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
# Then eagerly probe the token: if the access token has expired (suite run > ~1 h) the
# SharedTokenCacheCredential silent-refresh will fail in a new pwsh process that has no
# MSAL in-memory cache.  Catch that and fall back to a fresh ROPC login against Topaz
# (instantaneous — no real AAD roundtrip) and update the on-disk context for later tests.
$authenticated = $false
if (Test-Path /tmp/az-context.json) {
    Import-AzContext -Path /tmp/az-context.json | Out-Null
    try {
        $null = Get-AzAccessToken -ResourceUrl $ResourceManagerUrl -ErrorAction Stop
        $authenticated = $true
    } catch {
        # Token expired / silent refresh failed — fall through to re-authenticate.
        $authenticated = $false
    }
}

if (-not $authenticated) {
    $pw   = ConvertTo-SecureString "admin" -AsPlainText -Force
    $cred = New-Object PSCredential("topazadmin@topaz.local.dev", $pw)
    Connect-AzAccount `
        -Environment "Topaz" `
        -Credential  $cred `
        -TenantId    $TenantId | Out-Null
    # Persist the refreshed context so subsequent tests in this run can reuse it.
    Save-AzContext -Path /tmp/az-context.json -Force | Out-Null
}
