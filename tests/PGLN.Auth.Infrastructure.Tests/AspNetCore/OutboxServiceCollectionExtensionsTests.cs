using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PGLN.Auth.AspNetCore.Outbox;

namespace PGLN.Auth.Infrastructure.Tests.AspNetCore;

public sealed class OutboxServiceCollectionExtensionsTests
{
    [Fact]
    public void AddPGLNAuthOutboxBackgroundWorker_ShouldRegisterRequiredServices()
    {
        var services =
            new ServiceCollection();

        services.AddLogging();

        services.AddPGLNAuthOutboxBackgroundWorker();

        using var serviceProvider =
            services.BuildServiceProvider();

        var options =
            serviceProvider.GetService<
                OutboxBackgroundWorkerOptions>();

        var workerIdProvider =
            serviceProvider.GetService<
                IOutboxWorkerIdProvider>();

        var hostedServices =
            serviceProvider.GetServices<
                IHostedService>();

        Assert.NotNull(options);
        Assert.NotNull(workerIdProvider);

        Assert.Contains(
            hostedServices,
            service =>
                service is OutboxBackgroundService);
    }

    [Fact]
    public void AddPGLNAuthOutboxBackgroundWorker_ShouldApplyConfiguredOptions()
    {
        var services =
            new ServiceCollection();

        services.AddPGLNAuthOutboxBackgroundWorker(
            options =>
            {
                options.Enabled = false;

                options.PollInterval =
                    TimeSpan.FromSeconds(10);

                options.WorkerIdPrefix =
                    "test-worker";
            });

        using var serviceProvider =
            services.BuildServiceProvider();

        var options =
            serviceProvider.GetRequiredService<
                OutboxBackgroundWorkerOptions>();

        Assert.False(
            options.Enabled);

        Assert.Equal(
            TimeSpan.FromSeconds(10),
            options.PollInterval);

        Assert.Equal(
            "test-worker",
            options.WorkerIdPrefix);
    }
}
