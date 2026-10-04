# PGLN.Auth

PGLN.Auth is a reusable authentication and identity package for ASP.NET Core applications built on .NET 10.

It provides a production-oriented authentication foundation for applications that need:

- User registration
- Email verification
- Login
- JWT access tokens
- Refresh-token rotation
- Refresh-token replay detection
- Logout
- Logout from all sessions
- Session management
- Password recovery
- Password changes
- Email address changes
- Step-up authentication
- Trusted devices
- Google external authentication
- Account lockout
- Login throttling and rate limiting
- Transactional email outbox
- PostgreSQL persistence
- Entity Framework Core migrations
- Configurable password and token policies
- Customizable transactional email templates

PGLN.Auth is intended to be installed as a reusable package. The consuming application owns application-specific configuration, branding, email delivery infrastructure, frontend URLs, and deployment configuration.

---

# Quick Start

If this is your first time using PGLN.Auth, follow the sections below in order.

The complete setup is:

```text
ASP.NET Core application
        ↓
Install PGLN.Auth
        ↓
Start PostgreSQL
        ↓
Generate JWT + HMAC secrets
        ↓
Configure appsettings
        ↓
Configure Program.cs
        ↓
Register email sender
        ↓
Register outbox worker
        ↓
Apply database migrations
        ↓
Run application
        ↓
Register a user
        ↓
Inspect captured email
```

---

# 1. Requirements

You need:

- .NET 10 SDK
- ASP.NET Core
- Docker Desktop
- PowerShell
- PostgreSQL when using `AddPGLNAuthPostgreSql`

Verify .NET:

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

Create a Web API:

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

# 3. Start PostgreSQL with Docker

For local development, PostgreSQL can be run using Docker.

The following command creates a PostgreSQL 17 container:

```powershell
docker run --name pgln-auth-postgres `
    -e POSTGRES_USER=postgres `
    -e POSTGRES_PASSWORD=postgres `
    -e POSTGRES_DB=pgln_auth `
    -p 5434:5432 `
    -d postgres:17
```

This creates:

```text
Container: pgln-auth-postgres
PostgreSQL: 17
Host: localhost
Port: 5434
Database: pgln_auth
Username: postgres
Password: postgres
```

> The credentials above are intended for local development only. Do not use them in production.

---

## 3.1 Check whether PostgreSQL is running

```powershell
docker ps
```

You should see the `pgln-auth-postgres` container.

To include stopped containers:

```powershell
docker ps -a
```

---

## 3.2 Check PostgreSQL readiness

```powershell
docker exec pgln-auth-postgres pg_isready `
    -U postgres `
    -d pgln_auth
```

Expected output:

```text
/var/run/postgresql:5432 - accepting connections
```

---

## 3.3 If the container already exists

If Docker reports:

```text
Conflict. The container name "/pgln-auth-postgres" is already in use
```

do not create another container.

Start the existing one:

```powershell
docker start pgln-auth-postgres
```

Then verify:

```powershell
docker ps
```

---

## 3.4 Stop PostgreSQL

```powershell
docker stop pgln-auth-postgres
```

Start it again later:

```powershell
docker start pgln-auth-postgres
```

---

# 4. PostgreSQL connection string

For the Docker configuration above, the connection string is:

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

For production, do not commit database credentials to source control.

Use environment variables, user secrets, AWS Secrets Manager, Azure Key Vault, or another secure configuration provider.

---

# 5. Generate JWT and HMAC secrets

PGLN.Auth requires:

1. A JWT signing key
2. A step-up authentication HMAC secret

These values must be treated as secrets.

Do not use:

```text
password123
my-secret
Guid.NewGuid()
Random()
```

for security-sensitive secrets.

Use the .NET cryptographic random-number generator instead.

Run the following PowerShell script:

```powershell
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()

# Generate 32 bytes / 256 bits for the JWT signing key.
$bytes = New-Object byte[] 32
$rng.GetBytes($bytes)
$jwtSigningKey = [Convert]::ToBase64String($bytes)

# Generate 48 bytes / 384 bits for the step-up HMAC secret.
$bytes = New-Object byte[] 48
$rng.GetBytes($bytes)
$hmacSecret = [Convert]::ToBase64String($bytes)

$rng.Dispose()

Write-Host ""
Write-Host "JWT Signing Key:" -ForegroundColor Cyan
Write-Host $jwtSigningKey

