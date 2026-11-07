#!/usr/bin/env bash
set -e

echo "=== NextBus Dev API - Initial VPS Setup ==="

# 1. Ensure required directories exist
echo "Creating application directories..."
sudo mkdir -p /var/www/nextbus-dev-api /etc/nextbus-api

# 2. Setup environment configuration template if missing
if [ ! -f /etc/nextbus-api/nextbus-dev-api.env ]; then
    echo "Creating /etc/nextbus-api/nextbus-dev-api.env from example..."
    sudo cp deploy/nextbus-dev-api.env.example /etc/nextbus-api/nextbus-dev-api.env
    sudo chmod 600 /etc/nextbus-api/nextbus-dev-api.env
    echo "NOTICE: Please update /etc/nextbus-api/nextbus-dev-api.env with your real credentials."
fi

# 3. Install Systemd service
echo "Installing Systemd service..."
sudo cp deploy/nextbus-dev-api.service /etc/systemd/system/nextbus-dev-api.service
sudo systemctl daemon-reload
sudo systemctl enable nextbus-dev-api

# 4. Install Nginx configuration
if [ ! -f /etc/nginx/sites-available/api-dev-nextbus.conf ] && [ ! -f /etc/nginx/sites-available/api-dev-nextbus.u-check.ng ]; then
    echo "Configuring Nginx reverse proxy for api-dev-nextbus.u-check.ng..."
    sudo cp deploy/nginx-api-dev-nextbus.conf /etc/nginx/sites-available/api-dev-nextbus.conf
    sudo ln -sf /etc/nginx/sites-available/api-dev-nextbus.conf /etc/nginx/sites-enabled/
    sudo nginx -t && sudo systemctl reload nginx
fi

# 5. Issue Let's Encrypt SSL certificate if Certbot is available
if command -v certbot &> /dev/null; then
    echo "Requesting SSL certificate with Certbot..."
    sudo certbot --nginx -d api-dev-nextbus.u-check.ng --non-interactive --agree-tos --redirect -m hello@classicalaitem.com || true
fi

echo "=== VPS Setup Complete! ==="
