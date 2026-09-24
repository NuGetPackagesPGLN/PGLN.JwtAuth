using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Application.Abstractions.Time;
using PGLN.Auth.Application.Common;
using PGLN.Auth.Application.Configuration;
using PGLN.Auth.Application.Features.Login;
using PGLN.Auth.Application.Features.Registration;
using PGLN.Auth.Application.Messaging;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.Domain.VerificationTokens;

namespace PGLN.Auth.Application.Tests.DependencyInjection;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddPGLNAuthApplication_ShouldRegisterPasswordPolicy()
    {
        var services =
            new ServiceCollection();

        var passwordPolicy =
            new PasswordPolicyOptions
            {
                MinimumLength = 16,
                MaximumLength = 128,
                RequireUppercase = true,
                RequireLowercase = true,
                RequireDigit = true,
                RequireNonAlphanumeric = true
            };

        services.AddPGLNAuthApplication(
            passwordPolicy);

        using var provider =
            services.BuildServiceProvider();

        var resolved =
            provider.GetRequiredService<PasswordPolicyOptions>();

        Assert.Same(
            passwordPolicy,
            resolved);

        Assert.Equal(
            16,
            resolved.MinimumLength);

        Assert.True(
            resolved.RequireUppercase);
    }

    [Fact]
    public void AddPGLNAuthApplication_ShouldRegisterEmailVerificationOptions()
    {
        var services =
            new ServiceCollection();

        var emailVerificationOptions =
            new EmailVerificationOptions
            {
                TokenLifetime =
                    TimeSpan.FromMinutes(30)
            };

        services.AddPGLNAuthApplication(
            emailVerificationOptions:
                emailVerificationOptions);

        using var provider =
            services.BuildServiceProvider();

        var resolved =
            provider.GetRequiredService<EmailVerificationOptions>();

        Assert.Same(
            emailVerificationOptions,
            resolved);

        Assert.Equal(
            TimeSpan.FromMinutes(30),
            resolved.TokenLifetime);
    }

    [Fact]
    public void AddPGLNAuthApplication_ShouldRegisterRegistrationValidator()
    {
        var services =
            new ServiceCollection();

        services.AddPGLNAuthApplication();

        using var provider =
            services.BuildServiceProvider();

        var validators =
            provider
                .GetServices<IValidator<RegisterCommand>>()
                .ToArray();

        Assert.Contains(
            validators,
            validator =>
                validator is RegisterCommandValidator);
    }

    [Fact]
    public void AddPGLNAuthApplication_ShouldRegisterRegisterCommandHandler()
    {
        var services =
            new ServiceCollection();

        services.AddPGLNAuthApplication();

        AddRegistrationDependencies(
            services);

        using var provider =
            services.BuildServiceProvider();

        using var scope =
            provider.CreateScope();

        var handler =
            scope.ServiceProvider
                .GetRequiredService<
                    ICommandHandler<
                        RegisterCommand,
                        Result<RegisterResult>>>();

        Assert.IsType<
            ValidationCommandHandlerDecorator<
                RegisterCommand,
                Result<RegisterResult>>>(
            handler);
    }

    [Fact]
    public async Task ResolvedRegisterHandler_WithInvalidCommand_ShouldRunValidation()
    {
        var services =
            new ServiceCollection();

        services.AddPGLNAuthApplication(
            new PasswordPolicyOptions
            {
                MinimumLength = 12,
                MaximumLength = 128
            });

        AddRegistrationDependencies(
            services);

        using var provider =
            services.BuildServiceProvider();

        using var scope =
            provider.CreateScope();

        var handler =
            scope.ServiceProvider
                .GetRequiredService<
                    ICommandHandler<
                        RegisterCommand,
                        Result<RegisterResult>>>();

        var command =
            new RegisterCommand(
                "not-an-email",
                "short");

        await Assert.ThrowsAsync<
            PGLN.Auth.Application.Common.Validation.CommandValidationException>(
            () =>
                handler.HandleAsync(command));
    }

    [Fact]
    public void AddPGLNAuthApplication_WithInvalidStepUpLifetime_ShouldThrow()
    {
        var services =
            new ServiceCollection();

        var stepUpOptions =
            new StepUpChallengeOptions
            {
                Lifetime = TimeSpan.Zero
            };

        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    services.AddPGLNAuthApplication(
                        stepUpChallengeOptions:
                            stepUpOptions));

        Assert.Equal(
            "Step-up challenge lifetime must be greater than zero.",
            exception.Message);
    }

    [Fact]
    public void AddPGLNAuthApplication_WithInvalidStepUpMaxFailedAttempts_ShouldThrow()
    {
        var services =
            new ServiceCollection();

        var stepUpOptions =
            new StepUpChallengeOptions
            {
                MaxFailedAttempts = 0
            };

        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    services.AddPGLNAuthApplication(
                        stepUpChallengeOptions:
                            stepUpOptions));

        Assert.Equal(
            "Step-up maximum failed attempts must be greater than zero.",
            exception.Message);
    }
    [Fact]
    public void AddPGLNAuthApplication_ShouldRegisterInternalLoginCommandHandler()
    {
        var services =
            new ServiceCollection();

        services.AddPGLNAuthApplication();

        var serviceType =
            typeof(
                ICommandHandler<
                    LoginCommand,
                    Result<LoginResult>>);

        var descriptors =
            services
                .Where(
                    descriptor =>
                        descriptor.ServiceType ==
                        serviceType)
                .ToArray();

        Assert.NotEmpty(descriptors);
    }
    private static void AddRegistrationDependencies(
        IServiceCollection services)
    {
        services.AddScoped<
            IUserRepository,
            StubUserRepository>();

        services.AddScoped<
            IEmailVerificationTokenRepository,
            StubEmailVerificationTokenRepository>();

        services.AddScoped<
            IUnitOfWork,
            StubUnitOfWork>();

        services.AddScoped<
            IPasswordHasher,
            StubPasswordHasher>();

        services.AddScoped<
            IVerificationTokenGenerator,
            StubVerificationTokenGenerator>();

        services.AddScoped<
            ITokenHasher,
            StubTokenHasher>();

        services.AddScoped<
            IClock,
            StubClock>();

        services.AddScoped<
            PGLN.Auth.Application.Abstractions.Events.IIntegrationEventPublisher,
            StubIntegrationEventPublisher>();
    }

    private sealed class StubUserRepository
        : IUserRepository
    {
        public Task<User?> GetByIdAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<User?>(null);
        }

        public Task<User?> GetByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<User?>(null);
        }

        public Task<bool> ExistsByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task AddAsync(
            User user,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class StubEmailVerificationTokenRepository
        : IEmailVerificationTokenRepository
    {
        public Task<EmailVerificationToken?> GetByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<EmailVerificationToken?>(null);
        }

        public Task<IReadOnlyCollection<EmailVerificationToken>>
            GetActiveByUserIdAsync(
                UserId userId,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<EmailVerificationToken> result =
                Array.Empty<EmailVerificationToken>();

            return Task.FromResult(result);
        }

        public Task AddAsync(
            EmailVerificationToken token,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class StubUnitOfWork
        : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(1);
        }
    }

    private sealed class StubPasswordHasher
        : IPasswordHasher
    {
        public string Hash(string password)
        {
            return "hashed";
        }

        public bool Verify(
            string password,
            string passwordHash)
        {
            return true;
        }
    }

    private sealed class StubVerificationTokenGenerator
        : IVerificationTokenGenerator
    {
        public string Generate()
        {
            return "raw-verification-token";
        }
    }

    private sealed class StubTokenHasher
        : ITokenHasher
    {
        public string Hash(string token)
        {
            return $"hashed::{token}";
        }
    }

    private sealed class StubClock
        : IClock
    {
        public DateTimeOffset UtcNow =>
            new(
                2026,
                9,
                6,
                8,
                0,
                0,
                TimeSpan.Zero);
    }
}
