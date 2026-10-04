# PGLN.Auth

Reusable authentication infrastructure for ASP.NET Core applications built on .NET 10.

PGLN.Auth provides a reusable authentication foundation including:

- User registration
- Email verification
- Login
- JWT authentication
- Refresh tokens
- Password reset
- Password change
- Email change
- Step-up verification
- Trusted devices
- Account lockout
- Login throttling
- External login infrastructure
- Authentication sessions
- Outbox-based email delivery
- PostgreSQL persistence
- Configurable security policies
- Customizable email templates
- ASP.NET Core endpoint integration
- Entity Framework Core migrations

The package is designed to be consumed by applications rather than copied into individual application codebases.

---

# 1. Prerequisites

Before installing PGLN.Auth, make sure you have:

- .NET 10 SDK
- Docker Desktop
- PowerShell
- An ASP.NET Core application

Verify the .NET SDK:

```powershell
dotnet --version
```

Verify Docker:

```powershell
docker --version
```

---

# 2. Create an ASP.NET Core application

If you already have an ASP.NET Core application, skip this section.

Create a new Web API:

```powershell
dotnet new webapi -n MyAuthApp
cd MyAuthApp
```

Install PGLN.Auth:

```powershell
dotnet add package PGLN.Auth --version 0.1.0
```

Restore and build:

```powershell
dotnet restore
dotnet build
```

---

# 3. Start PostgreSQL

PGLN.Auth uses PostgreSQL through its Entity Framework Core PostgreSQL integration.

For local development, the easiest approach is Docker.

Create a PostgreSQL 17 container:

```powershell
docker run --name pgln-auth-postgres `
    -e POSTGRES_USER=postgres `
    -e POSTGRES_PASSWORD=postgres `
    -e POSTGRES_DB=pgln_auth `
    -p 5434:5432 `
    -d postgres:17
```

## Check that the container is running

```powershell
docker ps
```

You should see:

```text
pgln-auth-postgres
```

If you want to include stopped containers:

```powershell
docker ps -a
```

## Check PostgreSQL readiness

```powershell
docker exec pgln-auth-postgres pg_isready `
    -U postgres `
    -d pgln_auth
```

Expected output:

```text
/var/run/postgresql:5432 - accepting connections
```

## If the container already exists

If Docker reports that the container name is already in use:

```powershell
docker start pgln-auth-postgres
```

Then check it:

```powershell
docker ps
```

## Stop PostgreSQL

```powershell
docker stop pgln-auth-postgres
```

## Start it again later

```powershell
docker start pgln-auth-postgres
```

---

# 4. PostgreSQL connection string

For the Docker configuration above:

```text
Host=localhost;Port=5434;Database=pgln_auth;Username=postgres;Password=postgres
```

Add it to `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Auth": "Host=localhost;Port=5434;Database=pgln_auth;Username=postgres;Password=postgres"
  }
}
```

> Do not use this development password in production.

For production, provide the connection string through your deployment environment or secret-management system.

---

# 5. Generate JWT and HMAC secrets

PGLN.Auth requires cryptographically secure secrets.

Do not create production secrets using:

```text
password123
my-secret
Random()
Guid.NewGuid()
```

Use a cryptographically secure random generator instead.

Run this PowerShell script:

```powershell
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()

$bytes = New-Object byte[] 32
$rng.GetBytes($bytes)
$jwtSigningKey = [Convert]::ToBase64String($bytes)

$bytes = New-Object byte[] 48
$rng.GetBytes($bytes)
$hmacSecret = [Convert]::ToBase64String($bytes)

$rng.Dispose()

Write-Host ""
Write-Host "JWT Signing Key:" -ForegroundColor Cyan
Write-Host $jwtSigningKey