Write-Host ""
Write-Host "Step-Up HMAC Secret:" -ForegroundColor Cyan
Write-Host $hmacSecret

Write-Host ""
```

Keep the generated values private.

Do not commit them to Git.

---

# 6. Configure JWT and HMAC settings

PGLN.Auth reads the following configuration:

```json
{
  "PGLNAuth": {
    "Jwt": {
      "Issuer": "MyApplication",
      "Audience": "MyApplication",
      "SigningKey": "BASE64_ENCODED_256_BIT_OR_LARGER_KEY"
    },

    "StepUpSecurity": {
      "HmacSecret": "REPLACE_WITH_A_SECRET_AT_LEAST_32_CHARACTERS_LONG"
    }
  }
}
```

A complete local-development `appsettings.json` can therefore look like:

```json
{
  "ConnectionStrings": {
    "Auth": "Host=localhost;Port=5434;Database=pgln_auth;Username=postgres;Password=postgres"
  },

  "PGLNAuth": {
    "Jwt": {
      "Issuer": "MyAuthApp",
      "Audience": "MyAuthApp",
      "SigningKey": "YOUR_GENERATED_BASE64_JWT_SIGNING_KEY"
    },

    "StepUpSecurity": {
      "HmacSecret": "YOUR_GENERATED_HMAC_SECRET"
    }
  }
}
```

Replace the placeholder values with the values generated in the previous step.

## JWT signing key requirements

`PGLNAuth:Jwt:SigningKey` must:

- be Base64 encoded
- decode to at least 32 bytes / 256 bits

The default access-token lifetime is:

```text
15 minutes
```

## Step-up HMAC secret requirements

`PGLNAuth:StepUpSecurity:HmacSecret` must contain at least 32 characters.

Treat this value as a secret.

---

# 7. Configure PGLN.Auth

The basic PostgreSQL registration is:

```csharp
using PGLN.Auth;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("Auth")
    ?? throw new InvalidOperationException(
        "Auth connection string is missing.");

builder.Services.AddPGLNAuthPostgreSql(
    builder.Configuration,
    connectionString);
```

PGLN.Auth can also be configured using application options.

---

# 8. Configure application options

The following is a complete example:

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

> **Important:** Each option group is replaced as a complete object. The individual properties inside the option types are immutable after construction. When replacing an option group, specify the complete configuration you want the application to use.

---

# 9. Password policy

Applications can enforce stronger password requirements using `PasswordPolicy`.

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

Default settings:

| Setting | Default |
|---|---|
| Minimum password length | 12 |
| Maximum password length | 128 |
| Require uppercase | No |
| Require lowercase | No |
| Require digit | No |
| Require non-alphanumeric | No |

---

# 10. Refresh-token lifetime

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

# 11. Email verification lifetime

Configure email verification token lifetime:

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

# 12. Password reset lifetime

Configure password reset token lifetime:

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

# 13. Step-up authentication

Configure step-up challenges:

```csharp
auth.StepUp = new()
{
    Lifetime =
        TimeSpan.FromMinutes(5),

    MaxFailedAttempts = 5
};
```

Defaults:

| Setting | Default |
|---|---:|
| Challenge lifetime | 10 minutes |
| Maximum failed attempts | 5 |

---

# 14. Email delivery URLs

Authentication emails contain links generated using the configured email-delivery URLs.

Configure them:

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

The confirmation email may contain a URL similar to:

```text
https://example.com/confirm-email?token=<token>
```

The reset-password email may contain:

```text
https://example.com/reset-password?token=<token>
```

The consuming application owns these frontend/application routes.

For local development, you can use:

```text
https://localhost/confirm-email
https://localhost/confirm-email-change
https://localhost/reset-password
```

Production applications should replace these with their actual frontend URLs.

---

# 15. Email delivery

PGLN.Auth provides the email abstraction but does not force the consuming application to use a particular email provider.

The application provides an implementation of:

```csharp
PGLN.Auth.Application.Abstractions.Email.IEmailSender
```

The interface is:

```csharp
Task SendAsync(
    EmailMessage message,
    CancellationToken cancellationToken = default);
