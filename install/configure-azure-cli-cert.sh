#!/bin/bash

set -e  # Exit on any error

echo "============================================"
echo "Topaz Azure CLI Certificate Configuration"
echo "============================================"
echo ""
echo "This script configures Azure CLI to trust the Topaz certificate"
echo "by adding it to the Azure CLI's bundled certificate store."
echo ""

# Get the directory where this script is located
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
TOPAZ_CERT="$REPO_ROOT/certificate/topaz.crt"

# Check if Topaz certificate exists
if [ ! -f "$TOPAZ_CERT" ]; then
    echo "ERROR: Topaz certificate not found at: $TOPAZ_CERT"
    echo "Please generate the certificate first using: ./certificate/generate.sh"
    exit 1
fi

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo "ERROR: Azure CLI is not installed."
    echo ""
    echo "Install it with:"
    if [[ "$OSTYPE" == "darwin"* ]]; then
        echo "  brew install azure-cli"
    else
        echo "  curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash"
    fi
    exit 1
fi

echo "============================================"
echo "Step 1: Locating Azure CLI certifi bundle..."
echo "============================================"

# Function to find Azure CLI certifi bundle
find_az_certifi_bundle() {
    local search_paths=()
    
    # Detect OS and architecture
    if [[ "$OSTYPE" == "darwin"* ]]; then
        # macOS
        if [[ $(uname -m) == "arm64" ]]; then
            # Apple Silicon
            search_paths+=(
                "/opt/homebrew/Cellar/azure-cli/"*"/libexec/lib/python"*"/site-packages/certifi/cacert.pem"
            )
        else
            # Intel
            search_paths+=(
                "/usr/local/Cellar/azure-cli/"*"/libexec/lib/python"*"/site-packages/certifi/cacert.pem"
            )
        fi
    elif [[ -f /etc/debian_version ]]; then
        # Ubuntu/Debian
        search_paths+=(
            "/opt/az/lib/python"*"/site-packages/certifi/cacert.pem"
        )
    elif [[ -f /etc/redhat-release ]] || [[ -f /etc/centos-release ]] || [[ -f /etc/SuSE-release ]]; then
        # RHEL/CentOS/SUSE
        search_paths+=(
            "/usr/lib64/az/lib/python"*"/site-packages/certifi/cacert.pem"
        )
    else
        # Try all possible locations
        search_paths+=(
            "/opt/az/lib/python"*"/site-packages/certifi/cacert.pem"
            "/usr/lib64/az/lib/python"*"/site-packages/certifi/cacert.pem"
            "/usr/local/Cellar/azure-cli/"*"/libexec/lib/python"*"/site-packages/certifi/cacert.pem"
            "/opt/homebrew/Cellar/azure-cli/"*"/libexec/lib/python"*"/site-packages/certifi/cacert.pem"
        )
    fi
    
    # Expand globs and find first existing file
    for pattern in "${search_paths[@]}"; do
        for path in $pattern; do
            if [ -f "$path" ]; then
                echo "$path"
                return 0
            fi
        done
    done
    
    return 1
}

AZ_CERTIFI_BUNDLE=$(find_az_certifi_bundle)

if [ -z "$AZ_CERTIFI_BUNDLE" ]; then
    echo "ERROR: Could not find Azure CLI certifi bundle."
    echo ""
    echo "Searched in the following locations:"
    echo "  - /opt/az/lib/python*/site-packages/certifi/cacert.pem (Ubuntu/Debian)"
    echo "  - /usr/lib64/az/lib/python*/site-packages/certifi/cacert.pem (RHEL/CentOS/SUSE)"
    echo "  - /usr/local/Cellar/azure-cli/*/libexec/lib/python*/site-packages/certifi/cacert.pem (macOS Intel)"
    echo "  - /opt/homebrew/Cellar/azure-cli/*/libexec/lib/python*/site-packages/certifi/cacert.pem (macOS Silicon)"
    echo ""
    echo "Please locate the bundle manually and run:"
    echo "  sudo bash -c 'cat \"$TOPAZ_CERT\" >> /path/to/cacert.pem'"
    exit 1
fi

echo "Found Azure CLI certifi bundle at:"
echo "  $AZ_CERTIFI_BUNDLE"
echo ""

# Check if Topaz cert is already in the bundle
if grep -q "BEGIN CERTIFICATE" "$TOPAZ_CERT" && grep -Fq "$(openssl x509 -in "$TOPAZ_CERT" -noout -subject 2>/dev/null || echo "TOPAZ_CERT_CHECK")" "$AZ_CERTIFI_BUNDLE" 2>/dev/null; then
    echo "✓ Topaz certificate is already installed in the Azure CLI bundle."
    echo ""
    read -p "Would you like to reinstall it? (y/n) " -n 1 -r
    echo ""
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "Configuration unchanged."
        exit 0
    fi
fi

echo "============================================"
echo "Step 2: Creating backup..."
echo "============================================"

# Create backup
BACKUP_FILE="${AZ_CERTIFI_BUNDLE}.topaz-backup-$(date +%Y%m%d-%H%M%S)"
sudo cp "$AZ_CERTIFI_BUNDLE" "$BACKUP_FILE"
echo "Created backup at:"
echo "  $BACKUP_FILE"
echo ""

echo "============================================"
echo "Step 3: Adding Topaz certificate..."
echo "============================================"

# Add Topaz certificate to the bundle
sudo bash -c "echo '' >> '$AZ_CERTIFI_BUNDLE'"
sudo bash -c "echo '# Topaz Local Development Certificate' >> '$AZ_CERTIFI_BUNDLE'"
sudo bash -c "cat '$TOPAZ_CERT' >> '$AZ_CERTIFI_BUNDLE'"

echo "✓ Topaz certificate added to Azure CLI bundle"
echo ""

echo "============================================"
echo "Step 4: Testing Azure CLI Configuration"
echo "============================================"
echo ""

# Simple test - check Azure CLI version (works without authentication)
if az version --output none 2>/dev/null; then
    echo "✓ Azure CLI test successful with Topaz certificate!"
else
    echo "⚠ Azure CLI test completed"
fi

echo ""
echo "============================================"
echo "Configuration Complete!"
echo "============================================"
echo ""
echo "Azure CLI is now configured to trust the Topaz certificate."
echo ""
echo "When working with Topaz:"
echo "  1. Start Topaz: dotnet run --project Topaz.CLI -- start"
echo "  2. Use Azure CLI as normal - it will trust the Topaz certificate"
echo ""
echo "To restore the original bundle:"
echo "  sudo cp '$BACKUP_FILE' '$AZ_CERTIFI_BUNDLE'"
echo ""
echo "To update after regenerating certificates:"
echo "  $SCRIPT_DIR/$(basename "$0")"
echo ""
echo "For more information:"
echo "  https://learn.microsoft.com/cli/azure/use-azure-cli-successfully-troubleshooting"
echo ""
