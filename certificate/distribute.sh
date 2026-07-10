#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
CRT="$SCRIPT_DIR/topaz.crt"
KEY="$SCRIPT_DIR/topaz.key"

DESTINATIONS=(
  "$SCRIPT_DIR/../.devcontainer"
  "$SCRIPT_DIR/../Topaz.Host"
  "$SCRIPT_DIR/../Topaz.Portal"
  "$SCRIPT_DIR/../Topaz.Tests.Isolated"
  "$SCRIPT_DIR/../Topaz.Tests.AzureCLI"
  "$SCRIPT_DIR/../Topaz.Tests.AzurePowerShell"
  "$SCRIPT_DIR/../Topaz.Tests.Python"
  "$SCRIPT_DIR/../Topaz.Tests.NodeJS"
  "$SCRIPT_DIR/../Topaz.Tests.Terraform"
  "$SCRIPT_DIR/../Examples/Compose"
  "$SCRIPT_DIR/../Examples/Topaz.Example.AllInOne"
  "$SCRIPT_DIR/../Examples/Topaz.Examples.MassTransit"
  "$SCRIPT_DIR/../Examples/Devcontainer/.devcontainer"
  "$SCRIPT_DIR/../../topaz-testcontainers/src/Testcontainers.Topaz"
  "$SCRIPT_DIR/../../topaz-demo"
  "$SCRIPT_DIR/../../topaz-demo/tests/Topaz.Demo.Bicep.Tests"
)

for dest in "${DESTINATIONS[@]}"; do
  if [ -d "$dest" ]; then
    cp "$CRT" "$dest/topaz.crt"
    cp "$KEY" "$dest/topaz.key"
    echo "✓ $dest"
  else
    echo "⚠ Skipped (not found): $dest"
  fi
done
