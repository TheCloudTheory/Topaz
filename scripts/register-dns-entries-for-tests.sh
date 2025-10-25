#!/bin/bash

# Registers the required DNS entries in /etc/hosts for local development tests.
# Usage: ./register-dns-entries-for-tests.sh

cd "$(dirname "${BASH_SOURCE[0]}")"

DNS_ENTRIES_STORAGE=("blobstoragetests" "azurestoragetests" "tablestoragetests")

for entry in "${DNS_ENTRIES_STORAGE[@]}"; do
    ./register-dns-storage-entry.sh "$entry"
done