```

For local development and testing, a simple in-memory email sender is useful.

---

# 16. Development ConsoleEmailSender

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

This sender does not send real email.

It captures emails in memory so they can be inspected during development.

> Do not use this implementation as your production email provider.

---

# 17. Enable the email outbox worker

Register the ASP.NET Core outbox background worker:

```csharp
builder.Services.AddPGLNAuthOutboxBackgroundWorker();
```

The worker processes queued authentication email events in the background.

Default worker settings:

| Setting | Default |
|---|---|
| Poll interval | 5 seconds |
| Worker ID prefix | `aspnet` |
| Batch size | 20 |
| Maximum delivery attempts | 5 |
| Initial retry delay | 10 seconds |
| Maximum retry delay | 15 minutes |
| Claim duration | 2 minutes |

---

# 18. Custom email templates

PGLN.Auth provides a default email template renderer.

Applications can replace it with their own implementation of:

```csharp
PGLN.Auth.Application.Abstractions.Email.IEmailTemplateRenderer
```

The renderer supports:

- Email confirmation
- Email-change confirmation
- Welcome email
- Password reset
- Password changed
- New-device login
- Step-up verification code
- Account locked
- Email-changed notification

---

## 18.1 Create a custom renderer

Create:

```text
Infrastructure/
└── Email/
    └── ConsumerEmailTemplateRenderer.cs
```

The renderer should implement all methods defined by `IEmailTemplateRenderer`.

A minimal implementation follows the same public interface:

```csharp
using System.Net;
using PGLN.Auth.Application.Abstractions.Email;

namespace MyAuthApp.Infrastructure.Email;

