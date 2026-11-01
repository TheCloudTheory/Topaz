#!/bin/bash

set -e  # Exit on any error

echo "--------------------------------------------"
echo "Starting installation of dnsmasq for .topaz.local.dev domains..."
echo "(Note that sudo privileges will be required for some steps)"
echo "--------------------------------------------"

# Check if running as root
if [ "$EUID" -ne 0 ]; then 
    echo "Please run as root or with sudo"
    exit 1
fi

echo "--------------------------------------------"
echo "Configuring dnsmasq to listen on 127.0.0.1..."
echo "--------------------------------------------"

# Detect package manager
if command -v apt-get &> /dev/null; then
    PKG_MANAGER="apt-get"
    DNSMASQ_CONF_DIR="/etc/dnsmasq.d"
elif command -v yum &> /dev/null; then
    PKG_MANAGER="yum"
    DNSMASQ_CONF_DIR="/etc/dnsmasq.d"
elif command -v dnf &> /dev/null; then
    PKG_MANAGER="dnf"
    DNSMASQ_CONF_DIR="/etc/dnsmasq.d"
else
    echo "Unsupported package manager. Please install dnsmasq manually."
    exit 1
fi

echo "--------------------------------------------"
echo "Step 1 - Installing dnsmasq..."
echo "--------------------------------------------"

# Install dnsmasq
$PKG_MANAGER update -y
$PKG_MANAGER install -y dnsmasq

echo "--------------------------------------------"
echo "Step 2 - Configuring dnsmasq for .topaz.local.dev domains..."
echo "--------------------------------------------"

# Create dnsmasq configuration directory if it doesn't exist
mkdir -p $DNSMASQ_CONF_DIR

# Configure dnsmasq to resolve .topaz.local.dev domains and subdomains
echo "listen-address=127.0.0.1" > $DNSMASQ_CONF_DIR/topaz.local.dev.conf
echo "bind-interfaces" >> $DNSMASQ_CONF_DIR/topaz.local.dev.conf
echo "address=/.topaz.local.dev/127.0.0.1" >> $DNSMASQ_CONF_DIR/topaz.local.dev.conf
echo "address=/.keyvault.topaz.local.dev/127.0.0.1" >> $DNSMASQ_CONF_DIR/topaz.local.dev.conf
echo "address=/.vault.topaz.local.dev/127.0.0.1" >> $DNSMASQ_CONF_DIR/topaz.local.dev.conf
echo "address=/.storage.topaz.local.dev/127.0.0.1" >> $DNSMASQ_CONF_DIR/topaz.local.dev.conf
echo "address=/.servicebus.topaz.local.dev/127.0.0.1" >> $DNSMASQ_CONF_DIR/topaz.local.dev.conf
echo "address=/.eventhub.topaz.local.dev/127.0.0.1" >> $DNSMASQ_CONF_DIR/topaz.local.dev.conf
echo "address=/.ods.opinsights.topaz.local.dev/127.0.0.1" >> $DNSMASQ_CONF_DIR/topaz.local.dev.conf
echo "address=/.applicationinsights.topaz.local.dev/127.0.0.1" >> $DNSMASQ_CONF_DIR/topaz.local.dev.conf

echo "--------------------------------------------"
echo "Step 3 - Removing stale manual /etc/hosts entries for topaz.local.dev..."
echo "(These may have been added before dnsmasq was available; dnsmasq supersedes them)"
echo "--------------------------------------------"

# Remove any manually-added topaz.local.dev lines from /etc/hosts so they don't
# shadow the dnsmasq-provided 127.0.0.1 addresses with stale values (e.g. 127.0.2.1).
if grep -q "topaz\.local\.dev" /etc/hosts; then
    cp /etc/hosts /etc/hosts.topaz-backup
    sed -i '/topaz\.local\.dev/d' /etc/hosts
    echo "Removed topaz.local.dev entries from /etc/hosts (backup at /etc/hosts.topaz-backup)"
else
    echo "No topaz.local.dev entries found in /etc/hosts — nothing to remove"
fi

# Enable and start dnsmasq service
systemctl enable dnsmasq
if ! systemctl restart dnsmasq; then
    echo "ERROR: dnsmasq failed to start. Service diagnostics:"
    systemctl status dnsmasq --no-pager || echo "Unable to retrieve dnsmasq service status."
    journalctl -u dnsmasq -n 50 --no-pager || echo "Unable to retrieve dnsmasq journal."
    exit 1
fi

echo "--------------------------------------------"
echo "Step 3 - Verifying dnsmasq service status..."
echo "--------------------------------------------"

if systemctl is-active --quiet dnsmasq; then
    echo "dnsmasq is running successfully"
    systemctl status dnsmasq --no-pager
else
    echo "ERROR: dnsmasq failed to start"
    systemctl status dnsmasq --no-pager
    exit 1
fi

echo "--------------------------------------------"
echo "Step 4 - Configuring local DNS resolution..."
echo "--------------------------------------------"

# Check if NetworkManager is running
if systemctl is-active --quiet NetworkManager; then
    echo "NetworkManager detected. Configuring DNS..."
    
    # Create or update NetworkManager dnsmasq configuration
    mkdir -p /etc/NetworkManager/conf.d
    echo "[main]" > /etc/NetworkManager/conf.d/dnsmasq.conf
    echo "dns=dnsmasq" >> /etc/NetworkManager/conf.d/dnsmasq.conf
    
    # Restart NetworkManager
    systemctl restart NetworkManager

    # NetworkManager may overwrite /etc/resolv.conf with DHCP nameservers after restart.
    # Explicitly set dnsmasq as the primary nameserver so topaz.local.dev resolves locally.
    sleep 1
    if ! grep -q "^nameserver 127.0.0.1" /etc/resolv.conf; then
        cp /etc/resolv.conf /etc/resolv.conf.nm-backup 2>/dev/null || true
        sed -i '1s/^/nameserver 127.0.0.1\n/' /etc/resolv.conf
        echo "Prepended nameserver 127.0.0.1 to /etc/resolv.conf"
    fi

    echo "NetworkManager configured to use dnsmasq"
else
    echo "NetworkManager not detected. Configuring /etc/resolv.conf..."
    
    # Replace resolver-managed symlinks so local queries go directly to dnsmasq.
    if [ -L /etc/resolv.conf ]; then
        cp -L /etc/resolv.conf /etc/resolv.conf.backup
        rm /etc/resolv.conf
        echo "Backup created at /etc/resolv.conf.backup"
    elif [ -f /etc/resolv.conf ]; then
        cp /etc/resolv.conf /etc/resolv.conf.backup
        echo "Backup created at /etc/resolv.conf.backup"
    fi
    
    # Add 127.0.0.1 as the first nameserver
    {
        echo "nameserver 127.0.0.1"
        echo "nameserver 8.8.8.8"
        echo "nameserver 8.8.4.4"
    } > /etc/resolv.conf
    
    echo "/etc/resolv.conf configured to use dnsmasq at 127.0.0.1"
fi

echo "--------------------------------------------"
echo "Testing DNS resolution..."
echo "--------------------------------------------"

# Test dnsmasq configuration
dig test.topaz.local.dev @127.0.0.1

echo "--------------------------------------------"
echo "Installation complete!"
echo "Test with: ping xxx.topaz.local.dev"
echo "--------------------------------------------"
