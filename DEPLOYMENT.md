# DymoEnergy — Full AWS Deployment Guide

This document explains, end-to-end, how the DymoEnergy platform was deployed to AWS,
including every problem encountered and how it was solved. It is written so that
someone with basic AWS knowledge can reproduce the whole setup from scratch.

---

## 1. Architecture Overview

The platform has **three deployable pieces** plus a database:

| Component | Technology | Hosted on | Public URL |
|-----------|------------|-----------|------------|
| **Backend API** | ASP.NET Core (ABP Framework, .NET 10) | Elastic Beanstalk | (internal EB URL) |
| **API HTTPS wrapper** | — | CloudFront | `https://dl0ymm8cusxta.cloudfront.net` |
| **Admin portal** | Angular (ABP) | S3 + CloudFront | `https://d32u8xq08ndhng.cloudfront.net` |
| **User site** | Angular 20 | S3 + CloudFront | `https://www.dymoenergy.com` |
| **Database** | PostgreSQL | Supabase (pooler) | — |

### Why this shape?

```
                         ┌─────────────────────────────┐
   Browser (HTTPS)  ───▶ │ Angular CloudFront (S3)      │  admin + user site
                         └─────────────┬───────────────┘
                                       │ API calls (HTTPS)
                                       ▼
                         ┌─────────────────────────────┐
                         │ API CloudFront (HTTPS wrap)  │  dl0ymm8cusxta...
                         └─────────────┬───────────────┘
                                       │ HTTP (origin)
                                       ▼
                         ┌─────────────────────────────┐
                         │ Elastic Beanstalk (.NET API) │  NGINX + Kestrel
                         └─────────────┬───────────────┘
                                       │ TCP 5432 (SSL)
                                       ▼
                         ┌─────────────────────────────┐
                         │ Supabase PostgreSQL (pooler) │
                         └─────────────────────────────┘
```

**Key reason for the API CloudFront layer:** Elastic Beanstalk gives you an
**HTTP-only** URL by default (no custom domain, no free HTTPS). Browsers block
"mixed content" — an HTTPS Angular page cannot call an HTTP API. Putting a
CloudFront distribution in front of EB provides a free HTTPS endpoint and solves
the mixed-content problem without buying a domain for the API.

---

## 2. Prerequisites

- AWS account
- Domain registered at **Namecheap** (`dymoenergy.com`)
- .NET 10 SDK, Node.js + npm
- A Supabase project (PostgreSQL) — or any reachable PostgreSQL

---

## 3. Backend (API) — Elastic Beanstalk

### 3.1 Code changes required for production

Several code-level fixes were needed before the API would run correctly behind
EB + CloudFront. All are in `src/DymoEnergy.HttpApi.Host/`.

#### a) OpenIddict production signing certificate

In non-development, ABP/OpenIddict needs a real `.pfx` certificate instead of the
auto-generated dev one. In `DymoEnergyHttpApiHostModule.cs → PreConfigureServices`:

```csharp
if (!hostingEnvironment.IsDevelopment())
{
    PreConfigure<AbpOpenIddictAspNetCoreOptions>(options =>
    {
        options.AddDevelopmentEncryptionAndSigningCertificate = false;
    });

    PreConfigure<OpenIddictServerBuilder>(serverBuilder =>
    {
        serverBuilder.AddProductionEncryptionAndSigningCertificate(
            "openiddict.pfx", configuration["AuthServer:CertificatePassPhrase"]!);
        serverBuilder.SetIssuer(new Uri(configuration["AuthServer:Authority"]!));
    });
}
```

Generate the PFX (PowerShell). **The key usage MUST include both
`KeyEncipherment` and `DigitalSignature`** — OpenIddict needs encryption AND
signing. Using the wrong usage produces *"The specified certificate is not a key
encryption certificate."*

```powershell
$securePass = ConvertTo-SecureString "<YOUR-PASSPHRASE>" -AsPlainText -Force
$cert = New-SelfSignedCertificate `
    -Subject "CN=DymoEnergy OpenIddict" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyUsage DigitalSignature,KeyEncipherment `
    -KeyAlgorithm RSA -KeyLength 2048 `
    -NotAfter (Get-Date).AddYears(10)