Write-Host ""
Write-Host "HMAC Secret:" -ForegroundColor Cyan
Write-Host $hmacSecret
Write-Host ""
```

Copy the generated values somewhere secure.

Do not commit them to Git.

---

# 6. Configure application secrets

The exact configuration names should match the version of PGLN.Auth being consumed.

For local development, prefer:

```text
appsettings.Development.json
```

or environment variables / user secrets.

For production, use a secret manager such as AWS Secrets Manager, Azure Key Vault, or another appropriate secret-management system.

Never commit real production secrets to source control.

---

# 7. Configure PGLN.Auth

Import the package:

```csharp
using PGLN.Auth;
```

Register PostgreSQL:

```csharp
builder.Services.AddPGLNAuthPostgreSql(
    builder.Configuration,
    connectionString);
```

The connection string should be retrieved from configuration:

```csharp
var connectionString =
    builder.Configuration.GetConnectionString("Auth")
    ?? throw new InvalidOperationException(
        "Auth connection string is missing.");
```

---

# 8. Configure application security options

PGLN.Auth allows applications to replace the default security configuration.

Example:

```csharp
builder.Services.AddPGLNAuthPostgreSql(
    builder.Configuration,
    connectionString,
    auth =>
    {
        auth.PasswordPolicy = new()
        {
            MinimumLength = 16,
            MaximumLength = 128,
            RequireUppercase = true,
            RequireLowercase = true,
            RequireDigit = true,
            RequireNonAlphanumeric = true
        };

        auth.RefreshTokens = new()
        {
            TokenLifetime =
                TimeSpan.FromDays(14)
        };

        auth.EmailVerification = new()
        {
            TokenLifetime =
                TimeSpan.FromHours(12)
        };

        auth.PasswordReset = new()
        {
            TokenLifetime =
                TimeSpan.FromMinutes(20)
        };

        auth.StepUp = new()
        {
            Lifetime =
                TimeSpan.FromMinutes(5),

            MaxFailedAttempts = 5
        };

        auth.EmailDelivery = new()
        {
            ConfirmationBaseUrl =
                "https://example.com/confirm-email",

            EmailChangeConfirmationBaseUrl =
                "https://example.com/confirm-email-change",

            ResetPasswordBaseUrl =
                "https://example.com/reset-password"
        };
    });
```

## Important

Each option group is replaced as a complete object.

The properties inside the option types are immutable after construction.

Therefore, when replacing an option group, specify the complete configuration you want the application to use.

---

# 9. Password policy

The application can enforce stronger password requirements than the defaults.

Example:

```csharp
auth.PasswordPolicy = new()
{
    MinimumLength = 16,
    MaximumLength = 128,
    RequireUppercase = true,
    RequireLowercase = true,
    RequireDigit = true,
    RequireNonAlphanumeric = true
};
```

Default password settings:

| Setting | Default |
|---|---:|
| Minimum length | 12 |
| Maximum length | 128 |
| Uppercase required | No |
| Lowercase required | No |
| Digit required | No |
| Non-alphanumeric required | No |

---

# 10. Email verification

Configure the lifetime of email verification tokens:

```csharp
auth.EmailVerification = new()
{
    TokenLifetime =
        TimeSpan.FromHours(12)
};
```

Default:

```text
24 hours
```

---

# 11. Refresh tokens

Configure refresh-token lifetime:

```csharp
auth.RefreshTokens = new()
{
    TokenLifetime =
        TimeSpan.FromDays(14)
};
```

Default:

```text
30 days
```

---

# 12. Password reset

Configure password-reset token lifetime:

```csharp
auth.PasswordReset = new()
{
    TokenLifetime =
        TimeSpan.FromMinutes(20)
};
```

Default:

```text
30 minutes
```

---

# 13. Step-up verification

Configure step-up verification:

```csharp
auth.StepUp = new()
{
    Lifetime =
        TimeSpan.FromMinutes(5),

    MaxFailedAttempts = 5
};
```

Default lifetime:

```text
10 minutes
```

Default maximum failed attempts:

```text
5
```

---

# 14. Email delivery URLs

Configure the URLs used inside authentication emails:

```csharp
auth.EmailDelivery = new()
{
    ConfirmationBaseUrl =
        "https://example.com/confirm-email",

    EmailChangeConfirmationBaseUrl =
        "https://example.com/confirm-email-change",

    ResetPasswordBaseUrl =
        "https://example.com/reset-password"
};
```

For example, the confirmation email may contain:

```text
https://example.com/confirm-email?token=<token>
```

These URLs are application-owned endpoints.

PGLN.Auth generates the authentication token and email content, while the consuming application controls where the user is sent.

---

# 15. Email delivery

PGLN.Auth uses an email abstraction rather than requiring a specific email provider.

The application registers an implementation of:

```csharp
PGLN.Auth.Application.Abstractions.Email.IEmailSender
```

The interface is:

```csharp
Task SendAsync(
    EmailMessage message,
    CancellationToken cancellationToken = default);
