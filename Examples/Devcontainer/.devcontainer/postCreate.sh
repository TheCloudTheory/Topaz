#!/usr/bin/env bash
# postCreate.sh — run once after the devcontainer is created.
# Trusts the Topaz TLS certificate in the system store and in the Azure CLI
# certifi bundle so that `az` commands can reach Topaz over HTTPS without
# needing --insecure or custom REQUESTS_CA_BUNDLE env vars.

set -euo pipefail

CERT="$(pwd)/.devcontainer/topaz.crt"

echo "=== Topaz devcontainer setup ==="
echo ""

# ---------------------------------------------------------------------------
# 1. Trust cert system-wide (needed by curl, wget, most HTTP clients)
# ---------------------------------------------------------------------------
echo "[1/4] Trusting Topaz certificate in the system CA store..."
sudo cp "$CERT" /usr/local/share/ca-certificates/topaz.crt
sudo update-ca-certificates --fresh 2>&1 | grep -E "^(Updating|[0-9]+)" || true
echo "      Done."
echo ""

# ---------------------------------------------------------------------------
# 2. Configure wildcard DNS for *.topaz.local.dev via dnsmasq.
#    systemd is not available in devcontainers, so we run dnsmasq as a
#    background process and point /etc/resolv.conf at 127.0.0.1.
# ---------------------------------------------------------------------------
echo "[2/4] Configuring wildcard DNS for *.topaz.local.dev..."
sudo apt-get install -y -qq dnsmasq > /dev/null 2>&1

# Write dnsmasq config: resolve all *.topaz.local.dev to the Topaz sidecar IP
sudo mkdir -p /etc/dnsmasq.d
echo "address=/.topaz.local.dev/172.28.0.10" | sudo tee /etc/dnsmasq.d/topaz.conf > /dev/null

# Prepend 127.0.0.1 as the first nameserver (preserve existing upstreams)
if ! grep -q "^nameserver 127.0.0.1" /etc/resolv.conf; then
    EXISTING=$(cat /etc/resolv.conf)
    printf "nameserver 127.0.0.1\n%s\n" "$EXISTING" | sudo tee /etc/resolv.conf > /dev/null
fi

# Start dnsmasq in the background (no systemd in devcontainers)
sudo dnsmasq --conf-dir=/etc/dnsmasq.d --no-daemon --log-facility=/tmp/dnsmasq.log &
disown
echo "      Done (wildcard: *.topaz.local.dev → 172.28.0.10)"
echo ""

# ---------------------------------------------------------------------------
# 2. Inject cert into the Azure CLI certifi bundle
#    The devcontainer azure-cli feature installs az under /opt/az on Ubuntu.
# ---------------------------------------------------------------------------
echo "[3/4] Injecting Topaz certificate into the Azure CLI certifi bundle..."

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
    echo "      Azure CLI commands may fail with SSL errors."
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
# 3. Install Topaz CLI (best-effort — may not have a binary for this arch)
# ---------------------------------------------------------------------------
echo "[4/5] Installing Topaz CLI..."
TOPAZ_CLI_OK=false

# /releases/latest returns 404 for pre-releases; use /releases and pick the first.
TOPAZ_VERSION=$(curl -fsSL "https://api.github.com/repos/TheCloudTheory/Topaz/releases" \
    | grep '"tag_name"' | head -1 | sed 's/.*"tag_name": *"\([^"]*\)".*/\1/')

if [ -n "$TOPAZ_VERSION" ] && \
   curl -fsSL https://raw.githubusercontent.com/TheCloudTheory/Topaz/main/install/get-topaz.sh \
   | bash -s -- --version "$TOPAZ_VERSION" 2>&1; then
    TOPAZ_CLI_OK=true
    echo ""
    echo "      Note: the \"Next steps\" above are already handled by this devcontainer setup."
    echo "      Done."
else
    echo "      Skipped — could not install CLI (arch: $(uname -m), version: ${TOPAZ_VERSION:-unknown})."
    echo "      Use curl or az rest to interact with the Topaz host."
fi
echo ""

# ---------------------------------------------------------------------------
# 4. Set default environment variables for local Azure development
# ---------------------------------------------------------------------------
echo "[5/5] Configuring shell environment variables..."

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
echo "Topaz is running as a sidecar container (started automatically with the devcontainer)."
echo ""
echo "Verify the host is up:"
if [ "$TOPAZ_CLI_OK" = true ]; then
    echo "  topaz health"
fi
echo "  curl https://topaz.local.dev:8899/health"
echo ""
echo "List subscriptions (requires az login first):"
echo "  az rest --method get --url 'https://topaz.local.dev:8899/subscriptions?api-version=2020-01-01'"
echo ""
echo "Add resource-specific hostnames to the extra_hosts block in"
echo ".devcontainer/docker-compose.yml for Key Vault, Storage, and ACR endpoints."
