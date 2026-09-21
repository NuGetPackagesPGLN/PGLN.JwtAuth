using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PGLN.Auth.Application;
using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Abstractions.ExternalAuthentication;
using PGLN.Auth.Application.Abstractions.Email;
using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Application.Features.Login;
using PGLN.Auth.AspNetCore.Authentication;
using PGLN.Auth.AspNetCore.Endpoints;
using PGLN.Auth.AspNetCore.RateLimiting;
using PGLN.Auth.EntityFrameworkCore;
using PGLN.Auth.EntityFrameworkCore.Persistence;
using PGLN.Auth.Infrastructure;

namespace PGLN.Auth.IntegrationTests.Http;

internal sealed class HttpTestApplication
    : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private HttpTestApplication(
        WebApplication application,
        SqliteConnection connection)
    {
        Application =
            application;

        _connection =
            connection;
    }

    public WebApplication Application { get; }

    public HttpClient CreateClient()
    {
        return Application.GetTestClient();
    }

    public static async Task<HttpTestApplication> CreateAsync(
        LoginRateLimitOptions? loginRateLimitOptions = null,
        LoginEmailThrottleOptions? loginEmailThrottleOptions = null,
        StepUpRateLimitOptions? stepUpRateLimitOptions = null)
    {
        var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        var builder =
            WebApplication.CreateBuilder();

        ConfigureJwt(
            builder.Configuration);

        builder.WebHost.UseTestServer();

        builder.Services
            .AddPGLNAuthApplication(
                loginEmailThrottleOptions:
                    loginEmailThrottleOptions);

        builder.Services
            .AddPGLNAuthInfrastructure(
                builder.Configuration);

        builder.Services.RemoveAll<
            IExternalIdentityProvider>();

        builder.Services.AddSingleton<
            HttpTestGoogleExternalIdentityProvider>();

        builder.Services.AddSingleton<
            IExternalIdentityProvider>(
                serviceProvider =>
                    serviceProvider.GetRequiredService<
                        HttpTestGoogleExternalIdentityProvider>());

        builder.Services
            .AddPGLNAuthAspNetCore(
                builder.Configuration,
                loginRateLimitOptions,
                stepUpRateLimitOptions);

        builder.Services
            .AddPGLNAuthEntityFrameworkCore(
                options =>
                    options.UseSqlite(
                        connection));

        builder.Services.AddSingleton<
            IStepUpCodeGenerator,
            HttpTestStepUpCodeGenerator>();

        builder.Services.AddSingleton<
            IIntegrationEventPayloadProtector,
            HttpTestPayloadProtector>();

        builder.Services.AddSingleton<
            IEmailSender,
            HttpTestEmailSender>();

        builder.Services.AddSingleton<
            IEmailTemplateRenderer,
            HttpTestEmailTemplateRenderer>();

        var application =
            builder.Build();

        application.UseAuthentication();
        application.UseAuthorization();
        application.UseRateLimiter();

        application.MapPGLNAuthEndpoints();

        application.MapGet(
            "/protected",
            () => Results.NoContent())
            .RequireAuthorization();

        await application.StartAsync();

        await using (
            var scope =
                application.Services.CreateAsyncScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AuthDbContext>();

            await dbContext.Database
                .EnsureCreatedAsync();
        }

        return new HttpTestApplication(
            application,
            connection);
    }

    private static void ConfigureJwt(
        ConfigurationManager configuration)
    {
        var signingKey =
            RandomNumberGenerator.GetBytes(
                32);

        configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["PGLNAuth:Jwt:Issuer"] =
                    "PGLN.Auth.IntegrationTests",

                ["PGLNAuth:Jwt:Audience"] =
                    "PGLN.Auth.IntegrationTests.Client",

                ["PGLNAuth:Jwt:SigningKey"] =
                    Convert.ToBase64String(
                        signingKey),

                ["PGLNAuth:Jwt:AccessTokenLifetime"] =
                    "00:15:00",

                ["PGLNAuth:StepUpSecurity:HmacSecret"] =
                    "integration-test-step-up-hmac-secret-32-characters-minimum",

                ["PGLNAuth:ExternalAuthentication:Google:ClientId"] =
                    "integration-test-google-client-id",

                ["PGLNAuth:ExternalAuthentication:Google:ClientSecret"] =
                    "integration-test-google-client-secret",

                ["PGLNAuth:ExternalAuthentication:Google:AllowedRedirectUris:0"] =
                    "https://app.example.test/signin-google"
            });
    }

    public async ValueTask DisposeAsync()
    {
        await Application.StopAsync();

        await Application.DisposeAsync();

        await _connection.DisposeAsync();
    }
}




