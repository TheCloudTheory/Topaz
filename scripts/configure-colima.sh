#!/bin/bash

set -euo pipefail

# This script configured Colima so that it can recognize the local Topaz certificate and communicate with the specified host.
# It's a prerequisite for some ACR Tasks tests.

CURRENT_DIR=$(dirname "$0")

colima ssh -- sudo mkdir -p "/etc/docker/certs.d/topazacrrun01.cr.topaz.local.dev:8892"
cat "$CURRENT_DIR/../certificate/topaz.crt" |
colima ssh -- sudo tee "/etc/docker/certs.d/topazacrrun01.cr.topaz.local.dev:8892/ca.crt" >/dev/null