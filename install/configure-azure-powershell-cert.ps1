#!/usr/bin/env pwsh
# Trusts the Topaz self-signed TLS certificate in the current user's certificate store
# and in the .NET SSL trust bundle used by the Az PowerShell module.
#
# Usage (from the repo root):
#   pwsh ./install/configure-azure-powershell-cert.ps1 [-CertificatePath <path>]
#
# The default certificate path is ./certificate/topaz.crt (repo root).
# Run this script once before calling configure-azure-powershell-env.ps1.

param(
    [string] $CertificatePath = (Join-Path $PSScriptRoot ".." "certificate" "topaz.crt")
)

$ErrorActionPreference = "Stop"

$CertificatePath = Resolve-Path $CertificatePath

if (-not (Test-Path $CertificatePath)) {
    Write-Error "Certificate not found at '$CertificatePath'. Generate it first with the Topaz CLI or copy it from the running Topaz host."
    exit 1
}

Write-Host "Trusting Topaz certificate: $CertificatePath" -ForegroundColor Cyan

if ($IsWindows) {
    $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($CertificatePath)
    $store = New-Object System.Security.Cryptography.X509Certificates.X509Store(
        [System.Security.Cryptography.X509Certificates.StoreName]::Root,
        [System.Security.Cryptography.X509Certificates.StoreLocation]::CurrentUser
    )
    $store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
    $store.Add($cert)
    $store.Close()
    Write-Host "Certificate added to the CurrentUser\\Root store." -ForegroundColor Green
}
elseif ($IsMacOS) {
    $expandedPath = $CertificatePath.Path
    sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain $expandedPath
    Write-Host "Certificate trusted in the macOS System keychain." -ForegroundColor Green
}
elseif ($IsLinux) {
    $dest = "/usr/local/share/ca-certificates/topaz.crt"
    sudo cp $CertificatePath.Path $dest
    sudo update-ca-certificates
    Write-Host "Certificate installed and trusted via update-ca-certificates." -ForegroundColor Green
}
else {
    Write-Warning "Unknown platform — skipping OS trust. Add the certificate manually."
}

# Append to the .NET default SSL cert bundle so the Az module's HTTP client trusts it.
$dotnetSslCaBundle = [System.Environment]::GetEnvironmentVariable("SSL_CERT_FILE", "Machine")
if ([string]::IsNullOrWhiteSpace($dotnetSslCaBundle)) {
    $dotnetSslCaBundle = [System.Environment]::GetEnvironmentVariable("SSL_CERT_FILE", "User")
}

if (-not [string]::IsNullOrWhiteSpace($dotnetSslCaBundle) -and (Test-Path $dotnetSslCaBundle)) {
    $existing = Get-Content $dotnetSslCaBundle -Raw
    $topazCert = Get-Content $CertificatePath -Raw
    if (-not $existing.Contains($topazCert.Trim())) {
        Add-Content $dotnetSslCaBundle $topazCert
        Write-Host "Appended Topaz cert to SSL_CERT_FILE bundle: $dotnetSslCaBundle" -ForegroundColor Green
    }
    else {
        Write-Host "Topaz cert already present in SSL_CERT_FILE bundle." -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Done. Run configure-azure-powershell-env.ps1 to register the Topaz environment." -ForegroundColor Cyan
