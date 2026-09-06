using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace PGLN.Auth.AspNetCore.Outbox;

public static class OutboxServiceCollectionExtensions
{
    public static IServiceCollection AddPGLNAuthOutboxBackgroundWorker(
        this IServiceCollection services,
        Action<OutboxBackgroundWorkerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options =
            new OutboxBackgroundWorkerOptions();

        if (configure is not null)
        {
            var configured =
                new MutableOutboxBackgroundWorkerOptions();

            configure(
                configured.Options);

            options =
                configured.Options;
        }

        options.Validate();

        services.AddSingleton(
            options);

        services.TryAddSingleton<
            IOutboxWorkerIdProvider,
            OutboxWorkerIdProvider>();

        services.AddHostedService<
            OutboxBackgroundService>();

        return services;
    }

    private sealed class MutableOutboxBackgroundWorkerOptions
    {
        public OutboxBackgroundWorkerOptions Options { get; } =
            new();
    }
}
