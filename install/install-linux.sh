#!/bin/bash

echo "--------------------------------------------"
echo "Starting installation of dnsmasq for .topaz.local.dev domains..."
echo "(Note that sudo privileges will be required for some steps)"
echo "--------------------------------------------"

# Check if running as root
if [ "$EUID" -ne 0 ]; then 
    echo "Please run as root or with sudo"
    exit 1
fi

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

# Configure dnsmasq to resolve .topaz.local.dev domains
echo "address=/.topaz.local.dev/127.0.0.1" > $DNSMASQ_CONF_DIR/topaz.local.dev.conf

# Enable and start dnsmasq service
systemctl enable dnsmasq
systemctl restart dnsmasq

echo "--------------------------------------------"
echo "Step 3 - Verifying dnsmasq service status..."
echo "--------------------------------------------"

systemctl status dnsmasq --no-pager

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
    
    echo "NetworkManager configured to use dnsmasq"
else
    echo "NetworkManager not detected. You may need to configure /etc/resolv.conf manually."
    echo "Add 'nameserver 127.0.0.1' as the first nameserver in /etc/resolv.conf"
fi

echo "--------------------------------------------"
echo "Testing DNS resolution..."
echo "--------------------------------------------"

# Test dnsmasq configuration
dig test.topaz.local.dev @127.0.0.1

echo "--------------------------------------------"
echo "Installation complete!"
echo "Test with: ping xxx.storage.topaz.local.dev"
echo "--------------------------------------------"
