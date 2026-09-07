using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.EntityFrameworkCore.Outbox;
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
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

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
            IIntegrationEventPublisher,
            OutboxIntegrationEventPublisher>();

        services.AddScoped<
            IIntegrationEventDispatcher,
            IntegrationEventDispatcher>();

        services.AddScoped<
            IOutboxProcessor,
            OutboxProcessor>();

        services.AddSingleton<
            IntegrationEventTypeRegistry>();

        services.AddScoped<
            IUnitOfWork,
            UnitOfWork>();

                services.AddScoped<
            IRefreshTokenRepository,
            RefreshTokenRepository>();

        services.AddScoped<
            ILoginAttemptRepository,
            LoginAttemptRepository>();
return services;
    }
}