```

For local development, you can create a simple console/capture sender.

Create:

```text
Infrastructure/
└── Email/
    └── ConsoleEmailSender.cs
```

Add:

```csharp
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using PGLN.Auth.Application.Abstractions.Email;

namespace MyAuthApp.Infrastructure.Email;

public sealed record CapturedEmail(
    string To,
    string Subject,
    string? TextBody,
    string HtmlBody);

public sealed class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public ConsoleEmailSender(
        ILogger<ConsoleEmailSender> logger)
    {
        _logger = logger;
    }

    public static ConcurrentQueue<CapturedEmail> SentEmails { get; } = new();

    public Task SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        SentEmails.Enqueue(
            new CapturedEmail(
                message.To,
                message.Subject,
                message.TextBody,
                message.HtmlBody));

        _logger.LogInformation(
            "Development email captured for {To}: {Subject}",
            message.To,
            message.Subject);

        return Task.CompletedTask;
    }
}
```

Register it:

```csharp
builder.Services.AddPGLNAuthEmail<ConsoleEmailSender>();
```

This is intended for development/testing.

In production, replace it with an implementation that sends through your chosen email provider.

---

# 16. Custom email templates

PGLN.Auth provides a default email template renderer.

Applications can replace it with their own implementation.

The abstraction is:

```csharp
using PGLN.Auth.Application.Abstractions.Email;
```

Create a class implementing:

```csharp
IEmailTemplateRenderer
```

The renderer is responsible for the presentation of the authentication emails.

PGLN.Auth supports templates for:

- Email confirmation
- Email change confirmation
- Welcome email
- Password reset
- Password changed
- New device login
- Step-up verification code
- Account locked
- Email changed notification

Register your renderer with dependency injection:

```csharp
builder.Services.AddSingleton<
    IEmailTemplateRenderer,
    MyEmailTemplateRenderer>();
```

## Important registration order

Register the PGLN.Auth email service first:

```csharp
builder.Services.AddPGLNAuthEmail<ConsoleEmailSender>();
```

Then register the custom renderer:

```csharp
builder.Services.AddSingleton<
    IEmailTemplateRenderer,
    MyEmailTemplateRenderer>();
```

The consumer renderer must be registered after the PGLN.Auth email registration so that the application's implementation overrides the default renderer.

---

# 17. Preview captured emails during development

If using the development sender above, add:

```csharp
app.MapGet("/test/emails", () =>
    ConsoleEmailSender.SentEmails.ToArray());
```

You can also preview the latest email as HTML:

```csharp
app.MapGet("/test/email-preview", () =>
{
    var email =
        ConsoleEmailSender.SentEmails.LastOrDefault();

    if (email is null)
    {
        return Results.NotFound(
            "No captured emails available.");
    }

    return Results.Content(
        email.HtmlBody,
        "text/html; charset=utf-8");
});
```

After registering a user, open:

```text
/test/email-preview
```

in your browser.

This is a development/testing endpoint and should not be exposed publicly in production.

---

# 18. Enable the PGLN.Auth outbox worker

PGLN.Auth uses an outbox-based approach for email-related events.

Register the worker:

```csharp
builder.Services.AddPGLNAuthOutboxBackgroundWorker();
```

This allows email-related events to be persisted and processed asynchronously.

---

# 19. Apply database migrations

PGLN.Auth exposes:

```csharp
await app.Services.ApplyPGLNAuthMigrationsAsync();
```

Add it after building the application:

```csharp
var app = builder.Build();

