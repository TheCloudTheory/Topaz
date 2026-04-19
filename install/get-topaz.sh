#!/bin/bash

set -e

REPO="TheCloudTheory/Topaz"
INSTALL_DIR_ROOT="/usr/local/bin"
INSTALL_DIR_USER="$HOME/.local/bin"
BINARIES=("topaz" "topaz-host")

# --------------------------------------------
# Helpers
# --------------------------------------------

print_banner() {
    echo "--------------------------------------------"
    echo "$1"
    echo "--------------------------------------------"
}

fail() {
    echo "ERROR: $1" >&2
    exit 1
}

need_cmd() {
    command -v "$1" &>/dev/null || fail "'$1' is required but not installed."
}

# --------------------------------------------
# Parse arguments
# --------------------------------------------

VERSION="${TOPAZ_VERSION:-}"
INSTALL_MCP=false

while [[ $# -gt 0 ]]; do
    case "$1" in
        -v|--version)
            VERSION="$2"
            shift 2
            ;;
        --with-mcp)
            INSTALL_MCP=true
            shift
            ;;
        -h|--help)
            echo "Usage: get-topaz.sh [-v|--version <tag>] [--with-mcp]"
            echo ""
            echo "  -v, --version <tag>   Install a specific release (e.g. v1.1-beta.3)."
            echo "                        Defaults to the latest GitHub release."
            echo "      --with-mcp        Also install the Topaz MCP server binary."
            echo ""
            echo "  Set TOPAZ_VERSION env var as an alternative to -v."
            exit 0
            ;;
        *)
            fail "Unknown argument: $1. Run with --help for usage."
            ;;
    esac
done

# --------------------------------------------
# Pre-flight checks
# --------------------------------------------

need_cmd curl
need_cmd chmod
need_cmd uname

print_banner "Topaz installer for Linux"

# Detect architecture
ARCH="$(uname -m)"
case "$ARCH" in
    x86_64)  RID="linux-x64"  ;;
    aarch64) RID="linux-arm64" ;;
    arm64)   RID="linux-arm64" ;;
    *)       fail "Unsupported architecture: $ARCH" ;;
esac

echo "Detected architecture: $ARCH ($RID)"

# Determine install directory
if [ "$EUID" -eq 0 ]; then
    INSTALL_DIR="$INSTALL_DIR_ROOT"
else
    INSTALL_DIR="$INSTALL_DIR_USER"
    mkdir -p "$INSTALL_DIR"
    echo "Running without root — binaries will be installed to $INSTALL_DIR"
    if [[ ":$PATH:" != *":$INSTALL_DIR:"* ]]; then
        echo ""
        echo "  WARNING: $INSTALL_DIR is not in your PATH."
        echo "  Add the following line to your shell profile (~/.bashrc, ~/.zshrc, etc.):"
        echo "    export PATH=\"\$HOME/.local/bin:\$PATH\""
        echo ""
    fi
fi

# Resolve version
if [ -z "$VERSION" ]; then
    echo "Fetching latest release from GitHub..."
    VERSION="$(curl -fsSL "https://api.github.com/repos/$REPO/releases/latest" \
        | grep '"tag_name"' \
        | head -1 \
        | sed 's/.*"tag_name": *"\([^"]*\)".*/\1/')"
    [ -n "$VERSION" ] || fail "Could not determine the latest release. Check your internet connection."
fi

echo "Installing Topaz $VERSION"

# --------------------------------------------
# Download and install binaries
# --------------------------------------------

if $INSTALL_MCP; then
    BINARIES+=("topaz-mcp")
fi

BASE_URL="https://github.com/$REPO/releases/download/$VERSION"

print_banner "Downloading binaries..."

TMP_DIR="$(mktemp -d)"
trap 'rm -rf "$TMP_DIR"' EXIT

for BIN in "${BINARIES[@]}"; do
    REMOTE_NAME="${BIN}-${RID}"
    URL="${BASE_URL}/${REMOTE_NAME}"
    DEST="${TMP_DIR}/${BIN}"

    echo "  Downloading $REMOTE_NAME..."
    HTTP_STATUS="$(curl -fsSL -w "%{http_code}" -o "$DEST" "$URL")"

    if [ "$HTTP_STATUS" != "200" ]; then
        fail "Download failed for $REMOTE_NAME (HTTP $HTTP_STATUS). Check that version '$VERSION' exists and includes a $RID build."
    fi

    chmod +x "$DEST"
done

print_banner "Installing to $INSTALL_DIR..."

for BIN in "${BINARIES[@]}"; do
    cp "${TMP_DIR}/${BIN}" "${INSTALL_DIR}/${BIN}"
    echo "  Installed $BIN"
done

# --------------------------------------------
# Verify
# --------------------------------------------

print_banner "Verifying installation..."

for BIN in "${BINARIES[@]}"; do
    FULL_PATH="${INSTALL_DIR}/${BIN}"
    if [ -x "$FULL_PATH" ]; then
        echo "  $BIN: OK ($FULL_PATH)"
    else
        fail "$BIN not found at $FULL_PATH after install."
    fi
done

# --------------------------------------------
# Done
# --------------------------------------------

print_banner "Topaz $VERSION installed successfully!"

echo "Next steps:"
echo ""
echo "  1. Set up local DNS resolution for .topaz.local.dev domains:"
echo "       sudo bash <(curl -fsSL https://raw.githubusercontent.com/$REPO/main/install/install-linux.sh)"
echo ""
echo "  2. Trust the Topaz TLS certificate for Azure CLI:"
echo "       bash <(curl -fsSL https://raw.githubusercontent.com/$REPO/main/install/configure-azure-cli-cert.sh)"
echo ""
echo "  3. Start the host, then use the CLI:"
echo "       topaz-host &"
echo "       topaz --help"
echo ""
