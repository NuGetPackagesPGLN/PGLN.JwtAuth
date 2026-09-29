using PGLN.Auth.Application.Events.Dispatching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Application.Abstractions.Outbox;
using PGLN.Auth.Application.Abstractions.Inbox;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Domain.ExternalLogins;
using PGLN.Auth.EntityFrameworkCore.Outbox;
using PGLN.Auth.EntityFrameworkCore.Inbox;
using PGLN.Auth.EntityFrameworkCore.Persistence;
using PGLN.Auth.EntityFrameworkCore.Persistence.Repositories;

namespace PGLN.Auth.EntityFrameworkCore;

public static class DependencyInjection
{
    public static IServiceCollection AddPGLNAuthEntityFrameworkCore(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureOptions,
        OutboxProcessingOptions? outboxOptions = null)
    {
        ArgumentNullException.ThrowIfNull(
            services);

        ArgumentNullException.ThrowIfNull(
            configureOptions);

        outboxOptions ??=
            new OutboxProcessingOptions();

        outboxOptions.Validate();

        services.AddSingleton(
            outboxOptions);

        services.AddDbContext<AuthDbContext>(
            configureOptions);

        services.AddScoped<
            IUserRepository,
            UserRepository>();

        services.AddScoped<
            IEmailVerificationTokenRepository,
            EmailVerificationTokenRepository>();

        services.AddScoped<
            IEmailChangeTokenRepository,
            EmailChangeTokenRepository>();

        services.AddScoped<
            IPasswordResetTokenRepository,
            PasswordResetTokenRepository>();

        services.AddScoped<
            IRefreshTokenRepository,
            RefreshTokenRepository>();

        services.AddScoped<
            IAuthSessionRepository,
            AuthSessionRepository>();

        services.AddScoped<
            ITrustedDeviceRepository,
            TrustedDeviceRepository>();

        services.AddScoped<
            IExternalLoginRepository,
            ExternalLoginRepository>();

        services.AddScoped<
            IStepUpChallengeRepository,
            StepUpChallengeRepository>();

        services.AddScoped<
            ILoginAttemptRepository,
            LoginAttemptRepository>();

        services.AddScoped<
            IIntegrationEventPublisher,
            OutboxIntegrationEventPublisher>();

        services.AddScoped<
            IIntegrationEventDispatcher,
            IntegrationEventDispatcher>();

        services.AddScoped<
            IOutboxProcessor,
            OutboxProcessor>();

        services.AddScoped<
            IIntegrationEventInbox,
            EfIntegrationEventInbox>();

        services.AddSingleton<
            IntegrationEventTypeRegistry>();

        services.AddScoped<
            IUnitOfWork,
            UnitOfWork>();

        return services;
    }

    public static IServiceCollection AddPGLNAuthIntegrationEventInbox(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(
            services);

        ArgumentNullException.ThrowIfNull(
            configureOptions);

        services.AddDbContext<AuthDbContext>(
            configureOptions);

        services.AddScoped<
            IIntegrationEventInbox,
            EfIntegrationEventInbox>();

        return services;
    }


}
