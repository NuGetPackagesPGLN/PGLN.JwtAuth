using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PGLN.Auth.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace PGLN.Auth.AspNetCore.Authentication;

public static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddPGLNAuthAspNetCore(
        this IServiceCollection services,
        IConfiguration configuration,
        LoginRateLimitOptions? loginRateLimitOptions = null)
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
                });

        services.AddAuthorization();

        loginRateLimitOptions ??=
            new LoginRateLimitOptions();

        loginRateLimitOptions.Validate();

        services.AddSingleton(
            loginRateLimitOptions);

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
            });

        return services;
    }
}


