# DymoEnergy — VPS Deployment Guide (Bangladesh, no AWS)

This is the VPS-based alternative to `DEPLOYMENT.md` (which documents the AWS
setup). Same three deployables, one Linux box instead of EB + S3 + CloudFront:

| Component | Where it runs on the VPS |
|---|---|
| **Backend API** (.NET 10 ABP) | systemd service, Kestrel on `127.0.0.1:5000`, behind Nginx |
| **Admin portal** (Angular) | Static files, served by Nginx from `/var/www/admin` |
| **User site** (Angular 20) | Static files, served by Nginx from `/var/www/usersite` |
| **Database** | Keep Supabase (nothing to change) **or** self-host Postgres on the VPS |

```
Browser ──HTTPS──▶ Nginx (Let's Encrypt cert)
                     ├─ www.dymoenergy.com     → static files (user site)
                     ├─ admin.dymoenergy.com   → static files (admin portal)
                     └─ api.dymoenergy.com     → reverse proxy → Kestrel :5000 (ABP API)
                                                                    │
                                                                    ▼
                                                     Supabase Postgres (or local Postgres)
```

No CloudFront-style HTTPS-wrapper hack is needed here — Nginx terminates TLS
and talks to Kestrel directly on the same box, so `X-Forwarded-*` headers are
reliable (unlike the EB+CloudFront chain in the AWS guide).

---

## 1. Pick a VPS

You don't need AWS-scale infra for this app. One box handles all three pieces
comfortably.

