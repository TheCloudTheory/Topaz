#!/bin/bash

# Registers the required DNS entries in /etc/hosts for local development tests.
# Usage: ./register-dns-entries-for-tests.sh

cd "$(dirname "${BASH_SOURCE[0]}")"

DNS_ENTRIES_STORAGE=("blobstoragetests" "azurestoragetests" "tablestoragetests")
DNS_ENTRIES_KEYVAULT=("deletedvault456" "purgevault123")

for entry in "${DNS_ENTRIES_STORAGE[@]}"; do
    ./register-dns-storage-entry.sh "$entry"
done

for entry in "${DNS_ENTRIES_KEYVAULT[@]}"; do
    ./register-dns-keyvault-entry.sh "$entry"
done