public sealed class ConsumerEmailTemplateRenderer
    : IEmailTemplateRenderer
{
    public EmailTemplateResult RenderEmailConfirmation(
        EmailConfirmationEmailData data)
    {
        var subject = "Confirm your email address";

        var textBody =
            $"""
            Welcome.

            Please confirm your email address:

            {data.ConfirmationUrl}

            If you did not create this account, you can safely ignore this email.
            """;

        var htmlBody =
            CreateLayout(
                "Confirm your email address",
                $"""
                <p>Welcome.</p>

                <p>
                    Please confirm your email address to finish setting up
                    your account.
                </p>

                <p>
                    <a href="{HtmlEncode(data.ConfirmationUrl)}">
                        Confirm your email address
                    </a>
                </p>
                """);

        return new EmailTemplateResult(
            subject,
            textBody,
            htmlBody);
    }

    // Implement the remaining IEmailTemplateRenderer methods
    // using the signatures provided by the installed package version.

    private static string CreateLayout(
        string title,
        string content)
    {
        return $"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport"
                  content="width=device-width, initial-scale=1">
            <title>{HtmlEncode(title)}</title>
        </head>

        <body>
            <main>
                <h1>{HtmlEncode(title)}</h1>

                {content}

                <hr>

                <p>
                    This is a transactional message.
                </p>
            </main>
        </body>
        </html>
        """;
    }

    private static string HtmlEncode(string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}
```

> The exact parameter and return types should be copied from the `IEmailTemplateRenderer` interface exposed by the package version you are consuming. The important extension point is the public `IEmailTemplateRenderer` abstraction.

---

## 18.2 Register the custom renderer

Register the email sender first:

```csharp
builder.Services.AddPGLNAuthEmail<ConsoleEmailSender>();
```

Then register the consumer renderer:

```csharp
builder.Services.AddSingleton<
    IEmailTemplateRenderer,
    ConsumerEmailTemplateRenderer>();
```

### Registration order matters

The custom renderer should be registered **after** `AddPGLNAuthEmail<T>()`.

This allows the consuming application's renderer to override the default renderer supplied by PGLN.Auth.

---

# 19. Email inspection endpoints

For development, add an endpoint that exposes captured emails:

```csharp
app.MapGet("/test/emails", () =>
    ConsoleEmailSender.SentEmails.ToArray());
```

Inspect it with:

```powershell
Invoke-RestMethod `
    -Uri "http://localhost:5267/test/emails" `
    -Method Get
```

For more detail:

```powershell
Invoke-RestMethod `
    -Uri "http://localhost:5267/test/emails" `
    -Method Get |
    ConvertTo-Json -Depth 10
```

---

# 20. HTML email preview

You can render the latest captured email directly in a browser.

Add:

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

Then open:

```text
http://localhost:5267/test/email-preview
```

This is intended for local development only.

Remove or protect development email endpoints before deploying to production.

---

# 21. Apply database migrations

PGLN.Auth owns its authentication database schema.

The consuming application can apply the package migrations using:

```csharp
await app.Services.ApplyPGLNAuthMigrationsAsync();
```

Add this after creating the application:

```csharp
var app = builder.Build();

await app.Services.ApplyPGLNAuthMigrationsAsync();
```

This allows the consuming application to initialize or upgrade the PGLN.Auth database schema without directly referencing the package's internal migration infrastructure.

For environments where database migrations are managed separately from application startup, invoke the migration operation as part of your deployment process instead.

---

# 22. Complete Program.cs

The following is a complete development-oriented example combining the configuration described above:

```csharp
using PGLN.Auth;
using PGLN.Auth.Application.Abstractions.Email;
using PGLN.Auth.AspNetCore.Endpoints;
using PGLN.Auth.AspNetCore.Extensions;
using PGLN.Auth.AspNetCore.Outbox;
using MyAuthApp.Infrastructure.Email;

var builder = WebApplication.CreateBuilder(args);

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

// Optional: replace the default PGLN.Auth email renderer.
builder.Services.AddSingleton<
    IEmailTemplateRenderer,
    ConsumerEmailTemplateRenderer>();

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

// Development/testing only.
app.MapGet("/test/emails", () =>
    ConsoleEmailSender.SentEmails.ToArray());

// Development/testing only.
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

# 23. Build the application

Run:

```powershell
dotnet build
```

The application should build without errors.

---

# 24. Run the application

Run:

```powershell
dotnet run
```

ASP.NET Core will display the address where the application is listening.

For example:

```text
http://localhost:5267
```

---

# 25. Check the health endpoint

Open:

```text
http://localhost:5267/health
```

Or use PowerShell:

```powershell
Invoke-RestMethod `
    -Uri "http://localhost:5267/health" `
    -Method Get
```

Expected response:

```text
status
------
healthy
```

---

# 26. Register a test user

With the development application running:

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

The password must satisfy the password policy configured by the application.

---

# 27. Inspect the confirmation email

After registration, the email-confirmation event is placed into the outbox.

The background worker processes the event and invokes the registered email sender.

Inspect captured emails:

```powershell
Invoke-RestMethod `
    -Uri "http://localhost:5267/test/emails" `
    -Method Get |
    ConvertTo-Json -Depth 10
```

You should see the recipient, subject, plain-text body, and HTML body.

---

# 28. Preview the email

Open:

```text
http://localhost:5267/test/email-preview
```

The browser will render the latest captured HTML email.

This makes it possible to verify:

- Branding
- HTML layout
- CTA links
- Confirmation URL
- Security notices
- Footer
- Plain-text fallback

without requiring a real email provider.

---

# 29. Authentication endpoints

`MapPGLNAuthEndpoints()` uses:

```text
/api/auth
```

as the default prefix.

A different prefix can be supplied:

```csharp
app.MapPGLNAuthEndpoints("/auth");
```

---

## Registration and email

| Method | Endpoint |
|---|---|
| POST | `/api/auth/register` |
| POST | `/api/auth/confirm-email` |
| POST | `/api/auth/resend-confirmation` |
| POST | `/api/auth/change-email` |
| POST | `/api/auth/confirm-email-change` |

`/change-email` requires authentication.

---

## Authentication

| Method | Endpoint |
|---|---|
| POST | `/api/auth/login` |
| POST | `/api/auth/refresh` |
| POST | `/api/auth/logout` |
| POST | `/api/auth/logout-all` |
| POST | `/api/auth/verify-step-up` |

Login and step-up verification are rate limited.

---

## Password management

| Method | Endpoint |
|---|---|
| POST | `/api/auth/forgot-password` |
| POST | `/api/auth/reset-password` |
| POST | `/api/auth/change-password` |

`/change-password` requires authentication.

The forgot-password flow is designed to avoid exposing whether an account exists for a submitted email address.

---

## Sessions

| Method | Endpoint |
|---|---|
| GET | `/api/auth/sessions` |
| DELETE | `/api/auth/sessions/{sessionId}` |
| DELETE | `/api/auth/sessions/others` |

Session-management endpoints require authentication.

---

## Trusted devices

| Method | Endpoint |
|---|---|
| GET | `/api/auth/trusted-devices` |
| POST | `/api/auth/trusted-devices/current` |
| DELETE | `/api/auth/trusted-devices/{trustedDeviceId}` |

Trusted-device endpoints require authentication.

---

# 30. Google authentication

Google authentication is optional.

Endpoints:

| Method | Endpoint |
|---|---|
| GET | `/api/auth/external/google/start` |
| GET | `/api/auth/external/google/callback` |

Google authentication is only registered when both a client ID and client secret are configured.

Example:

```json
{
  "PGLNAuth": {
    "ExternalAuthentication": {
      "Google": {
        "ClientId": "YOUR_GOOGLE_CLIENT_ID",
        "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET",
        "AllowedRedirectUris": [
          "https://example.com/auth/google/callback"
        ]
      }
    }
  }
}
```

Configured Google redirect URIs must use HTTPS, except localhost development redirects where HTTP is permitted.

---

# 31. Refresh-token security

PGLN.Auth uses rotating refresh tokens.

When a refresh token is successfully exchanged, the previous token is replaced.

Reuse of a rotated refresh token is treated as replay and can invalidate the associated token family/session.

Security-sensitive account changes, including password and email changes, invalidate existing authentication sessions as appropriate.

---

# 32. Step-up authentication

PGLN.Auth supports step-up authentication for operations requiring stronger verification than possession of an access token alone.

Default challenge configuration:

```text
Lifetime: 10 minutes
Maximum failed attempts: 5
```

Applications can customize this:

```csharp
auth.StepUp = new()
{
    Lifetime = TimeSpan.FromMinutes(5),
    MaxFailedAttempts = 5
};
```

Trusted devices can reduce repeated verification on recognized devices.

---

# 33. Rate limiting and account lockout

Default login rate limiting:

```text
Permit limit: 10
Window: 1 minute
Queue limit: 0
```

Default step-up rate limiting:

```text
Permit limit: 5
Window: 1 minute
Queue limit: 0
```

Default account lockout:

| Setting | Default |
|---|---:|
| Failed attempts | 5 |
| Failure window | 15 minutes |
| Lockout duration | 15 minutes |

Default login email throttling:

| Setting | Default |
|---|---:|
| Attempts | 10 |
| Window | 5 minutes |

---

# 34. Default application settings

Unless overridden, PGLN.Auth uses:

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

Applications can override the appropriate option groups during registration.

---

# 35. Database inspection

You can connect to the development PostgreSQL database directly.

Start an interactive PostgreSQL shell:

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

You can also run individual commands from PowerShell.

For example:

```powershell
docker exec pgln-auth-postgres psql `
    -U postgres `
    -d pgln_auth `
    -c '\dt'
```

PGLN.Auth maintains authentication-related persistence including:

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
- Entity Framework migration history

The package owns its authentication schema.

Consuming applications should normally use:

```csharp
await app.Services.ApplyPGLNAuthMigrationsAsync();
```

rather than manually recreating the schema.

---

# 36. Middleware

A typical ASP.NET Core pipeline is:

```csharp
app.UsePGLNAuth();

app.UseAuthentication();
app.UseAuthorization();

app.MapPGLNAuthEndpoints();
```

Integrate additional application middleware according to the normal ASP.NET Core pipeline requirements of the consuming application.

---

# 37. Production deployment

The quick-start configuration is designed for local development.

Before deploying PGLN.Auth to production, review the following.

## Secrets

Never commit:

- JWT signing keys
- HMAC secrets
- database passwords
- Google client secrets
- email provider credentials

Use an appropriate secret-management solution.

## PostgreSQL

Use a production-grade PostgreSQL deployment.

Do not expose PostgreSQL unnecessarily to the public internet.

## Email

Replace:

```csharp
ConsoleEmailSender
```

with a real email provider implementation.

The provider may be:

- SMTP
- Amazon SES
- SendGrid
- Mailgun
- another transactional email service

The application owns this infrastructure integration.

## HTTPS

Authentication endpoints should be served over HTTPS.

## Database migrations

For production deployments, consider managing database migrations as a deployment operation rather than automatically applying them during application startup.

The package exposes:

```csharp
await app.Services.ApplyPGLNAuthMigrationsAsync();
```

for applications that want application-managed migration execution.

## Development endpoints

Remove or protect:

```text
/test/emails
/test/email-preview
```

before production deployment.

## Configuration

Keep environment-specific configuration outside source control wherever possible.

---

# 38. Recommended consumer project structure

A simple consumer application can use:

```text
MyAuthApp/
│
├── Infrastructure/
│   └── Email/
│       ├── ConsoleEmailSender.cs
│       └── ConsumerEmailTemplateRenderer.cs
│
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
└── MyAuthApp.csproj
```

The consuming application owns:

- Application branding
- Email templates
- Email provider integration
- Frontend URLs
- Password policy
- Environment configuration
- Deployment configuration

PGLN.Auth owns the reusable authentication infrastructure.

---

# 39. Architecture

Conceptually:

```text
┌─────────────────────────────────────┐
│           Consumer App             │
│                                     │
│ API / UI                            │
│ Branding                            │
│ Email Provider                      │
│ Email Templates                     │
│ Application Configuration           │
│ Deployment                          │
└──────────────────┬──────────────────┘
                   │
                   ▼
┌─────────────────────────────────────┐
│             PGLN.Auth               │
│                                     │
│ Registration                        │
│ Email Verification                  │
│ Login                               │
│ JWT                                 │
│ Refresh Tokens                      │
│ Password Reset                      │
│ Password Change                     │
│ Email Change                        │
│ Step-Up Authentication              │
│ Trusted Devices                     │
│ Sessions                            │
│ Lockout                             │
│ Throttling                          │
│ Outbox                              │
│ Persistence                         │
└──────────────────┬──────────────────┘
                   │
                   ▼
┌─────────────────────────────────────┐
│             PostgreSQL              │
└─────────────────────────────────────┘
```

This allows the same authentication foundation to be reused across multiple applications without copying authentication logic into every application.

---

# 40. Troubleshooting

## PostgreSQL container does not exist

Create it:

```powershell
docker run --name pgln-auth-postgres `
    -e POSTGRES_USER=postgres `
    -e POSTGRES_PASSWORD=postgres `
    -e POSTGRES_DB=pgln_auth `
    -p 5434:5432 `
    -d postgres:17
```

## PostgreSQL container exists but is stopped

```powershell
docker start pgln-auth-postgres
```

## Check PostgreSQL

```powershell
docker exec pgln-auth-postgres pg_isready `
    -U postgres `
    -d pgln_auth
```

## PostgreSQL connection fails

Verify:

```text
Host=localhost
Port=5434
Database=pgln_auth
Username=postgres
Password=postgres
```

Then:

```powershell
docker ps
```

## Database migrations fail

First check PostgreSQL:

```powershell
docker exec pgln-auth-postgres pg_isready `
    -U postgres `
    -d pgln_auth
```

Then inspect the application logs.

## Email is not appearing

Verify:

```csharp
builder.Services.AddPGLNAuthEmail<ConsoleEmailSender>();
```

and:

```csharp
builder.Services.AddPGLNAuthOutboxBackgroundWorker();
```

Also verify that the application has successfully applied its migrations.

## Custom email template is not being used

Make sure the registrations are ordered:

```csharp
builder.Services.AddPGLNAuthEmail<ConsoleEmailSender>();

builder.Services.AddSingleton<
    IEmailTemplateRenderer,
    ConsumerEmailTemplateRenderer>();
```

The custom renderer must be registered after the PGLN.Auth email registration.

## Password configuration is not being enforced

Verify that the customized options are passed to:

```csharp
builder.Services.AddPGLNAuthPostgreSql(
    builder.Configuration,
    connectionString,
    auth =>
    {
        // options
    });
```

Then rebuild and restart the application.

## JWT configuration errors

Verify:

```text
PGLNAuth:Jwt:Issuer
PGLNAuth:Jwt:Audience
PGLNAuth:Jwt:SigningKey
```

The signing key must be valid Base64 and decode to at least 32 bytes.

## Step-up configuration errors

Verify:

```text
PGLNAuth:StepUpSecurity:HmacSecret
```

The secret must contain at least 32 characters.

---

# 41. Version

Current package version:

```text
0.1.0
```

Install explicitly:

```powershell
dotnet add package PGLN.Auth --version 0.1.0
```

PGLN.Auth follows semantic versioning.

Pre-release versions use versions such as:

```text
0.1.0-rc.3
```

---

# 42. License

PGLN.Auth is licensed under the MIT License.