Export-PfxCertificate -Cert $cert `
    -FilePath "H:\SolarShop\DymoEnergy\publish\openiddict.pfx" `
    -Password $securePass
```

The passphrase must match `AuthServer:CertificatePassPhrase` in
`appsettings.Production.json`.

#### b) PostgreSQL DateTime fix (Npgsql)

Npgsql (modern versions) throws *"Cannot write DateTime with Kind=Local to
PostgreSQL"*. Restore legacy behavior at the very start of `Main()` in **both**
`DymoEnergy.HttpApi.Host/Program.cs` **and** `DymoEnergy.DbMigrator/Program.cs`:

```csharp
using System;
// ...
public async static Task<int> Main(string[] args)
{
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    // ...
}
```

#### c) Force HTTPS scheme behind CloudFront + EB  ⭐ (the critical fix)

**Symptom:** the OpenIddict discovery document
(`/.well-known/openid-configuration`) returned all endpoints as `http://`
even though the site was served over HTTPS. This broke the Angular OAuth login
and caused `/api/abp/application-configuration` to show as **"canceled"**.

**Root cause:** the chain CloudFront → EB → NGINX does not reliably forward
`X-Forwarded-Proto: https` to the .NET app. Adding a CloudFront custom origin
header alone did **not** fix it (NGINX overwrote/dropped it).

**Fix that works:** force the scheme in middleware, in
`DymoEnergyHttpApiHostModule.cs → OnApplicationInitialization`, right after
`app.UseForwardedHeaders()`:

```csharp
app.UseForwardedHeaders();

if (!env.IsDevelopment())
{
    app.Use(async (ctx, next) =>
    {
        ctx.Request.Scheme = "https";
        await next();
    });
}
```

Now OpenIddict/ABP generate every URL as `https://`.

#### d) CORS — allow all origins in production

The admin and user Angular sites live on different CloudFront domains, so the API
must accept them. In production `appsettings.Production.json` sets
`"App:CorsOrigins": "*"`, and `ConfigureCors` handles the wildcard:

```csharp
var corsOrigins = configuration["App:CorsOrigins"];
if (corsOrigins == "*")
{
    builder.SetIsOriginAllowed(_ => true)
           .WithAbpExposedHeaders()
           .AllowAnyHeader().AllowAnyMethod().AllowCredentials();
}
```

#### e) Root redirect to the login page

Visiting the API root should send the user to the login page and back to the
Angular app. In `OnApplicationInitialization` (after `app.UseRouting()`):

```csharp
var angularUrl = context.ServiceProvider
    .GetRequiredService<IConfiguration>()["App:AngularUrl"]!;
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path == "/")
    {
        ctx.Response.Redirect(
            $"/Account/Login?ReturnUrl={Uri.EscapeDataString(angularUrl)}");
        return;
    }
    await next();
});
```

### 3.2 Production configuration

`src/DymoEnergy.HttpApi.Host/appsettings.Production.json`:

```json
{
  "App": {
    "SelfUrl": "https://dl0ymm8cusxta.cloudfront.net",
    "AngularUrl": "https://d32u8xq08ndhng.cloudfront.net",
    "CorsOrigins": "*",
    "RedirectAllowedUrls": "https://d32u8xq08ndhng.cloudfront.net,https://dl0ymm8cusxta.cloudfront.net,https://www.dymoenergy.com,http://www.dymoenergy.com"
  },
  "ConnectionStrings": {
    "Default": "Host=aws-1-ap-south-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<ref>;Password=<PASSWORD>;SSL Mode=Require;Trust Server Certificate=true"
  },
  "AuthServer": {
    "Authority": "https://dl0ymm8cusxta.cloudfront.net",
    "RequireHttpsMetadata": false,
    "CertificatePassPhrase": "<YOUR-PASSPHRASE>"
  }
}
```

> **Supabase note:** use the **Session pooler** connection (host
> `aws-1-...pooler.supabase.com`, port **5432**). `SSL Mode=Require` +
> `Trust Server Certificate=true` are required.

