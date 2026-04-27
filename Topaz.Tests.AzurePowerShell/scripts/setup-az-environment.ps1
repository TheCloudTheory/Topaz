param(
    [string] $ResourceManagerUrl = "https://topaz.local.dev:8899",
    [string] $TenantId           = "50717675-3E5E-4A1E-8CB5-C62D8BE8CA48"
)

$ErrorActionPreference = "Stop"

# Ensure context is saved to disk so subsequent pwsh processes pick it up
Enable-AzContextAutosave -Scope CurrentUser

# Configure PSGallery and install required Az modules
Set-PSRepository -Name PSGallery -InstallationPolicy Trusted
Install-Module -Name Az.Accounts  -Force -Scope CurrentUser -AllowClobber -Repository PSGallery
Install-Module -Name Az.Resources -Force -Scope CurrentUser -AllowClobber -Repository PSGallery
Install-Module -Name Az.KeyVault  -Force -Scope CurrentUser -AllowClobber -Repository PSGallery

# Disable instance discovery so Az doesn't try to reach real AAD endpoints
$env:AZURE_CORE_INSTANCE_DISCOVERY = "false"

# Register the Topaz cloud environment
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

# Authenticate using the service-principal flow (client_credentials grant).
# Topaz accepts any credentials and maps them to the global admin identity.
$pw   = ConvertTo-SecureString "admin" -AsPlainText -Force
$cred = New-Object PSCredential("topazadmin", $pw)

Connect-AzAccount `
    -Environment     "Topaz" `
    -ServicePrincipal `
    -Credential      $cred `
    -TenantId        $TenantId

Write-Host "Topaz PowerShell setup complete."
