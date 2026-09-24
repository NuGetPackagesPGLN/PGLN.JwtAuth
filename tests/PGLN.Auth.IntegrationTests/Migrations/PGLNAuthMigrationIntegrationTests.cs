using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.IntegrationTests.Migrations;

public sealed class PGLNAuthMigrationIntegrationTests
{
    private const string ConnectionStringEnvironmentVariable =
        "PGLN_AUTH_MIGRATION_TEST_CONNECTION_STRING";

    [Fact]
    public async Task ApplyPGLNAuthMigrationsAsync_AppliesPendingMigrations()
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                ConnectionStringEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["PGLNAuth:Jwt:Issuer"] =
                            "PGLN.Auth.IntegrationTests",

                        ["PGLNAuth:Jwt:Audience"] =
                            "PGLN.Auth.IntegrationTests",

                        ["PGLNAuth:Jwt:SigningKey"] =
                            Convert.ToBase64String(
                                new byte[64]),

                        ["PGLNAuth:StepUpSecurity:HmacSecret"] =
                            Convert.ToBase64String(
                                new byte[32])
                    })
                .Build();

        var services =
            new ServiceCollection();

        services.AddPGLNAuthPostgreSql(
            configuration,
            connectionString);

        await using var serviceProvider =
            services.BuildServiceProvider();

        await using (var scope =
            serviceProvider.CreateAsyncScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AuthDbContext>();

            await dbContext.Database.EnsureDeletedAsync();
        }

        await serviceProvider
            .ApplyPGLNAuthMigrationsAsync();

        await using var verificationScope =
            serviceProvider.CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<AuthDbContext>();

        var appliedMigrations =
            await verificationDbContext.Database
                .GetAppliedMigrationsAsync();

        Assert.Contains(
            "20260917120047_InitialAuthSchema",
            appliedMigrations);

        Assert.True(
            await verificationDbContext.Database
                .CanConnectAsync());

        var pendingMigrations =
            await verificationDbContext.Database
                .GetPendingMigrationsAsync();

        Assert.Empty(
            pendingMigrations);
    }
}


