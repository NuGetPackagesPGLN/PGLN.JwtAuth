using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.AspNetCore.RateLimiting;
using PGLN.Auth.Domain.Sessions;
using System.Threading.RateLimiting;

namespace PGLN.Auth.AspNetCore.Authentication;

public static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddPGLNAuthAspNetCore(
        this IServiceCollection services,
        IConfiguration configuration,
        LoginRateLimitOptions? loginRateLimitOptions = null,
        StepUpRateLimitOptions? stepUpRateLimitOptions = null)
    {
        ArgumentNullException.ThrowIfNull(
            services);

        ArgumentNullException.ThrowIfNull(
            configuration);

        var issuer =
            configuration[
                "PGLNAuth:Jwt:Issuer"];

        var audience =
            configuration[
                "PGLNAuth:Jwt:Audience"];

        var signingKey =
            configuration[
                "PGLNAuth:Jwt:SigningKey"];

        ArgumentException.ThrowIfNullOrWhiteSpace(
            issuer);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            audience);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            signingKey);

        var signingKeyBytes =
            Convert.FromBase64String(
                signingKey);

        services
            .AddAuthentication(
                JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(
                options =>
                {
                    options.MapInboundClaims =
                        false;

                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidateIssuer =
                                true,

                            ValidIssuer =
                                issuer,

                            ValidateAudience =
                                true,

                            ValidAudience =
                                audience,

                            ValidateIssuerSigningKey =
                                true,

                            IssuerSigningKey =
                                new SymmetricSecurityKey(
                                    signingKeyBytes),

                            ValidateLifetime =
                                true,

                            ClockSkew =
                                TimeSpan.Zero
                        };

                    options.Events =
                        new JwtBearerEvents
                        {
                            OnTokenValidated =
                                async context =>
                                {
                                    var sessionIdValue =
                                        context.Principal?
                                            .FindFirst(
                                                "sid")?
                                            .Value;

                                    if (string.IsNullOrWhiteSpace(
                                            sessionIdValue))
                                    {
                                        context.Fail(
                                            "Access token does not contain a session id.");

                                        return;
                                    }

                                    if (!Guid.TryParse(
                                            sessionIdValue,
                                            out var sessionIdGuid))
                                    {
                                        context.Fail(
                                            "Access token contains an invalid session id.");

                                        return;
                                    }

                                    var sessionRepository =
                                        context.HttpContext
                                            .RequestServices
                                            .GetRequiredService<IAuthSessionRepository>();

                                    var session =
                                        await sessionRepository
                                            .GetByIdAsync(
                                                new AuthSessionId(
                                                    sessionIdGuid),
                                                context.HttpContext
                                                    .RequestAborted);

                                    if (session is null)
                                    {
                                        context.Fail(
                                            "Authentication session does not exist.");

                                        return;
                                    }

                                    if (!session.IsActive)
                                    {
                                        context.Fail(
                                            "Authentication session has been revoked.");

                                        return;
                                    }
                                }
                        };
                });

        services.AddAuthorization();

        loginRateLimitOptions ??=
            new LoginRateLimitOptions();

        loginRateLimitOptions.Validate();

        services.AddSingleton(
            loginRateLimitOptions);

        stepUpRateLimitOptions ??=
            new StepUpRateLimitOptions();

        stepUpRateLimitOptions.Validate();

        services.AddSingleton(
            stepUpRateLimitOptions);

        services.AddRateLimiter(
            options =>
            {
                options.RejectionStatusCode =
                    StatusCodes.Status429TooManyRequests;

                options.AddPolicy(
                    LoginRateLimitOptions.PolicyName,
                    httpContext =>
                    {
                        var clientIp =
                            httpContext.Connection
                                .RemoteIpAddress?
                                .ToString()
                            ?? "unknown";

                        return RateLimitPartition
                            .GetFixedWindowLimiter(
                                clientIp,
                                _ =>
                                    new FixedWindowRateLimiterOptions
                                    {
                                        PermitLimit =
                                            loginRateLimitOptions.PermitLimit,

                                        Window =
                                            loginRateLimitOptions.Window,

                                        QueueLimit =
                                            loginRateLimitOptions.QueueLimit,

                                        QueueProcessingOrder =
                                            QueueProcessingOrder.OldestFirst,

                                        AutoReplenishment =
                                            true
                                    });
                    });

                options.AddPolicy(
                    StepUpRateLimitOptions.PolicyName,
                    httpContext =>
                    {
                        var clientIp =
                            httpContext.Connection
                                .RemoteIpAddress?
                                .ToString()
                            ?? "unknown";

                        return RateLimitPartition
                            .GetFixedWindowLimiter(
                                clientIp,
                                _ =>
                                    new FixedWindowRateLimiterOptions
                                    {
                                        PermitLimit =
                                            stepUpRateLimitOptions.PermitLimit,

                                        Window =
                                            stepUpRateLimitOptions.Window,

                                        QueueLimit =
                                            stepUpRateLimitOptions.QueueLimit,

                                        QueueProcessingOrder =
                                            QueueProcessingOrder.OldestFirst,

                                        AutoReplenishment =
                                            true
                                    });
                    });
            });

        return services;
    }
}
