#!/usr/bin/env bash
# postCreate.sh — run once after the devcontainer is created.
# Trusts the Topaz TLS certificate in the system store and in the Azure CLI
# certifi bundle so that `az` commands can reach Topaz over HTTPS without
# needing --insecure or custom REQUESTS_CA_BUNDLE env vars.

set -euo pipefail

CERT="/workspace/certificate/topaz.crt"

echo "=== Topaz devcontainer setup ==="
echo ""

# ---------------------------------------------------------------------------
# 1. Trust cert system-wide (needed by curl, wget, most HTTP clients)
# ---------------------------------------------------------------------------
echo "[1/3] Trusting Topaz certificate in the system CA store..."
sudo cp "$CERT" /usr/local/share/ca-certificates/topaz.crt
sudo update-ca-certificates --fresh 2>&1 | grep -E "^(Updating|[0-9]+)" || true
echo "      Done."
echo ""

# ---------------------------------------------------------------------------
# 2. Inject cert into the Azure CLI certifi bundle
#    The devcontainer azure-cli feature installs az under /opt/az on Ubuntu.
# ---------------------------------------------------------------------------
echo "[2/3] Injecting Topaz certificate into the Azure CLI certifi bundle..."

# Find the certifi cacert.pem used by the installed Azure CLI
AZ_CERTIFI_BUNDLE=""
for pattern in \
    "/opt/az/lib/python"*"/site-packages/certifi/cacert.pem" \
    "/usr/lib64/az/lib/python"*"/site-packages/certifi/cacert.pem" \
    "/opt/homebrew/Cellar/azure-cli/"*"/libexec/lib/python"*"/site-packages/certifi/cacert.pem" \
    "/usr/local/Cellar/azure-cli/"*"/libexec/lib/python"*"/site-packages/certifi/cacert.pem"
do
    for path in $pattern; do
        if [ -f "$path" ]; then
            AZ_CERTIFI_BUNDLE="$path"
            break 2
        fi
    done
done

if [ -z "$AZ_CERTIFI_BUNDLE" ]; then
    echo "      WARNING: Could not find the Azure CLI certifi bundle."
    echo "      Azure CLI commands may fail with SSL errors until you manually run:"
    echo "        sudo bash /workspace/install/configure-azure-cli-cert.sh"
else
    # Avoid duplicate entries
    CERT_SUBJECT=$(openssl x509 -in "$CERT" -noout -subject 2>/dev/null || echo "")
    if [ -n "$CERT_SUBJECT" ] && grep -qF "$CERT_SUBJECT" "$AZ_CERTIFI_BUNDLE" 2>/dev/null; then
        echo "      Topaz certificate is already present in the Azure CLI bundle — skipping."
    else
        sudo bash -c "cat \"$CERT\" >> \"$AZ_CERTIFI_BUNDLE\""
        echo "      Injected into: $AZ_CERTIFI_BUNDLE"
    fi
fi
echo ""

# ---------------------------------------------------------------------------
# 3. Set default environment variables for local Azure development
# ---------------------------------------------------------------------------
echo "[3/3] Configuring shell environment variables..."

SHELL_RC="$HOME/.bashrc"

append_if_missing() {
    local line="$1"
    grep -qxF "$line" "$SHELL_RC" 2>/dev/null || echo "$line" >> "$SHELL_RC"
}

# Topaz default tenant ID — required for Azure SDK / Azure CLI auth
append_if_missing "export AZURE_TENANT_ID=50717675-3E5E-4A1E-8CB5-C62D8BE8CA48"

# Point REQUESTS_CA_BUNDLE at the system bundle so Python-based tools (including
# the Azure CLI when run manually) also trust the Topaz cert.
append_if_missing "export REQUESTS_CA_BUNDLE=/etc/ssl/certs/ca-certificates.crt"

echo "      AZURE_TENANT_ID and REQUESTS_CA_BUNDLE written to $SHELL_RC"
echo ""

echo "=== Setup complete ==="
echo ""
echo "Topaz is running as a sidecar container."
echo "Try: curl https://topaz.local.dev:8899/subscriptions?api-version=2020-01-01"
echo ""
echo "Add resource-specific hostnames to the extra_hosts block in"
echo ".devcontainer/docker-compose.yml for Key Vault, Storage, and ACR endpoints."
