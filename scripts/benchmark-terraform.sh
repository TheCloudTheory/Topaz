#!/usr/bin/env bash
# benchmark-terraform.sh
#
# Measures terraform apply + destroy time for the azurerm-combined workspace
# against REAL Azure, then prints instructions for timing the Topaz equivalent.
#
# Prerequisites:
#   - terraform installed locally  (brew install hashicorp/tap/terraform)
#   - An active 'az login' session
#   - Your identity must have Contributor on the subscription AND
#     Key Vault Crypto Officer + Key Vault Certificates Officer roles
#     (required for azurerm_key_vault_key and azurerm_key_vault_certificate).
#
# Run:
#   ./scripts/benchmark-terraform.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
TF_DIR="$REPO_ROOT/Topaz.Tests.Terraform/terraform"

CACHE_DIR="$HOME/.topaz-benchmark-cache"
WORK_DIR="$HOME/.topaz-benchmark-$(date +%s)"

# ── Check for local terraform ──────────────────────────────────────────────────

if ! command -v terraform &>/dev/null; then
  echo "Error: 'terraform' not found on PATH."
  echo "Install it with:  brew install hashicorp/tap/terraform"
  exit 1
fi

echo "terraform : $(terraform version -json | python3 -c 'import sys,json; print(json.load(sys.stdin)[\"terraform_version\"])')"

# ── Derive credentials from Azure CLI ─────────────────────────────────────────

if ! az account show &>/dev/null; then
  echo "Error: no active Azure CLI session. Run 'az login' first."
  exit 1
fi

ARM_SUBSCRIPTION_ID="${ARM_SUBSCRIPTION_ID:-$(az account show --query id -o tsv)}"
ARM_TENANT_ID="${ARM_TENANT_ID:-$(az account show --query tenantId -o tsv)}"
ARM_ACCESS_TOKEN="$(az account get-access-token --resource https://management.azure.com --query accessToken -o tsv)"

echo "Subscription: $ARM_SUBSCRIPTION_ID"
echo "Tenant      : $ARM_TENANT_ID"

# ── Prepare workspace ──────────────────────────────────────────────────────────

mkdir -p "$WORK_DIR" "$CACHE_DIR"

cp "$TF_DIR/azurerm-benchmark/main.tf"    "$WORK_DIR/"
cp "$TF_DIR/providers/azurerm-azure.tf"   "$WORK_DIR/"

# On any error: destroy whatever was provisioned before exiting.
trap 'echo ""; echo "==> Error — running terraform destroy to clean up Azure resources"; terraform -chdir="$WORK_DIR" destroy -auto-approve -input=false; rm -rf "$WORK_DIR"; exit 1' ERR

echo "Workspace : $WORK_DIR"
echo "Provider cache: $CACHE_DIR"
echo ""

export TF_PLUGIN_CACHE_DIR="$CACHE_DIR"
export ARM_SUBSCRIPTION_ID ARM_TENANT_ID ARM_ACCESS_TOKEN

# ── Init (not timed — provider download is one-time) ──────────────────────────

echo "==> terraform init (provider download, not included in timing)"
terraform -chdir="$WORK_DIR" init -input=false
echo ""

# ── Apply (timed) ──────────────────────────────────────────────────────────────

echo "==> terraform apply"
APPLY_START=$(date +%s)
terraform -chdir="$WORK_DIR" apply -auto-approve -input=false
APPLY_END=$(date +%s)
APPLY_TIME=$((APPLY_END - APPLY_START))
echo ""

# ── Destroy (timed) ───────────────────────────────────────────────────────────

echo "==> terraform destroy"
DESTROY_START=$(date +%s)
terraform -chdir="$WORK_DIR" destroy -auto-approve -input=false
DESTROY_END=$(date +%s)
DESTROY_TIME=$((DESTROY_END - DESTROY_START))
echo ""

rm -rf "$WORK_DIR"

# ── Summary ────────────────────────────────────────────────────────────────────

echo "========================================"
echo "Real Azure — azurerm-combined (57 resources)"
echo "  apply:   ${APPLY_TIME}s"
echo "  destroy: ${DESTROY_TIME}s"
echo "  total:   $((APPLY_TIME + DESTROY_TIME))s"
echo ""
echo "To benchmark Topaz, rebuild the image and time the equivalent test class:"
echo "  ./scripts/build-docker.sh arm64"
echo "  time dotnet test Topaz.Tests.Terraform/Topaz.Tests.Terraform.csproj \\"
echo "    --filter 'AzureRm' --logger 'console;verbosity=minimal'"
echo ""
echo "Note: access tokens expire after ~1 h. If the run takes longer, re-run the script."
echo "========================================"