**Recommended (best latency-to-price, excellent docs):**
- **DigitalOcean** or **Vultr**, **Singapore** region (~40–70 ms to Dhaka).
  Both have a "Bangladesh-friendly" BDT-equivalent card payment flow and
  huge community documentation for exactly this stack (Nginx + systemd +
  Let's Encrypt).
- Size: **2 vCPU / 4 GB RAM / 80 GB SSD** (~$18–24/mo) is enough for the API +
  two static sites. Go to 4 GB→8 GB if you also self-host Postgres.

**Alternative (in-country hosting / BDT billing):**
- Bangladesh-based VPS providers (e.g. Exon Host, UY Systems, and similar
  BDIX-connected hosts) exist and give lower latency + local invoicing.
  Trade-off: smaller support teams and less standardized tooling than
  DigitalOcean/Vultr — verify current uptime reputation and get a Ubuntu
  22.04/24.04 image before committing.

Either way, provision **Ubuntu 22.04 or 24.04 LTS**. Everything below assumes
that.

> **Note on Supabase:** Supabase's managed Postgres happens to run on AWS
> under the hood, but that's irrelevant to *your* hosting bill or lock-in —
> connecting to it from a non-AWS VPS is completely normal and requires zero
> code changes. "No AWS" here means no EC2/EB/S3/CloudFront for *your*
> infrastructure. Keep the existing Supabase connection string unless you
> specifically want to self-host Postgres too (see §7).

---

## 2. Initial server hardening

```bash
# as root, first login
adduser deploy
usermod -aG sudo deploy
rsync --archive --chown=deploy:deploy ~/.ssh /home/deploy

ufw allow OpenSSH
ufw allow 80/tcp
ufw allow 443/tcp
ufw enable

apt update && apt install -y fail2ban unattended-upgrades
systemctl enable fail2ban --now
```

Then edit `/etc/ssh/sshd_config`: `PasswordAuthentication no`,
`PermitRootLogin no` → `systemctl restart ssh`. From now on, SSH in as
`deploy` with a key.

---

## 3. Install runtimes

```bash
# .NET 10 ASP.NET Core runtime (hosting only — you build locally, not on the box)
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
sudo bash dotnet-install.sh --channel 10.0 --runtime aspnetcore --install-dir /usr/share/dotnet
sudo ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet

# Nginx
sudo apt install -y nginx

# Certbot (Let's Encrypt)
sudo apt install -y certbot python3-certbot-nginx
```

---

## 4. Backend API — systemd service

### 4.1 `appsettings.Production.json` (build locally, same as the AWS guide's §3.2, just new URLs)

```json
{
  "App": {
    "SelfUrl": "https://api.dymoenergy.com",
    "AngularUrl": "https://www.dymoenergy.com",
    "CorsOrigins": "https://www.dymoenergy.com,https://admin.dymoenergy.com",
    "RedirectAllowedUrls": "https://www.dymoenergy.com,https://admin.dymoenergy.com"
  },
  "ConnectionStrings": {
    "Default": "Host=aws-1-ap-south-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<ref>;Password=<PASSWORD>;SSL Mode=Require;Trust Server Certificate=true"
  },
  "AuthServer": {
    "Authority": "https://api.dymoenergy.com",
    "RequireHttpsMetadata": true,
    "CertificatePassPhrase": "<YOUR-PASSPHRASE>"
  }
}
```

Keep `CorsOrigins` as explicit origins now (not `*`) since you control both
Nginx server blocks — tighter than the AWS setup's wildcard.

Keep the `openiddict.pfx` production cert and the Npgsql legacy-timestamp
switch from `DEPLOYMENT.md` §3.1 — those are AWS-independent, still required.

### 4.2 Publish & upload

```powershell
# local machine
dotnet publish H:\SolarShop\DymoEnergy\src\DymoEnergy.HttpApi.Host `
    -c Release -o H:\SolarShop\DymoEnergy\publish

# copy to the server (contents of publish/, including openiddict.pfx)
scp -r H:\SolarShop\DymoEnergy\publish\* deploy@YOUR_VPS_IP:/var/www/api
```

### 4.3 systemd unit — `/etc/systemd/system/dymoenergy-api.service`

```ini
[Unit]
Description=DymoEnergy API
After=network.target

[Service]
WorkingDirectory=/var/www/api
ExecStart=/usr/bin/dotnet /var/www/api/DymoEnergy.HttpApi.Host.dll
Restart=always
RestartSec=5
KillSignal=SIGINT
SyslogIdentifier=dymoenergy-api
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000

[Install]
WantedBy=multi-user.target
```

```bash
sudo chown -R www-data:www-data /var/www/api
sudo systemctl daemon-reload
sudo systemctl enable --now dymoenergy-api
sudo journalctl -u dymoenergy-api -f   # check it started (watch for pfx / DB errors)
```

### 4.4 Seed the database (DbMigrator) — same as AWS guide §3.3

Run once, from your local machine or the VPS, pointed at the same connection
string, with `OpenIddict.Applications.*.RootUrl` set to
`https://www.dymoenergy.com` / `https://admin.dymoenergy.com`.

---

## 5. Nginx server blocks

### 5.1 API reverse proxy — `/etc/nginx/sites-available/api.dymoenergy.com`

```nginx
server {
    listen 80;
    server_name api.dymoenergy.com;

    location / {
        proxy_pass         http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header    Host $host;
        proxy_set_header    X-Real-IP $remote_addr;
        proxy_set_header    X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header    X-Forwarded-Proto $scheme;
        proxy_set_header    Upgrade $http_upgrade;
        proxy_set_header    Connection keep-alive;
    }
}
```

### 5.2 User site — `/etc/nginx/sites-available/www.dymoenergy.com`

```nginx
server {
    listen 80;
    server_name www.dymoenergy.com dymoenergy.com;
    root /var/www/usersite;
    index index.html;

    location /media/ { try_files $uri =404; }
    location / { try_files $uri $uri/ /index.html; }   # SPA routing
}
```

### 5.3 Admin portal — `/etc/nginx/sites-available/admin.dymoenergy.com`

```nginx
server {
    listen 80;
    server_name admin.dymoenergy.com;
    root /var/www/admin;
    index index.html;

    location /media/ { try_files $uri =404; }
    location / { try_files $uri $uri/ /index.html; }
}
```

```bash
sudo ln -s /etc/nginx/sites-available/api.dymoenergy.com   /etc/nginx/sites-enabled/
sudo ln -s /etc/nginx/sites-available/www.dymoenergy.com   /etc/nginx/sites-enabled/
sudo ln -s /etc/nginx/sites-available/admin.dymoenergy.com /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx
```

### 5.4 HTTPS for all three (Let's Encrypt)

```bash
sudo certbot --nginx -d www.dymoenergy.com -d dymoenergy.com
sudo certbot --nginx -d admin.dymoenergy.com
sudo certbot --nginx -d api.dymoenergy.com
```

Certbot edits each server block in place, adds the `listen 443 ssl` directives
and an HTTP→HTTPS redirect. Auto-renewal is installed as a systemd timer by
default (`systemctl list-timers | grep certbot`) — nothing else to do.

---

## 6. DNS (Namecheap, or wherever the domain lives)

| Type | Host | Value |
|---|---|---|
| A | `@` | `YOUR_VPS_IP` |
| A | `www` | `YOUR_VPS_IP` |
| A | `admin` | `YOUR_VPS_IP` |
| A | `api` | `YOUR_VPS_IP` |

Plain A records — no CNAME/ALIAS gymnastics needed like the CloudFront setup,
and (unlike the AWS guide's §7 apex-domain limitation) the bare
`dymoenergy.com` gets real HTTPS here too, since Nginx/Certbot serves it
directly instead of relying on a registrar's HTTP-only redirect.

---

## 7. Deploying the Angular apps

```powershell
# Admin
cd H:\SolarShop\DymoEnergy\angular
npm run build:prod
scp -r dist\DymoEnergy\browser\* deploy@YOUR_VPS_IP:/var/www/admin

# User site
cd H:\SolarShop\DymoEnergyUserSite
npm run build -- --configuration production
scp -r dist\DymoEnergyUserSite\browser\* deploy@YOUR_VPS_IP:/var/www/usersite
```

Update `environment.prod.ts` in both Angular projects first, pointing at
`https://api.dymoenergy.com` instead of the CloudFront URL — same file, same
idea as `DEPLOYMENT.md` §5.1/§6.1, just a different API host.

No CloudFront invalidation step — Nginx serves the new files immediately.
(If you add a `Cache-Control` header for hashed JS/CSS chunks later, do a
hard-refresh or bump `index.html`'s cache-control to `no-cache` so deploys
show up instantly.)

---

## 8. Optional: self-host Postgres instead of Supabase

Only do this if you want to fully remove the AWS-hosted Supabase dependency
too (not required — Supabase works fine from any VPS).

```bash
sudo apt install -y postgresql-16
sudo -u postgres createuser --pwprompt dymoenergy
sudo -u postgres createdb -O dymoenergy dymoenergy_prod
```

Update the connection string to `Host=localhost;Port=5432;...`. You now own
backups — set up `pg_dump` on a cron job to off-box storage (e.g. rsync to
another VPS or object storage), since a single VPS has no automatic
point-in-time recovery the way Supabase does.

---

## 9. Troubleshooting quick-reference

| Symptom | Cause | Fix |
|---|---|---|
| 502 Bad Gateway on `api.` | Kestrel not running / wrong port | `systemctl status dymoenergy-api`, check `journalctl -u dymoenergy-api` |
| Discovery doc / login shows `http://` | Missing forwarded-headers config | Ensure `app.UseForwardedHeaders()` runs before auth middleware; Nginx already sets `X-Forwarded-Proto` correctly here, unlike EB |
| SPA deep-link refresh → 404 | No `try_files` fallback | Add `try_files $uri $uri/ /index.html;` to the site's Nginx block |
| Icons render as empty boxes | `media/` not uploaded or not matched by Nginx | Confirm `/media/` files exist under the site root and the `location /media/` block is present |
| "not a key encryption certificate" | Wrong `-KeyUsage` on the PFX | Same fix as `DEPLOYMENT.md` — `DigitalSignature,KeyEncipherment` |
| "Cannot write DateTime with Kind=Local" | Npgsql behavior | Same fix — `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)` |
| Certbot fails domain validation | DNS not propagated yet | Wait for the A record to resolve (`dig api.dymoenergy.com`) before rerunning certbot |
| Old JS/CSS still served after deploy | Browser cache on `index.html` | Set `index.html` to `Cache-Control: no-cache` in Nginx; hashed chunk files can stay long-cached |

---

## 10. Security follow-ups (same as AWS guide §9)

- Rotate any secrets that were ever committed to config files (Cloudinary
  API secret, DB password, cert passphrase).
- `ufw` should only allow 22/80/443 — nothing else exposed.
- Keep `unattended-upgrades` on for OS security patches.
