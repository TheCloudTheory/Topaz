#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
openssl pkcs12 -inkey "$SCRIPT_DIR/topaz.key" -in "$SCRIPT_DIR/topaz.crt" -export -out "$SCRIPT_DIR/topaz.pfx" -passout pass:qwerty
