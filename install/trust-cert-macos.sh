#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
CRT="$SCRIPT_DIR/../certificate/topaz.crt"

if [ ! -f "$CRT" ]; then
  echo "Certificate not found at $CRT"
  echo "Run certificate/generate.sh first."
  exit 1
fi

echo "Trusting Topaz certificate in macOS Keychain (sudo required)..."
sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain "$CRT"

echo "Done. The Topaz certificate is now trusted on this machine."
