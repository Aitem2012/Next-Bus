# NextBus Dev Deployment Reference

This directory contains the deployment configuration for NextBus Dev API hosted at **`api-dev-nextbus.u-check.ng`**.

## Architecture & Infrastructure

- **Domain/Subdomain**: `api-dev-nextbus.u-check.ng`
- **Internal Loopback Port**: `http://127.0.0.1:5070`
- **Target Path**: `/var/www/nextbus-dev-api`
- **Systemd Service**: `nextbus-dev-api.service`
- **Environment File**: `/etc/nextbus-api/nextbus-dev-api.env`
- **Nginx Configuration**: `/etc/nginx/sites-available/api-dev-nextbus.conf`
- **Database**: PostgreSQL (`next-bus` on Aiven Cloud)

## Automated CI/CD (GitHub Actions)

On every push to `develop`, the workflow [`.github/workflows/next-bus.yml`](../.github/workflows/next-bus.yml) automatically:
1. Restores NuGet packages, builds the solution, and runs unit tests.
2. Publishes the release bundle.
3. Injects configuration from GitHub secrets (if provided).
4. SSHs into the VPS host (`secrets.VPS_HOST`).
5. Rsyncs the build artifacts to `/var/www/nextbus-dev-api/` (preserving `appsettings.Local.json`).
6. Ensures systemd and Nginx configurations are active.
7. Restarts `nextbus-dev-api.service` and prints the health status.

## GitHub Secrets Required

| Secret | Description |
|---|---|
| `VPS_HOST` | VPS IP address or hostname |
| `VPS_USER` | Deploy user (defaults to `deploy` if omitted) |
| `VPS_SSH_KEY` or `VPS_DEPLOY_KEY` | Private SSH key authorized for the VPS |
| `NEXTBUS_DB_CONNECTION_STRING` or `DB_CONNECTION_STRING` | Aiven PostgreSQL connection string |
| `PAYSTACK_SECRET_KEY` | Paystack secret key |
| `PAYSTACK_PUBLIC_KEY` | Paystack public key |

## SSL / Let's Encrypt Setup on VPS

To enable HTTPS with Let's Encrypt:
```bash
# 1. Ensure the Nginx virtual host is enabled
sudo ln -sf /etc/nginx/sites-available/api-dev-nextbus.conf /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx

# 2. Install certificate (or request new one)
sudo certbot --nginx -d api-dev-nextbus.u-check.ng
# If certificate was already downloaded:
sudo certbot install --cert-name api-dev-nextbus.u-check.ng
```
