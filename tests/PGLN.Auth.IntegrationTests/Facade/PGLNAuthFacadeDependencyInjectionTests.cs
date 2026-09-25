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

    [Fact]
    public async Task AddPGLNAuth_WithCustomOptions_RegistersConfiguredApplicationOptions()
    {
        // Arrange
        var builder =
            WebApplication.CreateBuilder();

        AddRequiredConfiguration(
            builder.Configuration);

        var expectedResetUrl =
            "https://example.com/reset-password";

        builder.Services.AddPGLNAuth(
            builder.Configuration,
            options =>
                options.UseSqlite(
                    "Data Source=:memory:"),
            auth =>
            {
                auth.PasswordPolicy =
                    new PGLN.Auth.Application.Abstractions.Authentication.PasswordPolicyOptions
                    {
                        MinimumLength = 16
                    };

                auth.RefreshTokens =
                    new PGLN.Auth.Application.Abstractions.Authentication.RefreshTokenOptions
                    {
                        TokenLifetime =
                            TimeSpan.FromDays(7)
                    };

                auth.EmailDelivery =
                    new PGLN.Auth.Application.Abstractions.Email.EmailDeliveryOptions
                    {
                        ResetPasswordBaseUrl =
                            expectedResetUrl
                    };

                auth.StepUp =
                    new PGLN.Auth.Application.Configuration.StepUpChallengeOptions
                    {
                        Lifetime =
                            TimeSpan.FromMinutes(5)
                    };

                auth.PasswordReset =
                    new PGLN.Auth.Application.Features.ForgotPassword.PasswordResetOptions
                    {
                        TokenLifetime =
                            TimeSpan.FromMinutes(20)
                    };
            });

        var app =
            builder.Build();

        try
        {
            // Act
            var passwordPolicy =
                app.Services.GetRequiredService<
                    PGLN.Auth.Application.Abstractions.Authentication.PasswordPolicyOptions>();

            var refreshTokens =
                app.Services.GetRequiredService<
                    PGLN.Auth.Application.Abstractions.Authentication.RefreshTokenOptions>();

            var emailDelivery =
                app.Services.GetRequiredService<
                    PGLN.Auth.Application.Abstractions.Email.EmailDeliveryOptions>();

            var stepUp =
                app.Services.GetRequiredService<
                    PGLN.Auth.Application.Configuration.StepUpChallengeOptions>();

            var passwordReset =
                app.Services.GetRequiredService<
                    PGLN.Auth.Application.Features.ForgotPassword.PasswordResetOptions>();

            // Assert
            Assert.Equal(
                16,
                passwordPolicy.MinimumLength);

            Assert.Equal(
                TimeSpan.FromDays(7),
                refreshTokens.TokenLifetime);

            Assert.Equal(
                expectedResetUrl,
                emailDelivery.ResetPasswordBaseUrl);

            Assert.Equal(
                TimeSpan.FromMinutes(5),
                stepUp.Lifetime);

            Assert.Equal(
                TimeSpan.FromMinutes(20),
                passwordReset.TokenLifetime);
        }
        finally
        {
            await app.DisposeAsync();
        }
    }
    [Fact]
    public async Task AddPGLNAuth_WithoutCustomOptions_RegistersDefaultApplicationOptions()
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

        var app =
            builder.Build();

        try
        {
            // Act
            var passwordPolicy =
                app.Services.GetRequiredService<
                    PGLN.Auth.Application.Abstractions.Authentication.PasswordPolicyOptions>();

            var emailVerification =
                app.Services.GetRequiredService<
                    PGLN.Auth.Application.Abstractions.Authentication.EmailVerificationOptions>();

            var emailDelivery =
                app.Services.GetRequiredService<
                    PGLN.Auth.Application.Abstractions.Email.EmailDeliveryOptions>();

            var refreshTokens =
                app.Services.GetRequiredService<
                    PGLN.Auth.Application.Abstractions.Authentication.RefreshTokenOptions>();

            var accountLockout =
                app.Services.GetRequiredService<
                    PGLN.Auth.Application.Features.Login.AccountLockoutOptions>();

            var loginThrottle =
                app.Services.GetRequiredService<
                    PGLN.Auth.Application.Features.Login.LoginEmailThrottleOptions>();

            var stepUp =
                app.Services.GetRequiredService<
                    PGLN.Auth.Application.Configuration.StepUpChallengeOptions>();

            var passwordReset =
                app.Services.GetRequiredService<
                    PGLN.Auth.Application.Features.ForgotPassword.PasswordResetOptions>();

            // Assert
            Assert.Equal(
                12,
                passwordPolicy.MinimumLength);

            Assert.Equal(
                128,
                passwordPolicy.MaximumLength);

            Assert.False(
                passwordPolicy.RequireUppercase);

            Assert.False(
                passwordPolicy.RequireLowercase);

            Assert.False(
                passwordPolicy.RequireDigit);

            Assert.False(
                passwordPolicy.RequireNonAlphanumeric);

            Assert.Equal(
                TimeSpan.FromHours(24),
                emailVerification.TokenLifetime);

            Assert.Equal(
                "https://localhost/confirm-email",
                emailDelivery.ConfirmationBaseUrl);

            Assert.Equal(
                "https://localhost/confirm-email-change",
                emailDelivery.EmailChangeConfirmationBaseUrl);

            Assert.Equal(
                "https://localhost/reset-password",
                emailDelivery.ResetPasswordBaseUrl);

            Assert.Equal(
                TimeSpan.FromDays(30),
                refreshTokens.TokenLifetime);

            Assert.Equal(
                5,
                accountLockout.MaxFailedAttempts);

            Assert.Equal(
                TimeSpan.FromMinutes(15),
                accountLockout.FailureWindow);

            Assert.Equal(
                TimeSpan.FromMinutes(15),
                accountLockout.LockoutDuration);

            Assert.Equal(
                10,
                loginThrottle.MaxFailedAttempts);

            Assert.Equal(
                TimeSpan.FromMinutes(5),
                loginThrottle.Window);

            Assert.Equal(
                TimeSpan.FromMinutes(10),
                stepUp.Lifetime);

            Assert.Equal(
                5,
                stepUp.MaxFailedAttempts);

            Assert.Equal(
                TimeSpan.FromMinutes(30),
                passwordReset.TokenLifetime);
        }
        finally
        {
            await app.DisposeAsync();
        }
    }
    [Fact]
    public void AddPGLNAuth_WithInvalidCustomOptions_Throws()
    {
        // Arrange
        var builder =
            WebApplication.CreateBuilder();

        AddRequiredConfiguration(
            builder.Configuration);

        // Act
        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    builder.Services.AddPGLNAuth(
                        builder.Configuration,
                        options =>
                            options.UseSqlite(
                                "Data Source=:memory:"),
                        auth =>
                        {
                            auth.PasswordPolicy =
                                new PGLN.Auth.Application.Abstractions.Authentication.PasswordPolicyOptions
                                {
                                    MinimumLength = 0
                                };
                        }));

        // Assert
        Assert.Equal(
            "Password minimum length must be greater than zero.",
            exception.Message);
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
