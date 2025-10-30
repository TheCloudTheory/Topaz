#!/bin/bash

# This script modifies `/etc/hosts` to add DNS entries for local development.
# It requires sudo privileges to run.

if [ "$#" -gt 1 ]; then
    echo "Usage: $0 [ip-address]"
    echo "If no IP address is provided, 127.0.2.1 will be used as default."
    exit 1
fi

# Use provided IP address or default to 127.0.2.1
IP_ADDRESS="${1:-127.0.2.1}"

echo "Using IP address: $IP_ADDRESS"

# Define the DNS entries to be added
DNS_ENTRIES=(
    "$IP_ADDRESS   topaz.local.dev"
    "$IP_ADDRESS   topaz.storage.table.local.dev"
    "$IP_ADDRESS   topaz.storage.blob.local.dev"
    "$IP_ADDRESS   topaz.storage.queue.local.dev"
    "$IP_ADDRESS   topaz.servicebus.local.dev"
    "$IP_ADDRESS   topaz.eventhub.local.dev"
    "$IP_ADDRESS   topaz.keyvault.local.dev"
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