### 3.3 Seed the database (DbMigrator)

The DbMigrator seeds the OpenIddict app registrations and the admin user. Without
running it you get *"AbpValidationException: ModelState is not valid!"* at login.

- Put the **same** production connection string in
  `src/DymoEnergy.DbMigrator/appsettings.json`.
- Set the OpenIddict `RootUrl`s to the CloudFront URLs:

```json
"OpenIddict": {
  "Applications": {
    "DymoEnergy_App":     { "ClientId": "DymoEnergy_App",     "RootUrl": "https://d32u8xq08ndhng.cloudfront.net" },
    "DymoEnergy_Swagger": { "ClientId": "DymoEnergy_Swagger", "RootUrl": "https://dl0ymm8cusxta.cloudfront.net/" }
  }
}
```

- Run it **from inside the DbMigrator project directory** (so it picks up its own
  `appsettings.json`):

```powershell
cd H:\SolarShop\DymoEnergy\src\DymoEnergy.DbMigrator
dotnet run
```

### 3.4 Publish & package

```powershell
# 1. Generate the PFX into the publish folder (see 3.1a)
# 2. Publish
dotnet publish H:\SolarShop\DymoEnergy\src\DymoEnergy.HttpApi.Host `
    -c Release -o H:\SolarShop\DymoEnergy\publish

# 3. Zip the CONTENTS of publish/ (must include openiddict.pfx)
Compress-Archive -Path "H:\SolarShop\DymoEnergy\publish\*" `
    -DestinationPath "H:\SolarShop\DymoEnergy\dymo-api-deploy.zip" -Force
```

### 3.5 Deploy to Elastic Beanstalk

1. AWS Console → **Elastic Beanstalk** → create/select the .NET environment
   (Amazon Linux 2023, .NET 10).
2. **Upload and deploy** → choose `dymo-api-deploy.zip`.
3. Wait for **Health: Green**.

**Gotcha:** if health goes **Red** with *"openiddict.pfx not found"*, the cert
was not inside the zip — regenerate it into `publish/` and re-zip.

---

## 4. API CloudFront (HTTPS wrapper for EB)

1. CloudFront → **Create distribution**.
2. **Origin domain:** the EB environment URL (e.g.
   `dymo-api-prod.eba-xxxx.ap-south-1.elasticbeanstalk.com`).
3. **Protocol:** **HTTP only** (EB has no HTTPS).
4. **Viewer protocol policy:** Redirect HTTP to HTTPS.
5. **Allowed methods:** GET, HEAD, OPTIONS, PUT, POST, PATCH, DELETE (the API
   needs writes).
6. **Cache policy:** `CachingDisabled`.
7. **Origin request policy:** `AllViewer`
   (forwards **all** headers — crucial so the `Authorization` header reaches the
   API; otherwise you get *"Error detail not sent by server"*).

This distribution's domain became `https://dl0ymm8cusxta.cloudfront.net`.

> Combined with the "force HTTPS scheme" middleware (3.1c), this makes login and
> all authenticated API calls work over HTTPS.

---

## 5. Admin Angular app → S3 + CloudFront

### 5.1 Configure the environment

`angular/src/environments/environment.prod.ts`:

```typescript
const baseUrl = 'https://d32u8xq08ndhng.cloudfront.net';
const oAuthConfig = {
  issuer: 'https://dl0ymm8cusxta.cloudfront.net/',
  redirectUri: baseUrl,
  clientId: 'DymoEnergy_App',
  responseType: 'code',
  scope: 'offline_access DymoEnergy',
  requireHttps: false,
};
export const environment = {
  production: true,
  application: { baseUrl, name: 'DymoEnergy' },
  oAuthConfig,
  apis: {
    default:          { url: 'https://dl0ymm8cusxta.cloudfront.net', rootNamespace: 'DymoEnergy' },
    AbpAccountPublic: { url: oAuthConfig.issuer, rootNamespace: 'AbpAccountPublic' },
  },
  remoteEnv: { url: '/getEnvConfig', mergeStrategy: 'deepmerge' }
} as Environment;
```

### 5.2 Build

