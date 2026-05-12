#!/usr/bin/env bash
# start-dns.sh — starts dnsmasq on every container start (postStartCommand).
# Safe to run multiple times: kills any existing dnsmasq first.

set -euo pipefail

# Ensure config exists (in case postCreateCommand hasn't run yet on first start)
sudo mkdir -p /etc/dnsmasq.d
if [ ! -f /etc/dnsmasq.d/topaz.conf ]; then
    echo "address=/.topaz.local.dev/172.28.0.10" | sudo tee /etc/dnsmasq.d/topaz.conf > /dev/null
fi

# Prepend 127.0.0.1 to resolv.conf so dnsmasq is queried first
if ! grep -q "^nameserver 127.0.0.1" /etc/resolv.conf; then
    EXISTING=$(cat /etc/resolv.conf)
    printf "nameserver 127.0.0.1\n%s\n" "$EXISTING" | sudo tee /etc/resolv.conf > /dev/null
fi

# Kill any existing instance on port 53 and restart
sudo fuser -k 53/udp 53/tcp > /dev/null 2>&1 || true
sleep 0.3
sudo dnsmasq --conf-dir=/etc/dnsmasq.d --no-daemon --log-facility=/tmp/dnsmasq.log &
disown
