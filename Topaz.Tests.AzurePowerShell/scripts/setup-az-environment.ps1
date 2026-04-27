param(
    [string] $ResourceManagerUrl = "https://topaz.local.dev:8899",
    [string] $TenantId           = "50717675-3E5E-4A1E-8CB5-C62D8BE8CA48"
)

$ErrorActionPreference = "Stop"

# Az modules are pre-installed in the Docker image (see Dockerfile.powershell).
# Import Az.Accounts explicitly to warm up the assembly loader before any cmdlets are used.
Import-Module Az.Accounts -Force

# Disable WAM (Windows Authentication Manager) — not available on Linux but safe to set;
# also ensures legacy credential flows are used rather than interactive browser.
Update-AzConfig -EnableLoginByWam $false | Out-Null

# Ensure context is saved to disk so subsequent pwsh processes pick it up.
Enable-AzContextAutosave -Scope CurrentUser | Out-Null

# Register the Topaz cloud environment.
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

# Authenticate using the username/password ROPC flow — the same grant that
# `az login --username` uses. Unlike -ServicePrincipal, this does not trigger the
# MSAL "combined flat storage" restriction that affects ClientSecretCredential.
# The full UPN (topazadmin@topaz.local.dev) is required by Topaz's UserDataPlane lookup.
$pw   = ConvertTo-SecureString "admin" -AsPlainText -Force
$cred = New-Object PSCredential("topazadmin@topaz.local.dev", $pw)

Connect-AzAccount `
    -Environment          "Topaz" `
    -Credential           $cred `
    -TenantId             $TenantId `
    -SkipContextPopulation

Write-Host "Topaz PowerShell setup complete."
