#!/bin/bash

# Registers a single storage DNS entry in /etc/hosts for local development.
# Usage: ./register-dns-entry.sh <hostname>

if [ "$#" -ne 1 ]; then
    echo "Usage: $0 <hostname>"
    exit 1
fi

HOSTNAME=$1
ENTRY_TABLE="127.0.2.1   $HOSTNAME.topaz.storage.table.local.dev"
ENTRY_BLOB="127.0.2.1   $HOSTNAME.topaz.storage.blob.local.dev"
ENTRY_QUEUE="127.0.2.1   $HOSTNAME.topaz.storage.queue.local.dev"
DNS_ENTRIES=("$ENTRY_TABLE" "$ENTRY_BLOB" "$ENTRY_QUEUE")

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