param(
    [string] $ResourceManagerUrl = "https://topaz.local.dev:8899",
    [string] $TenantId           = "50717675-3E5E-4A1E-8CB5-C62D8BE8CA48"
)

$ErrorActionPreference = "Stop"

# Az modules are pre-installed in the Docker image (see Dockerfile.powershell).
# Import Az.Accounts explicitly to warm up the assembly loader before any cmdlets are used.
Import-Module Az.Accounts -Force

# Keep the context in-memory only. Enabling autosave triggers MSAL to try
# persisting tokens via the OS keyring (libsecret), which hangs in Docker
# containers that have no keyring daemon. Everything runs in one pwsh process
# per test invocation so disk persistence is not needed.
Disable-AzContextAutosave | Out-Null

# Register the Topaz cloud environment (idempotent across repeated runs).
if (-not (Get-AzEnvironment -Name "Topaz" -ErrorAction SilentlyContinue))
{
    $null = Add-AzEnvironment `
        -Name                                "Topaz" `
        -ResourceManagerUrl                  $ResourceManagerUrl `
        -ActiveDirectoryAuthority            "$ResourceManagerUrl/" `
        -ActiveDirectoryServiceEndpointResourceId $ResourceManagerUrl `
        -GraphEndpointResourceId             $ResourceManagerUrl `
        -GraphUrl                            $ResourceManagerUrl `
        -StorageEndpointSuffix               "storage.topaz.local.dev" `
        -AzureKeyVaultDnsSuffix              "vault.topaz.local.dev" `
        -AzureKeyVaultServiceEndpointResourceId $ResourceManagerUrl
}

# Authenticate using the username/password ROPC flow — the same grant that
# `az login --username` uses. Unlike -ServicePrincipal, this does not trigger the
# MSAL "combined flat storage" restriction that affects ClientSecretCredential.
# The full UPN (topazadmin@topaz.local.dev) is required by Topaz's UserDataPlane lookup.
$pw   = ConvertTo-SecureString "admin" -AsPlainText -Force
$cred = New-Object PSCredential("topazadmin@topaz.local.dev", $pw)

Connect-AzAccount `
    -Environment          "Topaz" `
    -Credential           $cred `
    -TenantId             $TenantId | Out-Null

# Persist an explicit subscription context so new pwsh processes can reuse
# the saved context without relying on implicit context population.
$subscription = Get-AzSubscription | Select-Object -First 1
if ($null -ne $subscription)
{
    Set-AzContext -Subscription $subscription.Id -Tenant $TenantId | Out-Null
}

Write-Host "Topaz PowerShell setup complete."