await app.Services.ApplyPGLNAuthMigrationsAsync();
```

This allows the consuming application to initialize or upgrade the PGLN.Auth database schema without directly referencing the package's internal migration infrastructure.

For environments where database migrations are managed separately from application startup, invoke the migration operation as part of the deployment process instead.

---

# 20. Configure the authentication middleware

Add:

```csharp
app.UsePGLNAuth();

app.UseAuthentication();
app.UseAuthorization();
```

Then map the PGLN.Auth endpoints:

```csharp
app.MapPGLNAuthEndpoints();
```

---

# 21. Complete Program.cs example

A minimal consumer application can look like this:

```csharp
using PGLN.Auth;
using PGLN.Auth.Application.Abstractions.Email;
using PGLN.Auth.AspNetCore.Endpoints;
using PGLN.Auth.AspNetCore.Extensions;
using PGLN.Auth.AspNetCore.Outbox;
using MyAuthApp.Infrastructure.Email;

var builder =
    WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("Auth")
    ?? throw new InvalidOperationException(
        "Auth connection string is missing.");

builder.Services.AddPGLNAuthPostgreSql(
    builder.Configuration,
    connectionString,
    auth =>
    {
        auth.PasswordPolicy = new()
        {
            MinimumLength = 16,
            MaximumLength = 128,
            RequireUppercase = true,
            RequireLowercase = true,
            RequireDigit = true,
            RequireNonAlphanumeric = true
        };

        auth.RefreshTokens = new()
        {
            TokenLifetime =
                TimeSpan.FromDays(14)
        };

        auth.EmailVerification = new()
        {
            TokenLifetime =
                TimeSpan.FromHours(12)
        };

        auth.PasswordReset = new()
        {
            TokenLifetime =
                TimeSpan.FromMinutes(20)
        };

        auth.StepUp = new()
        {
            Lifetime =
                TimeSpan.FromMinutes(5),

            MaxFailedAttempts = 5
        };

        auth.EmailDelivery = new()
        {
            ConfirmationBaseUrl =
                "https://example.com/confirm-email",

            EmailChangeConfirmationBaseUrl =
                "https://example.com/confirm-email-change",

            ResetPasswordBaseUrl =
                "https://example.com/reset-password"
        };
    });

builder.Services.AddPGLNAuthEmail<ConsoleEmailSender>();

// Optional:
// Replace the default PGLN.Auth email templates.
builder.Services.AddSingleton<
    IEmailTemplateRenderer,
    MyEmailTemplateRenderer>();

builder.Services.AddPGLNAuthOutboxBackgroundWorker();

var app = builder.Build();

await app.Services.ApplyPGLNAuthMigrationsAsync();

app.UsePGLNAuth();

app.UseAuthentication();
app.UseAuthorization();

app.MapPGLNAuthEndpoints();

app.MapGet("/health", () =>
    Results.Ok(new
    {
        status = "healthy"
    }));

app.MapGet("/test/emails", () =>
    ConsoleEmailSender.SentEmails.ToArray());

app.MapGet("/test/email-preview", () =>
{
    var email =
        ConsoleEmailSender.SentEmails.LastOrDefault();

    if (email is null)
    {
        return Results.NotFound(
            "No captured emails available.");
    }

    return Results.Content(
        email.HtmlBody,
        "text/html; charset=utf-8");
});

