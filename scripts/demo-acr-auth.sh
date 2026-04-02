#!/usr/bin/env bash
# ---------------------------------------------------------------------------
# ACR authentication demo — LinkedIn showcase
#
# Prerequisites:
#   - Topaz is running locally  (dotnet run --project Topaz.CLI -- start)
#   - Azure CLI is registered against Topaz and you are logged in:
#       az cloud register -n Topaz --cloud-config cloud.json
#       az cloud set -n Topaz
#       az login --username topazadmin@topaz.local.dev --password admin
#   - DNS wildcard *.cr.topaz.local.dev resolves to 127.0.0.1
#   - Topaz self-signed certificate is trusted by curl/Docker
#
# Each scene pauses — press Enter to advance.
# ---------------------------------------------------------------------------

REGISTRY="myregistry"
RESOURCE_GROUP="demo-rg"
LOGIN_SERVER="${REGISTRY}.cr.topaz.local.dev:8892"
ACR_PORT="8892"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TOPAZ_CERT="${SCRIPT_DIR}/../certificate/topaz.crt"

# Locate the Azure CLI certifi bundle (covers macOS Homebrew Intel/Silicon + Linux).
_find_az_certifi() {
    for pattern in \
        "/opt/homebrew/Cellar/azure-cli/"*"/libexec/lib/python"*"/site-packages/certifi/cacert.pem" \
        "/usr/local/Cellar/azure-cli/"*"/libexec/lib/python"*"/site-packages/certifi/cacert.pem" \
        "/opt/az/lib/python"*"/site-packages/certifi/cacert.pem" \
        "/usr/lib64/az/lib/python"*"/site-packages/certifi/cacert.pem"; do
        for path in $pattern; do
            [ -f "$path" ] && echo "$path" && return 0
        done
    done
    return 1
}

AZ_CERTIFI_BUNDLE=$(_find_az_certifi)
if [ -z "$AZ_CERTIFI_BUNDLE" ]; then
    echo "ERROR: Could not find the Azure CLI certifi bundle. Run install/configure-azure-cli-cert.sh first."
    exit 1
fi

# Build a temporary CA bundle that includes the Topaz self-signed certificate.
TEMP_CA_BUNDLE=$(mktemp)
cat "$AZ_CERTIFI_BUNDLE" "$TOPAZ_CERT" > "$TEMP_CA_BUNDLE"
export REQUESTS_CA_BUNDLE="${TEMP_CA_BUNDLE}"

# Disable Azure CLI instance discovery so it accepts the local Topaz authority.
export AZURE_CORE_INSTANCE_DISCOVERY=false

_banner() {
    echo ""
    printf '\e[1;36m━━━  %s  ━━━\e[0m\n' "$1"
    echo ""
}

_pause() {
    echo ""
    printf '\e[2m  [ press Enter to continue ]\e[0m'
    read -r _
}

_run() {
    printf '\e[1;33m$ %s\e[0m\n' "$*"
    eval "$@"
}

# ─── Setup ──────────────────────────────────────────────────────────────────
_banner "SETUP — Register Topaz cloud and log in"

CLOUD_JSON="${SCRIPT_DIR}/../cloud.json"

az cloud show -n Topaz &>/dev/null \
    && az cloud update -n Topaz --cloud-config "@${CLOUD_JSON}" \
    || az cloud register -n Topaz --cloud-config "@${CLOUD_JSON}"
az cloud set -n Topaz
az login

# ─── Scene 1 ────────────────────────────────────────────────────────────────
_banner "SCENE 1 — Create a Container Registry in Topaz"

_run "az group create -n $RESOURCE_GROUP -l westeurope"
echo ""
_run "az acr create \
  --name $REGISTRY \
  --resource-group $RESOURCE_GROUP \
  --sku Basic \
  --admin-enabled true \
  --location westeurope"

_pause

# ─── Scene 2 ────────────────────────────────────────────────────────────────
_banner "SCENE 2 — The login server points to a local subdomain"

_run "az acr show --name $REGISTRY --resource-group $RESOURCE_GROUP \
  --query '{loginServer:loginServer, adminUserEnabled:adminUserEnabled}' \
  -o json"

echo ""
echo "  ↳ loginServer is ${LOGIN_SERVER}"
echo "  ↳ Every registry gets its own subdomain — no port juggling."

_pause

# ─── Scene 3 ────────────────────────────────────────────────────────────────
_banner "SCENE 3 — Unauthenticated probe: Docker V2 challenge"

echo "  Any Docker client starts here: GET /v2/"
echo "  No token → 401 + Www-Authenticate header directing the client to /oauth2/token."
echo ""

_run "curl -s --cacert \"${TOPAZ_CERT}\" -D - https://${LOGIN_SERVER}/v2/ | head -10"

_pause

# ─── Scene 4 ────────────────────────────────────────────────────────────────
_banner "SCENE 4 — Token exchange: AAD token → ACR refresh token"

echo "  az acr login --expose-token sends your Entra token to POST /oauth2/exchange"
echo "  and gets back an ACR refresh token — a real signed JWT."
echo ""

_run "az acr login --name $REGISTRY --expose-token -o json"

_pause

# ─── Scene 5 ────────────────────────────────────────────────────────────────
_banner "SCENE 5 — Admin credentials: docker login without Entra"

echo "  Retrieve the admin password generated at registry creation time."
echo ""

ADMIN_PASSWORD=$(_run "az acr credential show \
  --name $REGISTRY \
  --resource-group $RESOURCE_GROUP \
  --query 'passwords[0].value' -o tsv" 2>/dev/null | tail -1)

echo ""
echo "  Now authenticate directly against the Docker V2 API with Basic auth."
echo "  username = registry name, password = admin credential."
echo ""

_run "curl -s --cacert \"${TOPAZ_CERT}\" -u \"${REGISTRY}:${ADMIN_PASSWORD}\" \
  -w '\nHTTP %{http_code}\n' \
  https://${LOGIN_SERVER}/v2/"

echo ""
echo "  ↳ HTTP 200 — authenticated. Same endpoint, no token exchange needed."

_pause

# ─── Scene 6 ────────────────────────────────────────────────────────────────
_banner "SCENE 6 — Bearer token: authenticated probe"

echo "  Acquire a token via the exchange flow and replay GET /v2/ with it."
echo ""

TOKEN=$(az acr login --name $REGISTRY --expose-token --query accessToken -o tsv 2>/dev/null)

echo "  Probing /v2/ with Bearer header..."
echo ""

_run "curl -s --cacert \"${TOPAZ_CERT}\" \
  -H \"Authorization: Bearer ${TOKEN}\" \
  -w '\nHTTP %{http_code}\n' \
  https://${LOGIN_SERVER}/v2/"

echo ""
echo "  ↳ HTTP 200 — JWT validated, identity confirmed."

_pause

# ─── Cleanup ────────────────────────────────────────────────────────────────
_banner "CLEANUP"

_run "az acr delete --name $REGISTRY --resource-group $RESOURCE_GROUP --yes"
_run "az group delete -n $RESOURCE_GROUP --yes"

echo ""
unset AZURE_CORE_INSTANCE_DISCOVERY
unset REQUESTS_CA_BUNDLE
rm -f "${TEMP_CA_BUNDLE}"
printf '\e[1;32m  Done. Full ACR auth flow — locally.\e[0m\n'
echo ""