```powershell
cd H:\SolarShop\DymoEnergy\angular
npm install
npm run build   # output: dist/DymoEnergy/browser/
```

### 5.3 Upload to S3  ⚠️ (two upload gotchas)

1. Create an S3 bucket, block public access (CloudFront reads it privately).
2. **Upload the *contents* of the `browser/` folder, NOT the folder itself.**
   `index.html` must land at the **bucket root**. If you upload the folder you
   get keys like `browser/index.html` → CloudFront returns
   *404 NoSuchKey: index.html*.
3. **Upload the `media/` sub-folder too.** It contains the icon fonts
   (`fa-solid-900-*.woff2`, `bootstrap-icons-*`, etc.). If it's missing, every
   `/media/*` request is caught by the SPA 404→index.html rule and returns HTML
   instead of a font → **all icons render as empty boxes**. Keep it as a real
   `media/` folder at the bucket root.

### 5.4 CloudFront for the admin site

1. Origin: the S3 bucket, **Origin access control (OAC)** enabled → apply the
   generated bucket policy.
2. **S3 origin protocol: HTTP only** (using the website-endpoint over HTTPS
   causes *504 Gateway Timeout*).
3. **Default root object:** `index.html`.
4. **Error pages** (required for Angular client-side routing):
   - 403 → `/index.html` → response 200
   - 404 → `/index.html` → response 200

Admin site became `https://d32u8xq08ndhng.cloudfront.net`.

---

## 6. User Site → S3 + CloudFront + custom domain (`www.dymoenergy.com`)

This is the piece that uses the real domain from Namecheap.

### 6.1 Add a production environment

This project (`DymoEnergyUserSite`, Angular 20) shipped **without** an
`environment.prod.ts` and without a `fileReplacements` rule, so a prod build
still pointed at `localhost`. Fixed:

`src/environments/environment.prod.ts`:
```typescript
export const environment = {
  production: true,
  apiUrl: 'https://dl0ymm8cusxta.cloudfront.net'
};
```

`angular.json` → `configurations.production`:
```json
"fileReplacements": [
  { "replace": "src/environments/environment.ts",
    "with": "src/environments/environment.prod.ts" }
]
```

> The prod build also failed on a CSS **budget** error (`home.component.scss`
> exceeded 8 kB). Raised `anyComponentStyle` budget to
> `maximumWarning: 8kB, maximumError: 20kB`.

Build:
```powershell
cd H:\SolarShop\DymoEnergyUserSite
npm run build -- --configuration production   # output: dist/DymoEnergyUserSite/browser/
```

### 6.2 Request an SSL certificate (ACM) — MUST be in `us-east-1`

CloudFront only accepts certificates from the **N. Virginia (us-east-1)** region.

1. ACM (region **us-east-1**) → **Request certificate** → **Public**.
2. Domains: `www.dymoenergy.com` **and** `dymoenergy.com`.
3. Validation: **DNS**.
4. ACM shows one **CNAME** validation record per domain.

#### Adding the validation CNAMEs in Namecheap

Namecheap auto-appends your domain to the Host field, so **strip the domain part**:

| ACM CNAME name | Namecheap **Host** | Namecheap **Value** |
|---|---|---|
| `_5de1...a5.www.dymoenergy.com.` | `_5de1...a5.www` | `_3cb9...acm-validations.aws` |
| `_3312...9ee.dymoenergy.com.` | `_3312...9ee` | `_df7c...acm-validations.aws` |

Wait until both domains show **Issued / Success** in ACM.

### 6.3 Create the S3 bucket and upload

1. S3 → **Create bucket** (e.g. `www.dymoenergy.com`), **block all public
   access** (CloudFront reads via OAC).
2. Upload the **contents** of `dist/DymoEnergyUserSite/browser/` to the bucket
   root (`index.html`, JS chunks, CSS, and the `media/` folder).

### 6.4 Create the CloudFront distribution

Using the current wizard:

1. **Get started** → Distribution name `dymoenergy-usersite`, type **Single
   website or app**.
2. **Specify origin** → pick the S3 bucket. Keep **"Allow private S3 bucket
   access to CloudFront"** checked — the wizard creates the OAC and updates the
   bucket policy automatically.