app.Run();
```

---

# 22. Build the application

Run:

```powershell
dotnet build
```

Expected:

```text
Build succeeded.
```

---

# 23. Run the application

Run:

```powershell
dotnet run
```

ASP.NET Core will display the URL where the application is listening.

For example:

```text
http://localhost:5267
```

---

# 24. Check the health endpoint

From another PowerShell window:

```powershell
Invoke-RestMethod `
    -Uri "http://localhost:5267/health" `
    -Method Get
```

Expected:

```text
status
------
healthy
```

---

# 25. Register a user

Example:

```powershell
$response = Invoke-RestMethod `
    -Uri "http://localhost:5267/api/auth/register" `
    -Method Post `
    -ContentType "application/json" `
    -Body (@{
        email = "test@example.com"
        password = "ValidPassword1234!"
    } | ConvertTo-Json)

$response
```

The password must satisfy your configured password policy.

---

# 26. Inspect captured emails

If using `ConsoleEmailSender`:

```powershell
Invoke-RestMethod `
    -Uri "http://localhost:5267/test/emails" `
    -Method Get
```

Or:

```powershell
Invoke-RestMethod `
    -Uri "http://localhost:5267/test/emails" `
    -Method Get |
    ConvertTo-Json -Depth 10
```

---

# 27. Preview the email in a browser

Open:

```text
http://localhost:5267/test/email-preview
```

The latest captured HTML email will be rendered by the browser.

---

# 28. Database inspection

Check the PostgreSQL container:

```powershell
docker ps
```

Connect directly to PostgreSQL:

```powershell
docker exec -it pgln-auth-postgres psql `
    -U postgres `
    -d pgln_auth
```

List tables:

```sql
\dt
```

Exit:

```sql
\q
```

You can also execute individual queries without entering the PostgreSQL shell.

For example:

```powershell
docker exec pgln-auth-postgres psql `
    -U postgres `
    -d pgln_auth `
    -c '\dt'
```

---

# 29. Authentication database schema

PGLN.Auth maintains authentication-related persistence including tables for functionality such as:

- Users
- AuthSessions
- RefreshTokens
- EmailVerificationTokens
- EmailChangeTokens
- PasswordResetTokens
- StepUpChallenges
- TrustedDevices
- LoginAttempts
- ExternalLogins
- OutboxMessages
- EF Core migration history

The exact schema is owned by the package and should not normally be recreated manually by the consuming application.

Use:

```csharp
await app.Services.ApplyPGLNAuthMigrationsAsync();
```

to initialize or upgrade the schema.

---

# 30. Default application settings

| Setting | Default |
|---|---:|
| Minimum password length | 12 |
| Maximum password length | 128 |
| Require uppercase | No |
| Require lowercase | No |
| Require digit | No |
| Require non-alphanumeric | No |
| Email verification lifetime | 24 hours |
| Refresh-token lifetime | 30 days |
| Password-reset lifetime | 30 minutes |
| Step-up challenge lifetime | 10 minutes |
| Step-up maximum failed attempts | 5 |
| Account lockout failed attempts | 5 |
| Account lockout failure window | 15 minutes |
| Account lockout duration | 15 minutes |
| Login email throttle attempts | 10 |
| Login email throttle window | 5 minutes |

Applications can enforce stronger password requirements using `PasswordPolicy`.

---

# 31. Production considerations

The development setup in this README is intentionally simple.

Before deploying PGLN.Auth to production:

## Secrets

Do not commit:

- JWT signing keys
- HMAC secrets
- database passwords
- email provider credentials

Use a proper secret-management solution.

## PostgreSQL

Use a managed or production-grade PostgreSQL deployment.

Do not expose the database unnecessarily to the public internet.

## Email

Replace:

```csharp
ConsoleEmailSender
```

with a real email provider integration.

## HTTPS

Authentication endpoints should be served over HTTPS.

## Database migrations

Consider running migrations as part of the deployment process rather than automatically during every application startup.

## Development endpoints

