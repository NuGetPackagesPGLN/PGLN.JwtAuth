# PGLN.Auth

PGLN.Auth is reusable authentication and identity infrastructure for ASP.NET Core applications.

> PGLN.Auth is currently a preview package. APIs and configuration may change before the first stable release.

## Requirements

- .NET 10
- ASP.NET Core
- PostgreSQL
- Entity Framework Core

## Installation

Install the package:

```powershell
dotnet add package PGLN.Auth --version 0.1.0-preview.1
```

## Basic setup

Register PGLN.Auth with PostgreSQL:

```csharp
using PGLN.Auth;

var connectionString =
    builder.Configuration.GetConnectionString("AuthDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'AuthDatabase' is not configured.");

builder.Services.AddPGLNAuthPostgreSql(
    builder.Configuration,
    connectionString);
```

Configure the ASP.NET Core pipeline:

```csharp
using PGLN.Auth.AspNetCore.Endpoints;
using PGLN.Auth.AspNetCore.Extensions;

var app = builder.Build();

app.UsePGLNAuth();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapPGLNAuthEndpoints();

app.Run();
```

`UsePGLNAuth()` provides PGLN.Auth-specific HTTP boundary behavior. Command validation failures are converted into standardized `400 Bad Request` responses.

## Email delivery

The host application provides the email sender:

```csharp
builder.Services.AddPGLNAuthEmail<MyEmailSender>();
```

## Outbox worker

Enable in-process outbox processing with:

```csharp
using PGLN.Auth.AspNetCore.Outbox;

builder.Services.AddPGLNAuthOutboxBackgroundWorker();
```

## Data Protection

PGLN.Auth uses ASP.NET Core Data Protection for protected authentication infrastructure, including protected outbox payloads.

The host application is responsible for choosing an appropriate persistent Data Protection key store for its deployment environment.

When multiple application instances share the same PGLN.Auth database or outbox, those instances must use a compatible shared persistent Data Protection key ring and a stable application name.

Do not run multiple PGLN.Auth outbox workers against the same database with isolated Data Protection key rings. One instance may otherwise claim an outbox message encrypted by another instance and be unable to decrypt it.

## Security

The host application remains responsible for deployment-specific security including secret management, HTTPS, trusted proxy configuration, Data Protection key storage, database security, and operational monitoring.

Do not commit JWT signing keys, OAuth client secrets, Data Protection keys, database credentials, or other production secrets to source control.

## Status

Current version: `0.1.0-preview.1`

PGLN.Auth is under active development and should be evaluated carefully before production use.
