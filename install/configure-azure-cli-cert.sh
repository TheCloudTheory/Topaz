#!/bin/bash

set -e  # Exit on any error

echo "============================================"
echo "Topaz Azure CLI Certificate Configuration"
echo "============================================"
echo ""
echo "This script configures Azure CLI to trust the Topaz certificate"
echo "by creating a custom CA bundle that includes both system CAs"
echo "and the Topaz certificate."
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

# Create Topaz config directory
TOPAZ_DIR="$HOME/.topaz"
mkdir -p "$TOPAZ_DIR"

CUSTOM_BUNDLE="$TOPAZ_DIR/ca-bundle.crt"

echo "============================================"
echo "Step 1: Detecting system CA bundle..."
echo "============================================"

# Function to find system CA bundle
find_system_ca_bundle() {
    # Try common locations in order of preference
    local locations=(
        "/etc/ssl/certs/ca-certificates.crt"  # Debian/Ubuntu/Gentoo
        "/etc/pki/tls/certs/ca-bundle.crt"    # RHEL/CentOS/Fedora
        "/etc/ssl/ca-bundle.pem"               # openSUSE
        "/etc/ssl/cert.pem"                    # macOS/Alpine Linux
        "/usr/local/etc/openssl/cert.pem"     # macOS with Homebrew OpenSSL
        "/opt/homebrew/etc/openssl/cert.pem"  # macOS Apple Silicon Homebrew
        "/etc/pki/tls/cert.pem"               # Alternative RHEL location
        "/etc/ssl/certs/ca-bundle.crt"        # Alternative Fedora location
    )
    
    for location in "${locations[@]}"; do
        if [ -f "$location" ]; then
            echo "$location"
            return 0
        fi
    done
    
    # Try to find using Python certifi if available
    if command -v python3 &> /dev/null; then
        local certifi_path=$(python3 -c "import certifi; print(certifi.where())" 2>/dev/null || echo "")
        if [ -n "$certifi_path" ] && [ -f "$certifi_path" ]; then
            echo "$certifi_path"
            return 0
        fi
    fi
    
    # Try OpenSSL default
    if command -v openssl &> /dev/null; then
        local openssl_dir=$(openssl version -d 2>/dev/null | cut -d'"' -f2)
        if [ -n "$openssl_dir" ] && [ -f "$openssl_dir/cert.pem" ]; then
            echo "$openssl_dir/cert.pem"
            return 0
        fi
    fi
    
    return 1
}

SYSTEM_CA_BUNDLE=$(find_system_ca_bundle)

if [ -z "$SYSTEM_CA_BUNDLE" ]; then
    echo "ERROR: Could not find system CA bundle."
    echo ""
    echo "Please locate your system's CA bundle manually and run:"
    echo "  cat /path/to/system/ca-bundle.crt $TOPAZ_CERT > $CUSTOM_BUNDLE"
    echo "  export REQUESTS_CA_BUNDLE=\"$CUSTOM_BUNDLE\""
    exit 1
fi

echo "Found system CA bundle at: $SYSTEM_CA_BUNDLE"
echo ""

echo "============================================"
echo "Step 2: Creating custom CA bundle..."
echo "============================================"

# Create custom bundle by combining system CAs and Topaz certificate
cat "$SYSTEM_CA_BUNDLE" > "$CUSTOM_BUNDLE"
echo "" >> "$CUSTOM_BUNDLE"  # Add newline separator
echo "# Topaz Local Development Certificate" >> "$CUSTOM_BUNDLE"
cat "$TOPAZ_CERT" >> "$CUSTOM_BUNDLE"

echo "Custom CA bundle created at: $CUSTOM_BUNDLE"
echo ""

echo "============================================"
echo "Step 3: Configuration Complete!"
echo "============================================"
echo ""
echo "To use this certificate bundle with Azure CLI, you need to set"
echo "the REQUESTS_CA_BUNDLE environment variable:"
echo ""
echo "  export REQUESTS_CA_BUNDLE=\"$CUSTOM_BUNDLE\""
echo ""
echo "Add this line to your shell profile to make it permanent:"
echo ""

# Detect shell and provide appropriate instructions
if [ -n "$BASH_VERSION" ]; then
    SHELL_RC="$HOME/.bashrc"
    SHELL_NAME="bash"
elif [ -n "$ZSH_VERSION" ]; then
    SHELL_RC="$HOME/.zshrc"
    SHELL_NAME="zsh"
else
    SHELL_RC="$HOME/.profile"
    SHELL_NAME="shell"
fi

echo "  echo 'export REQUESTS_CA_BUNDLE=\"$CUSTOM_BUNDLE\"' >> $SHELL_RC"
echo "  source $SHELL_RC"
echo ""
echo "Or add it to your current session:"
echo ""
echo "  export REQUESTS_CA_BUNDLE=\"$CUSTOM_BUNDLE\""
echo ""

# Offer to add automatically
read -p "Would you like to add this to $SHELL_RC automatically? (y/n) " -n 1 -r
echo ""
if [[ $REPLY =~ ^[Yy]$ ]]; then
    # Check if already configured
    if grep -q "REQUESTS_CA_BUNDLE.*$CUSTOM_BUNDLE" "$SHELL_RC" 2>/dev/null; then
        echo "Configuration already exists in $SHELL_RC"
    else
        echo "" >> "$SHELL_RC"
        echo "# Topaz Azure CLI certificate configuration" >> "$SHELL_RC"
        echo "export REQUESTS_CA_BUNDLE=\"$CUSTOM_BUNDLE\"" >> "$SHELL_RC"
        echo "✓ Added to $SHELL_RC"
        echo ""
        echo "Run the following to apply in your current session:"
        echo "  source $SHELL_RC"
    fi
else
    echo "Skipped automatic configuration."
fi

echo ""
echo "============================================"
echo "Testing Azure CLI Configuration"
echo "============================================"
echo ""

if command -v az &> /dev/null; then
    echo "Azure CLI is installed. Testing with the new certificate bundle..."
    echo ""
    
    # Test with the new bundle
    export REQUESTS_CA_BUNDLE="$CUSTOM_BUNDLE"
    
    # Simple test - list locations (works without authentication)
    if az account list-locations --output none 2>/dev/null; then
        echo "✓ Azure CLI test successful with custom certificate bundle!"
    else
        echo "⚠ Azure CLI test completed (may require authentication)"
    fi
else
    echo "Azure CLI is not installed. Install it with:"
    echo ""
    if [[ "$OSTYPE" == "darwin"* ]]; then
        echo "  brew install azure-cli"
    else
        echo "  curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash"
    fi
fi

echo ""
echo "============================================"
echo "Additional Information"
echo "============================================"
echo ""
echo "When working with Topaz:"
echo "  1. Start Topaz: dotnet run --project Topaz.CLI -- start"
echo "  2. Ensure REQUESTS_CA_BUNDLE is set in your session"
echo "  3. Use Azure CLI as normal - it will trust the Topaz certificate"
echo ""
echo "To update the certificate bundle after regenerating certificates:"
echo "  $SCRIPT_DIR/$(basename "$0")"
echo ""
echo "For more information on Azure CLI proxy/certificate configuration:"
echo "  https://learn.microsoft.com/en-gb/cli/azure/use-azure-cli-successfully-troubleshooting"
echo ""
