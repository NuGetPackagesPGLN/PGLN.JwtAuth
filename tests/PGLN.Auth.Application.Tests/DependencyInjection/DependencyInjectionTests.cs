using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Common;
using PGLN.Auth.Application.Features.Registration;

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

        services.AddScoped<
            PGLN.Auth.Application.Abstractions.Persistence.IUserRepository,
            StubUserRepository>();

        services.AddScoped<
            PGLN.Auth.Application.Abstractions.Persistence.IUnitOfWork,
            StubUnitOfWork>();

        services.AddScoped<
            IPasswordHasher,
            StubPasswordHasher>();

        services.AddScoped<
            PGLN.Auth.Application.Abstractions.Time.IClock,
            StubClock>();

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

        services.AddScoped<
            PGLN.Auth.Application.Abstractions.Persistence.IUserRepository,
            StubUserRepository>();

        services.AddScoped<
            PGLN.Auth.Application.Abstractions.Persistence.IUnitOfWork,
            StubUnitOfWork>();

        services.AddScoped<
            IPasswordHasher,
            StubPasswordHasher>();

        services.AddScoped<
            PGLN.Auth.Application.Abstractions.Time.IClock,
            StubClock>();

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

    private sealed class StubUserRepository
        : PGLN.Auth.Application.Abstractions.Persistence.IUserRepository
    {
        public Task<PGLN.Auth.Domain.Users.User?> GetByIdAsync(
            PGLN.Auth.Domain.Users.UserId userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<
                PGLN.Auth.Domain.Users.User?>(null);
        }

        public Task<PGLN.Auth.Domain.Users.User?> GetByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<
                PGLN.Auth.Domain.Users.User?>(null);
        }

        public Task<bool> ExistsByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task AddAsync(
            PGLN.Auth.Domain.Users.User user,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class StubUnitOfWork
        : PGLN.Auth.Application.Abstractions.Persistence.IUnitOfWork
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

    private sealed class StubClock
        : PGLN.Auth.Application.Abstractions.Time.IClock
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
