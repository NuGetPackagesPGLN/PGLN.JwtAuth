# PGLN.Auth

PGLN.Auth is a reusable authentication and identity package for ASP.NET Core applications.

It provides a prebuilt authentication foundation for applications that need registration, email verification, login, JWT access tokens, refresh-token rotation, session management, password recovery, step-up authentication, trusted devices, email changes, and optional Google authentication.

The package is designed for .NET 10 applications and includes PostgreSQL persistence through Entity Framework Core.

## Features

PGLN.Auth includes:

- User registration
- Email confirmation
- Resend email confirmation
- Login
- JWT access tokens
- Refresh-token rotation
- Refresh-token replay detection
- Logout
- Logout from all sessions
- Session management
- Forgot password
- Reset password
- Change password
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

## Requirements

- .NET 10
- ASP.NET Core
- PostgreSQL when using `AddPGLNAuthPostgreSql`

## Installation

Install the package from NuGet:

```bash
dotnet add package PGLN.Auth --version 0.1.0
```

## Basic setup

Register PGLN.Auth in `Program.cs`:

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

var app = builder.Build();

await app.Services.ApplyPGLNAuthMigrationsAsync();

app.UsePGLNAuth();

app.UseAuthentication();
app.UseAuthorization();

app.MapPGLNAuthEndpoints();

app.Run();
```

By default, authentication endpoints are mapped under:

```text
/api/auth
```

A different prefix can be supplied:

```csharp
app.MapPGLNAuthEndpoints("/auth");
```

## Required configuration

PGLN.Auth requires JWT configuration and a step-up authentication HMAC secret.

Example `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Auth": "Host=localhost;Port=5432;Database=my_app;Username=postgres;Password=change-me"
  },

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

Do not commit production secrets to source control.

Use environment variables, a secret manager, or another secure configuration provider in deployed environments.

### JWT signing key

`PGLNAuth:Jwt:SigningKey` must:

- be Base64 encoded
- decode to at least 32 bytes (256 bits)

The default access-token lifetime is 15 minutes.

### Step-up HMAC secret

`PGLNAuth:StepUpSecurity:HmacSecret` must contain at least 32 characters.

It is used to protect step-up authentication security data and should be treated as a secret.

## Database migrations

PGLN.Auth owns its authentication schema and provides a migration API.

Apply migrations during application startup:

```csharp
await app.Services.ApplyPGLNAuthMigrationsAsync();
```

This allows consuming applications to initialize or upgrade the PGLN.Auth database schema without directly referencing the package's internal migration infrastructure.

For environments where database migrations are managed separately from application startup, invoke the migration operation as part of your deployment process instead.

## Application options

Application-level authentication behavior can be customized when registering PGLN.Auth.

```csharp
builder.Services.AddPGLNAuthPostgreSql(
    builder.Configuration,
    connectionString,
    auth =>
    {
        auth.PasswordPolicy = new()
        {
            MinimumLength = 16,
            RequireUppercase = true,
            RequireLowercase = true,
            RequireDigit = true
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

Each option group is replaced as a complete object. The individual properties inside the option types are immutable after construction.

## Default application settings

Unless overridden, PGLN.Auth uses the following defaults:

| Setting | Default |
|---|---|
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

Applications can enforce stronger password requirements through `PasswordPolicy`.

## Email delivery

Features such as email confirmation, password recovery, password-change notifications, and email changes require an application-provided email sender.

Register your implementation through the PGLN.Auth email integration.

The application is responsible for connecting the package to its actual email provider.

Email links use the configured `EmailDelivery` URLs.

Default development URLs are:

```text
https://localhost/confirm-email
https://localhost/confirm-email-change
https://localhost/reset-password
```

Production applications should replace these with their real frontend URLs.

## Outbox processing

PGLN.Auth uses an outbox for durable asynchronous email delivery.

The ASP.NET Core outbox background worker can be enabled by the consuming application so queued authentication emails are processed in the background.

The worker is enabled by default once the background-worker integration is registered.

Default worker behavior includes:

| Setting | Default |
|---|---|
| Poll interval | 5 seconds |
| Worker ID prefix | `aspnet` |
| Batch size | 20 |
| Maximum delivery attempts | 5 |
| Initial retry delay | 10 seconds |
| Maximum retry delay | 15 minutes |
| Claim duration | 2 minutes |

## Endpoints

`MapPGLNAuthEndpoints()` uses `/api/auth` as its default prefix.

### Registration and email

| Method | Endpoint |
|---|---|
| POST | `/api/auth/register` |
| POST | `/api/auth/confirm-email` |
| POST | `/api/auth/resend-confirmation` |
| POST | `/api/auth/change-email` |
| POST | `/api/auth/confirm-email-change` |

`/change-email` requires authentication.

### Authentication

| Method | Endpoint |
|---|---|
| POST | `/api/auth/login` |
| POST | `/api/auth/refresh` |
| POST | `/api/auth/logout` |
| POST | `/api/auth/logout-all` |
| POST | `/api/auth/verify-step-up` |

Login and step-up verification are rate limited.

### Password management

| Method | Endpoint |
|---|---|
| POST | `/api/auth/forgot-password` |
| POST | `/api/auth/reset-password` |
| POST | `/api/auth/change-password` |

`/change-password` requires authentication.

The forgot-password flow is designed to avoid exposing whether an account exists for a submitted email address.

### Sessions

| Method | Endpoint |
|---|---|
| GET | `/api/auth/sessions` |
| DELETE | `/api/auth/sessions/{sessionId}` |
| DELETE | `/api/auth/sessions/others` |

Session-management endpoints require authentication.

### Trusted devices

| Method | Endpoint |
|---|---|
| GET | `/api/auth/trusted-devices` |
| POST | `/api/auth/trusted-devices/current` |
| DELETE | `/api/auth/trusted-devices/{trustedDeviceId}` |

Trusted-device endpoints require authentication.

## Google authentication

Optional Google authentication endpoints are:

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

## Refresh-token security

PGLN.Auth uses rotating refresh tokens.

When a refresh token is successfully exchanged, the previous token is replaced.

Reuse of a rotated refresh token is treated as replay and can invalidate the associated token family/session.

Security-sensitive account changes, including password and email changes, invalidate existing authentication sessions as appropriate.

## Step-up authentication

PGLN.Auth supports step-up authentication for operations that require stronger verification than possession of an access token alone.

Default challenge behavior:

```text
Lifetime: 10 minutes
Maximum failed attempts: 5
```

Trusted-device support can be used to reduce repeated verification on recognized devices.

## Rate limiting and lockout

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

Account-level lockout and email-based login throttling provide additional protection against repeated authentication failures.

## Middleware

A typical application pipeline is:

```csharp
app.UsePGLNAuth();

app.UseAuthentication();
app.UseAuthorization();

app.MapPGLNAuthEndpoints();
```

If the consuming application has additional middleware or authorization requirements, integrate PGLN.Auth into the normal ASP.NET Core middleware ordering used by that application.

## Architecture

PGLN.Auth is split internally into focused assemblies for:

- Application logic
- Domain logic
- Infrastructure
- ASP.NET Core integration
- Entity Framework Core persistence
- PostgreSQL support
- Contracts

Consumers normally install and reference only the `PGLN.Auth` package and use the public facade rather than wiring the internal assemblies manually.

## Versioning

PGLN.Auth follows semantic versioning.

The first stable release is:

```text
0.1.0
```

Pre-release builds use release-candidate versions such as:

```text
0.1.0-rc.3
```

## License

PGLN.Auth is licensed under the MIT License.