3. Create the distribution (domain e.g. `dgozccjusabmg.cloudfront.net`).
4. **Add the custom domain + certificate:** distribution → **Add domain /
   Route domains to CloudFront** → add `www.dymoenergy.com` (and
   `dymoenergy.com`) → select the ACM certificate (auto-matches both).
5. **General → Edit → Default root object:** `index.html`.
6. **Error pages** → add:
   - 403 → `/index.html` → 200
   - 404 → `/index.html` → 200

### 6.5 Point Namecheap DNS at CloudFront

The AWS "Set up DNS routing" popup suggests **A/AAAA alias** records — those are
**Route 53 only**. On Namecheap use a **CNAME** instead.

**Problem hit:** Namecheap already had a default `www` CNAME → `parkingpage.
namecheap.com`, so adding a second `www` record failed with *"One or more errors
occurred."*

**Fix — edit the existing record in place** (don't add a duplicate):

| Type | Host | Value |
|------|------|-------|
| CNAME Record | `www` | `dgozccjusabmg.cloudfront.net`  *(replaces parkingpage)* |
| URL Redirect Record | `@` | `https://www.dymoenergy.com/`  *(apex → www)* |

Leave the Zoho `TXT` records and the two ACM validation `CNAME`s untouched.

Click **Save All Changes** and wait for DNS propagation (minutes–hours).

### 6.6 Verify

```powershell
Invoke-WebRequest https://www.dymoenergy.com          -UseBasicParsing   # 200
Invoke-WebRequest https://www.dymoenergy.com/shop     -UseBasicParsing   # 200 (SPA routing OK)
```

---

## 7. Known limitation — apex domain over HTTPS

`https://dymoenergy.com` (no `www`) **times out**, while `http://dymoenergy.com`
works. Reason: Namecheap's free **URL Redirect** only listens on port 80 (HTTP),
not 443 (HTTPS). Browsers that try HTTPS-first on the bare domain fail before the
redirect runs.

**Options to fix (optional):**
- Move DNS to **Cloudflare** (free) and use a CNAME/flattened-ANAME at the apex
  pointing to the CloudFront domain — gives real HTTPS on `dymoenergy.com`.
- Or move DNS to **Route 53** and use an **A/AAAA Alias** to CloudFront.

Most traffic reaches `www.dymoenergy.com` (which is fully HTTPS), so this is a
minor edge case.

---

## 8. Troubleshooting quick-reference

| Symptom | Cause | Fix |
|---|---|---|
| EB Health **Red**, "openiddict.pfx not found" | Cert not in zip | Regenerate PFX into `publish/`, re-zip |
| "not a key encryption certificate" | Wrong `-KeyUsage` | Use `DigitalSignature,KeyEncipherment` |
| "Cannot write DateTime with Kind=Local" | Npgsql behavior | `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)` |
| Login "ModelState is not valid" | DB not seeded | Run DbMigrator |
| Supabase connection timeout | Wrong host/port/password | Use session pooler `:5432`, correct password |
| Discovery doc shows `http://` endpoints | EB/NGINX drops scheme | Force `ctx.Request.Scheme="https"` middleware |
| "Error detail not sent by server" | API CloudFront strips headers | Cache `CachingDisabled` + `AllViewer` |
| 404 NoSuchKey: index.html | Uploaded folder, not contents | Upload contents of `browser/` |
| 504 Gateway Timeout (Angular CF) | S3 origin set to HTTPS | Set S3 origin protocol to **HTTP only** |
| All icons are empty boxes | `media/` folder not uploaded | Upload `media/` to bucket root |
| SPA deep-link refresh 404/403 | No SPA error pages | 403 & 404 → `/index.html` → 200 |
| Namecheap "One or more errors occurred" | Duplicate `www` record | Edit existing `www` CNAME instead of adding |

---

## 9. Security follow-ups

- **Regenerate any secrets** that appeared in config files (Cloudinary API secret,
  DB password, cert passphrase) before this is treated as production-hardened.
- Consider AWS Secrets Manager / EB environment properties instead of committing
  secrets in `appsettings.Production.json`.
