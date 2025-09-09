#!/bin/bash

# This script modifies `/etc/hosts` to add DNS entries for local development.
# It requires sudo privileges to run.

# Define the DNS entries to be added
DNS_ENTRIES=(
    "127.0.2.1   topaz.local.dev"
    "127.0.2.1   topaz.storage.table.local.dev"
    "127.0.2.1   topaz.storage.blob.local.dev"
    "127.0.2.1   topaz.storage.queue.local.dev"
    "127.0.2.1   topaz.servicebus.local.dev"
    "127.0.2.1   topaz.eventhub.local.dev"
    "127.0.2.1   topaz.keyvault.local.dev"
)

# Backup the current /etc/hosts file
sudo cp /etc/hosts /etc/hosts.bak
echo "Backup of /etc/hosts created at /etc/hosts.bak"

# Add DNS entries if they do not already exist
for ENTRY in "${DNS_ENTRIES[@]}"; do
    if ! grep -q "$ENTRY" /etc/hosts; then
        echo "Adding entry: $ENTRY"
        echo "$ENTRY" | sudo tee -a /etc/hosts > /dev/null
    else
        echo "Entry already exists: $ENTRY"
    fi
done