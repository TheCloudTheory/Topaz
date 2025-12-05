#!/bin/bash

# Registers a single Key Vault DNS entry in /etc/hosts for local development.
# Usage: ./register-dns-keyvault-entry.sh <hostname>

if [ "$#" -ne 1 ]; then
    echo "Usage: $0 <hostname>"
    exit 1
fi

HOSTNAME=$1
ENTRY="127.0.2.1   $HOSTNAME.topaz.keyvault.local.dev"

# Backup the current /etc/hosts file
sudo cp /etc/hosts /etc/hosts.bak
echo "Backup of /etc/hosts created at /etc/hosts.bak"

# Add DNS entries if they do not already exist
if ! grep -q "$ENTRY" /etc/hosts; then
    echo "Adding entry: $ENTRY"
    echo "$ENTRY" | sudo tee -a /etc/hosts > /dev/null
else
    echo "Entry already exists: $ENTRY"
fi