using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PGLN.Auth.EntityFrameworkCore.Outbox;

namespace PGLN.Auth.AspNetCore.Outbox;

internal sealed class OutboxBackgroundService
    : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOutboxWorkerIdProvider _workerIdProvider;
    private readonly OutboxBackgroundWorkerOptions _options;
    private readonly ILogger<OutboxBackgroundService> _logger;

    public OutboxBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOutboxWorkerIdProvider workerIdProvider,
        OutboxBackgroundWorkerOptions options,
        ILogger<OutboxBackgroundService> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(workerIdProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        options.Validate();

        _scopeFactory = scopeFactory;
        _workerIdProvider = workerIdProvider;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "PGLN.Auth outbox background worker is disabled.");

            return;
        }

        _logger.LogInformation(
            "PGLN.Auth outbox worker {WorkerId} started.",
            _workerIdProvider.WorkerId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOnceAsync(
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unhandled exception while processing the PGLN.Auth outbox.");
            }

            try
            {
                await Task.Delay(
                    _options.PollInterval,
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation(
            "PGLN.Auth outbox worker {WorkerId} stopped.",
            _workerIdProvider.WorkerId);
    }

    internal async Task<int> ProcessOnceAsync(
        CancellationToken cancellationToken = default)
    {
        await using var scope =
            _scopeFactory.CreateAsyncScope();

        var processor =
            scope.ServiceProvider
                .GetRequiredService<IOutboxProcessor>();

        var processed =
            await processor.ProcessAsync(
                _workerIdProvider.WorkerId,
                cancellationToken);

        if (processed > 0)
        {
            _logger.LogInformation(
                "PGLN.Auth outbox worker {WorkerId} processed {MessageCount} message(s).",
                _workerIdProvider.WorkerId,
                processed);
        }

        return processed;
    }
}
