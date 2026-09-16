using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Email;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.IntegrationTests.Facade;

public sealed class PGLNAuthFacadeDependencyInjectionTests
{
    [Fact]
    public async Task AddPGLNAuth_WithEmailSender_HasValidDependencyGraph()
    {
        // Arrange
        var builder =
            WebApplication.CreateBuilder();

        AddRequiredConfiguration(
            builder.Configuration);

        builder.Services.AddPGLNAuth(
            builder.Configuration,
            options =>
                options.UseSqlite(
                    "Data Source=:memory:"));

        builder.Services
            .AddPGLNAuthEmail<TestEmailSender>();

        var app =
            builder.Build();

        try
        {
            // Act
            using var scope =
                app.Services.CreateScope();

            var emailSender =
                scope.ServiceProvider
                    .GetRequiredService<IEmailSender>();

            // Assert
            Assert.IsType<TestEmailSender>(
                emailSender);
        }
        finally
        {
            await app.DisposeAsync();
        }
    }

    [Fact]
    public async Task AddPGLNAuthPostgreSql_ConfiguresProviderAndMigrationsAssembly()
    {
        // Arrange
        var builder =
            WebApplication.CreateBuilder();

        AddRequiredConfiguration(
            builder.Configuration);

        builder.Services.AddPGLNAuthPostgreSql(
            builder.Configuration,
            "Host=localhost;Port=5432;Database=pgln_auth_tests;Username=postgres;Password=postgres");

        var app =
            builder.Build();

        try
        {
            // Act
            using var scope =
                app.Services.CreateScope();

            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AuthDbContext>();

            var migrationsAssembly =
                dbContext
                    .GetService<IMigrationsAssembly>();

            // Assert
            Assert.Equal(
                "Npgsql.EntityFrameworkCore.PostgreSQL",
                dbContext.Database.ProviderName);

            Assert.Equal(
                "PGLN.Auth.EntityFrameworkCore.PostgreSql",
                migrationsAssembly.Assembly.GetName().Name);
        }
        finally
        {
            await app.DisposeAsync();
        }
    }

    private static void AddRequiredConfiguration(
        ConfigurationManager configuration)
    {
        configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["PGLNAuth:Jwt:Issuer"] =
                    "PGLN.Auth.Tests",

                ["PGLNAuth:Jwt:Audience"] =
                    "PGLN.Auth.Tests",

                ["PGLNAuth:Jwt:SigningKey"] =
                    Convert.ToBase64String(
                        new byte[32])
            });
    }

    private sealed class TestEmailSender
        : IEmailSender
    {
        public Task SendAsync(
            EmailMessage message,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
