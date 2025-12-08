#!/bin/bash

echo "--------------------------------------------"
echo "Starting installation of dnsmasq for .topaz.local.dev domains..."
echo "(Note that sudo privileges will be required for some steps)"
echo "--------------------------------------------"

#  Step 1 - Install dnsmasq on macOS
brew install dnsmasq

# Switch to sudo for remaining operations
sudo -s << 'EOF'

# Step 2 - create resolver for .topaz.local.dev domains
mkdir -p /etc/resolver
echo "nameserver 127.0.0.1" > /etc/resolver/topaz.local.dev
echo "port 53" >> /etc/resolver/topaz.local.dev

# Step 3 - Configure dnsmasq to resolve .topaz.local.dev domains
mkdir -p /opt/homebrew/etc/dnsmasq.d
echo "address=/.topaz.local.dev/127.0.0.1" > /opt/homebrew/etc/dnsmasq.d/topaz.local.dev.conf

# Step 4 - Start dnsmasq service
brew services restart dnsmasq

EOF

echo "--------------------------------------------"
echo "Testing DNS resolution..."
echo "--------------------------------------------"

# Step 5 - Test dnsmasq configuration
dig test.topaz.local.dev @127.0.0.1
scutil --dns | grep -A 5 "resolver #"

echo "--------------------------------------------"
echo "Installation complete!"
echo "Test with: ping xxx.topaz.local.dev"
echo "--------------------------------------------"