Remove or protect:

```text
/test/emails
/test/email-preview
```

before production deployment.

---

# 32. Troubleshooting

## Docker container does not exist

Create it:

```powershell
docker run --name pgln-auth-postgres `
    -e POSTGRES_USER=postgres `
    -e POSTGRES_PASSWORD=postgres `
    -e POSTGRES_DB=pgln_auth `
    -p 5434:5432 `
    -d postgres:17
```

## Docker container exists but is stopped

```powershell
docker start pgln-auth-postgres
```

## Check PostgreSQL

```powershell
docker exec pgln-auth-postgres pg_isready `
    -U postgres `
    -d pgln_auth
```

## Database connection fails

Verify:

```text
Host=localhost
Port=5434
Database=pgln_auth
Username=postgres
Password=postgres
```

and confirm:

```powershell
docker ps
```

shows the container running.

## Migrations fail

Check that PostgreSQL is accepting connections:

```powershell
docker exec pgln-auth-postgres pg_isready `
    -U postgres `
    -d pgln_auth
```

Then check the application logs.

## Email is not captured

Verify that:

```csharp
builder.Services.AddPGLNAuthEmail<ConsoleEmailSender>();
```

is registered.

Also verify:

```csharp
builder.Services.AddPGLNAuthOutboxBackgroundWorker();
```

is registered.

## Custom email template is not being used

Make sure:

```csharp
builder.Services.AddPGLNAuthEmail<ConsoleEmailSender>();

builder.Services.AddSingleton<
    IEmailTemplateRenderer,
    MyEmailTemplateRenderer>();
```

The custom renderer must be registered **after** the PGLN.Auth email registration.

---

# 33. Recommended development setup

For a simple local consumer application, the recommended structure is:

```text
MyAuthApp/
│
├── Infrastructure/
│   └── Email/
│       ├── ConsoleEmailSender.cs
│       └── MyEmailTemplateRenderer.cs
│
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
└── MyAuthApp.csproj
```

The consuming application owns:

- Branding
- Email templates
- Email provider implementation
- Application URLs
- Application-specific password policy
- Environment-specific configuration
- Deployment configuration

PGLN.Auth owns the reusable authentication infrastructure.

---

# 34. Architecture

The consuming application should not need to copy PGLN.Auth's authentication implementation into its own project.

Conceptually:

```text
┌─────────────────────────────┐
│       Consumer App          │
│                             │
│  API / UI / Application     │
│  Branding                   │
│  Email Provider             │
│  Email Templates            │
│  Application Configuration  │
└──────────────┬──────────────┘
               │
               ▼
┌─────────────────────────────┐
│          PGLN.Auth          │
│                             │
│ Registration                │
│ Login                       │
│ JWT                         │
│ Refresh Tokens              │
│ Email Verification          │
│ Password Reset              │
│ Step-Up                     │
│ Lockout / Throttling        │
│ Sessions                    │
│ Trusted Devices             │
│ Outbox                      │
│ Persistence                 │
└──────────────┬──────────────┘
               │
               ▼
┌─────────────────────────────┐
│         PostgreSQL          │
└─────────────────────────────┘
```

This allows PGLN.Auth to be reused across multiple .NET applications while each application retains control over its own presentation, configuration, infrastructure integrations, and deployment environment.

---

# 35. Summary

The basic consumer workflow is:

```text
1. Create ASP.NET Core application
2. Install PGLN.Auth
3. Start PostgreSQL
4. Configure connection string
5. Generate secure JWT/HMAC secrets
6. Configure PGLN.Auth
7. Configure application security options
8. Register email sender
9. Optionally replace email templates
10. Register outbox worker
11. Apply migrations
12. Configure middleware
13. Map authentication endpoints
14. Build
15. Run
16. Register a test user
17. Inspect the generated email
```

Once these steps are complete, the application has a working PGLN.Auth authentication foundation without having to copy the authentication implementation into the application itself.
