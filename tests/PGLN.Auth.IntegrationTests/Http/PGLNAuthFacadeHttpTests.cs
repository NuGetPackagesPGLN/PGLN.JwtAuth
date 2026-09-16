using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.AspNetCore.Endpoints;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.IntegrationTests.Http;

public sealed class PGLNAuthFacadeHttpTests
{
    [Fact]
    public async Task AddPGLNAuth_CanBuildAndMapAuthenticationEndpoints()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        var builder =
            WebApplication.CreateBuilder();

        ConfigureJwt(
            builder.Configuration);

        builder.WebHost.UseTestServer();

        builder.Services.AddPGLNAuth(
            builder.Configuration,
            options =>
                options.UseSqlite(
                    connection));

        var application =
            builder.Build();

        try
        {
            application.UseAuthentication();
            application.UseAuthorization();
            application.UseRateLimiter();

            application.MapPGLNAuthEndpoints();

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

            using var client =
                application.GetTestClient();

            var response =
                await client.PostAsJsonAsync(
                    "/api/auth/register",
                    new
                    {
                        Email =
                            "facade-test@example.com",

                        Password =
                            "FacadeTestPassword123!"
                    });

            Assert.Equal(
                System.Net.HttpStatusCode.Created,
                response.StatusCode);
        }
        finally
        {
            await application.StopAsync();
            await application.DisposeAsync();
        }
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
                    "PGLN.Auth.FacadeTests",

                ["PGLNAuth:Jwt:Audience"] =
                    "PGLN.Auth.FacadeTests.Client",

                ["PGLNAuth:Jwt:SigningKey"] =
                    Convert.ToBase64String(
                        signingKey),

                ["PGLNAuth:Jwt:AccessTokenLifetime"] =
                    "00:15:00",

                ["PGLNAuth:StepUpSecurity:HmacSecret"] =
                    "facade-integration-test-hmac-secret-32-characters-minimum"
            });
    }
}
