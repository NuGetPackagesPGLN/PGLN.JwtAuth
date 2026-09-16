using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.AspNetCore.Authentication;
using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Infrastructure.Tests.AspNetCore;

public sealed class SessionAwareJwtAuthenticationTests
{
    private const string Issuer =
        "https://issuer.test";

    private const string Audience =
        "pglnauth-tests";

    private const string SigningKey =
        "MDEyMzQ1Njc4OWFiY2RlZjAxMjM0NTY3ODlhYmNkZWY=";
    [Fact]
    public async Task ProtectedEndpoint_WithActiveSession_ShouldReturnOk()
    {
        var userId =
            new UserId(
                Guid.NewGuid());

        var sessionId =
            AuthSessionId.New();

        var session =
            AuthSession.Create(
                sessionId,
                userId,
                "device-001",
                "Test Device",
                "127.0.0.1",
                "PGLN.Auth.Tests",
                DateTimeOffset.UtcNow.AddMinutes(-5));

        var sessionRepository =
            new TestAuthSessionRepository(
                session);

        var builder =
            WebApplication.CreateBuilder();

        builder.WebHost.UseTestServer();

        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["PGLNAuth:Jwt:Issuer"] =
                    Issuer,

                ["PGLNAuth:Jwt:Audience"] =
                    Audience,

                ["PGLNAuth:Jwt:SigningKey"] =
                    SigningKey
            });

        builder.Services.AddSingleton<IAuthSessionRepository>(
            sessionRepository);

        builder.Services.AddPGLNAuthAspNetCore(
            builder.Configuration);

        await using var app =
            builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet(
                "/protected",
                () => Results.Ok())
            .RequireAuthorization();

        await app.StartAsync();

        var client =
            app.GetTestClient();

        var token =
            CreateAccessToken(
                sessionId);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                JwtBearerDefaults.AuthenticationScheme,
                token);

        var response =
            await client.GetAsync(
                "/protected");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }
    [Fact]
    public async Task ProtectedEndpoint_WithMissingSession_ShouldReturnUnauthorized()
    {
        var sessionId =
            AuthSessionId.New();

        var sessionRepository =
            new TestAuthSessionRepository();

        var builder =
            WebApplication.CreateBuilder();

        builder.WebHost.UseTestServer();

        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["PGLNAuth:Jwt:Issuer"] =
                    Issuer,

                ["PGLNAuth:Jwt:Audience"] =
                    Audience,

                ["PGLNAuth:Jwt:SigningKey"] =
                    SigningKey
            });

        builder.Services.AddSingleton<IAuthSessionRepository>(
            sessionRepository);

        builder.Services.AddPGLNAuthAspNetCore(
            builder.Configuration);

        await using var app =
            builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet(
                "/protected",
                () => Results.Ok())
            .RequireAuthorization();

        await app.StartAsync();

        var client =
            app.GetTestClient();

        var token =
            CreateAccessToken(
                sessionId);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                JwtBearerDefaults.AuthenticationScheme,
                token);

        var response =
            await client.GetAsync(
                "/protected");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
    [Fact]
    public async Task ProtectedEndpoint_WithoutSessionIdClaim_ShouldReturnUnauthorized()
    {
        var sessionRepository =
            new TestAuthSessionRepository();

        var builder =
            WebApplication.CreateBuilder();

        builder.WebHost.UseTestServer();

        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["PGLNAuth:Jwt:Issuer"] =
                    Issuer,

                ["PGLNAuth:Jwt:Audience"] =
                    Audience,

                ["PGLNAuth:Jwt:SigningKey"] =
                    SigningKey
            });

        builder.Services.AddSingleton<IAuthSessionRepository>(
            sessionRepository);

        builder.Services.AddPGLNAuthAspNetCore(
            builder.Configuration);

        await using var app =
            builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet(
                "/protected",
                () => Results.Ok())
            .RequireAuthorization();

        await app.StartAsync();

        var client =
            app.GetTestClient();

        var token =
            CreateAccessTokenWithoutSessionId();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                JwtBearerDefaults.AuthenticationScheme,
                token);

        var response =
            await client.GetAsync(
                "/protected");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
    [Fact]
    public async Task ProtectedEndpoint_WithMalformedSessionId_ShouldReturnUnauthorized()
    {
        var sessionRepository =
            new TestAuthSessionRepository();

        var builder =
            WebApplication.CreateBuilder();

        builder.WebHost.UseTestServer();

        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["PGLNAuth:Jwt:Issuer"] =
                    Issuer,

                ["PGLNAuth:Jwt:Audience"] =
                    Audience,

                ["PGLNAuth:Jwt:SigningKey"] =
                    SigningKey
            });

        builder.Services.AddSingleton<IAuthSessionRepository>(
            sessionRepository);

        builder.Services.AddPGLNAuthAspNetCore(
            builder.Configuration);

        await using var app =
            builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet(
                "/protected",
                () => Results.Ok())
            .RequireAuthorization();

        await app.StartAsync();

        var client =
            app.GetTestClient();

        var token =
            CreateAccessTokenWithSessionId(
                "not-a-valid-guid");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                JwtBearerDefaults.AuthenticationScheme,
                token);

        var response =
            await client.GetAsync(
                "/protected");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }





    [Fact]
    public async Task ProtectedEndpoint_WithRevokedSession_ShouldReturnUnauthorized()
    {
        var userId =
            new UserId(
                Guid.NewGuid());

        var sessionId =
            AuthSessionId.New();

        var session =
            AuthSession.Create(
                sessionId,
                userId,
                "device-001",
                "Test Device",
                "127.0.0.1",
                "PGLN.Auth.Tests",
                DateTimeOffset.UtcNow.AddMinutes(-5));

        session.Revoke(
            DateTimeOffset.UtcNow,
            "Test revocation.");

        var sessionRepository =
            new TestAuthSessionRepository(
                session);

        var builder =
            WebApplication.CreateBuilder();

        builder.WebHost.UseTestServer();

        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["PGLNAuth:Jwt:Issuer"] =
                    Issuer,

                ["PGLNAuth:Jwt:Audience"] =
                    Audience,

                ["PGLNAuth:Jwt:SigningKey"] =
                    SigningKey
            });

        builder.Services.AddSingleton<IAuthSessionRepository>(
            sessionRepository);

        builder.Services.AddPGLNAuthAspNetCore(
            builder.Configuration);

        await using var app =
            builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet(
                "/protected",
                () => Results.Ok())
            .RequireAuthorization();

        await app.StartAsync();

        var client =
            app.GetTestClient();

        var token =
            CreateAccessToken(
                sessionId);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                JwtBearerDefaults.AuthenticationScheme,
                token);

        var response =
            await client.GetAsync(
                "/protected");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
    private static string CreateAccessTokenWithSessionId(
        string sessionId)
    {
        var signingKeyBytes =
            Convert.FromBase64String(
                SigningKey);

        var signingKey =
            new SymmetricSecurityKey(
                signingKeyBytes);

        var credentials =
            new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256);

        var now =
            DateTimeOffset.UtcNow;

        var claims =
            new[]
            {
                new Claim(
                    JwtRegisteredClaimNames.Sub,
                    Guid.NewGuid().ToString()),

                new Claim(
                    "sid",
                    sessionId),

                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString()),

                new Claim(
                    JwtRegisteredClaimNames.Iat,
                    now
                        .ToUnixTimeSeconds()
                        .ToString(),
                    ClaimValueTypes.Integer64)
            };

        var token =
            new JwtSecurityToken(
                issuer:
                    Issuer,
                audience:
                    Audience,
                claims:
                    claims,
                notBefore:
                    now.UtcDateTime,
                expires:
                    now.AddMinutes(15).UtcDateTime,
                signingCredentials:
                    credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(
                token);
    }

    private static string CreateAccessTokenWithoutSessionId()
    {
        var signingKeyBytes =
            Convert.FromBase64String(
                SigningKey);

        var signingKey =
            new SymmetricSecurityKey(
                signingKeyBytes);

        var credentials =
            new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256);

        var now =
            DateTimeOffset.UtcNow;

        var claims =
            new[]
            {
                new Claim(
                    JwtRegisteredClaimNames.Sub,
                    Guid.NewGuid().ToString()),

                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString()),

                new Claim(
                    JwtRegisteredClaimNames.Iat,
                    now
                        .ToUnixTimeSeconds()
                        .ToString(),
                    ClaimValueTypes.Integer64)
            };

        var token =
            new JwtSecurityToken(
                issuer:
                    Issuer,
                audience:
                    Audience,
                claims:
                    claims,
                notBefore:
                    now.UtcDateTime,
                expires:
                    now.AddMinutes(15).UtcDateTime,
                signingCredentials:
                    credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(
                token);
    }


    private static string CreateAccessToken(
        AuthSessionId sessionId)
    {
        var signingKeyBytes =
            Convert.FromBase64String(
                SigningKey);

        var signingKey =
            new SymmetricSecurityKey(
                signingKeyBytes);

        var credentials =
            new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256);

        var now =
            DateTimeOffset.UtcNow;

        var claims =
            new[]
            {
                new Claim(
                    JwtRegisteredClaimNames.Sub,
                    Guid.NewGuid().ToString()),

                new Claim(
                    "sid",
                    sessionId.Value.ToString()),

                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString()),

                new Claim(
                    JwtRegisteredClaimNames.Iat,
                    now
                        .ToUnixTimeSeconds()
                        .ToString(),
                    ClaimValueTypes.Integer64)
            };

        var token =
            new JwtSecurityToken(
                issuer:
                    Issuer,
                audience:
                    Audience,
                claims:
                    claims,
                notBefore:
                    now.UtcDateTime,
                expires:
                    now.AddMinutes(15).UtcDateTime,
                signingCredentials:
                    credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(
                token);
    }

    private sealed class TestAuthSessionRepository
        : IAuthSessionRepository
    {
        private readonly Dictionary<AuthSessionId, AuthSession>
            _sessions = new();

        public TestAuthSessionRepository(
            params AuthSession[] sessions)
        {
            foreach (var session in sessions)
            {
                _sessions[session.Id] =
                    session;
            }
        }

        public Task<AuthSession?> GetByIdAsync(
            AuthSessionId sessionId,
            CancellationToken cancellationToken = default)
        {
            _sessions.TryGetValue(
                sessionId,
                out var session);

            return Task.FromResult(
                session);
        }

        public Task<IReadOnlyCollection<AuthSession>> GetByUserIdAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<AuthSession> sessions =
                _sessions.Values
                    .Where(
                        session =>
                            session.UserId == userId)
                    .ToArray();

            return Task.FromResult(
                sessions);
        }

        public Task<AuthSession?> GetActiveByDeviceIdHashAsync(
            UserId userId,
            string deviceIdHash,
            CancellationToken cancellationToken = default)
        {
            var session =
                _sessions.Values
                    .FirstOrDefault(
                        session =>
                            session.UserId == userId &&
                            session.DeviceIdHash ==
                                deviceIdHash &&
                            session.IsActive);

            return Task.FromResult(
                session);
        }

        public Task<bool> HasSeenDeviceAsync(
            UserId userId,
            string deviceIdHash,
            CancellationToken cancellationToken = default)
        {
            var result =
                _sessions.Values.Any(
                    session =>
                        session.UserId == userId &&
                        session.DeviceIdHash ==
                            deviceIdHash);

            return Task.FromResult(
                result);
        }

        public Task<bool> HasAnySessionAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
        {
            var result =
                _sessions.Values.Any(
                    session =>
                        session.UserId == userId);

            return Task.FromResult(
                result);
        }

        public Task AddAsync(
            AuthSession session,
            CancellationToken cancellationToken = default)
        {
            _sessions[session.Id] =
                session;

            return Task.CompletedTask;
        }
    }
}
