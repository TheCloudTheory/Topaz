#!/usr/bin/env pwsh
# Registers the Topaz emulator as an Az PowerShell environment and authenticates.
#
# Usage (from anywhere):
#   pwsh ./install/configure-azure-powershell-env.ps1 \
#       [-ResourceManagerUrl <url>] [-Username <upn>] [-Password <pwd>]
#
# Prerequisites:
#   1. Az module installed: Install-Module -Name Az -Force -Scope CurrentUser -Repository PSGallery
#   2. Topaz TLS cert trusted: run configure-azure-powershell-cert.ps1 first
#   3. Topaz host is running (default port 8899)

param(
    [string] $ResourceManagerUrl = "https://topaz.local.dev:8899",
    [string] $EnvironmentName    = "Topaz",
    [string] $TenantId           = "50717675-3E5E-4A1E-8CB5-C62D8BE8CA48",
    [string] $Username           = "topazadmin@topaz.local.dev",
    [string] $Password           = "admin"
)

$ErrorActionPreference = "Stop"

if (-not (Get-Module -ListAvailable -Name Az.Accounts)) {
    Write-Error "Az module not found. Install it with: Install-Module -Name Az -Force -Scope CurrentUser -Repository PSGallery"
    exit 1
}

Import-Module Az.Accounts -MinimumVersion 2.0.0

Write-Host "Registering Az environment '$EnvironmentName'..." -ForegroundColor Cyan

$authUrl = "$ResourceManagerUrl/"

$null = Add-AzEnvironment `
    -Name                                $EnvironmentName `
    -ResourceManagerUrl                  $ResourceManagerUrl `
    -ActiveDirectoryAuthority            $authUrl `
    -ActiveDirectoryServiceEndpointResourceId $ResourceManagerUrl `
    -GraphEndpointResourceId             $ResourceManagerUrl `
    -GraphUrl                            $ResourceManagerUrl `
    -StorageEndpointSuffix               "storage.topaz.local.dev" `
    -AzureKeyVaultDnsSuffix              "vault.topaz.local.dev" `
    -AzureKeyVaultServiceEndpointResourceId $ResourceManagerUrl

Write-Host "Environment registered." -ForegroundColor Green

Write-Host "Authenticating as '$Username'..." -ForegroundColor Cyan

$securePassword = ConvertTo-SecureString $Password -AsPlainText -Force
$credential     = New-Object System.Management.Automation.PSCredential($Username, $securePassword)

Connect-AzAccount `
    -Environment $EnvironmentName `
    -Credential  $credential `
    -TenantId    $TenantId

Write-Host "Connected to Topaz successfully." -ForegroundColor Green
Write-Host "You can now run Az cmdlets against the Topaz emulator, e.g.:" -ForegroundColor Cyan
Write-Host "  Get-AzResourceGroup"
Write-Host "  New-AzKeyVault -Name myvault -ResourceGroupName myrg -Location westeurope"
