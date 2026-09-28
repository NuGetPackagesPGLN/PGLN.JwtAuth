using PGLN.Auth.Application.Events.Dispatching;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Application.Abstractions.Outbox;
using PGLN.Auth.Application.Abstractions.Time;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.EntityFrameworkCore.Outbox;

public sealed class OutboxProcessor
    : IOutboxProcessor
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly AuthDbContext _dbContext;
    private readonly IIntegrationEventPayloadProtector _payloadProtector;
    private readonly IntegrationEventTypeRegistry _eventTypeRegistry;
    private readonly IIntegrationEventDispatcher _dispatcher;
    private readonly IClock _clock;
    private readonly OutboxProcessingOptions _options;

    public OutboxProcessor(
        AuthDbContext dbContext,
        IIntegrationEventPayloadProtector payloadProtector,
        IntegrationEventTypeRegistry eventTypeRegistry,
        IIntegrationEventDispatcher dispatcher,
        IClock clock,
        OutboxProcessingOptions options)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(payloadProtector);
        ArgumentNullException.ThrowIfNull(eventTypeRegistry);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(options);

        options.Validate();

        _dbContext = dbContext;
        _payloadProtector = payloadProtector;
        _eventTypeRegistry = eventTypeRegistry;
        _dispatcher = dispatcher;
        _clock = clock;
        _options = options;
    }

    public async Task<int> ProcessAsync(
        string workerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            workerId);

        var now =
            _clock.UtcNow;

        var candidates =
            await _dbContext
                .OutboxMessages
                .Where(
                    message =>
                        message.ProcessedAtUtc == null &&
                        message.DeadLetteredAtUtc == null &&
                        (
                            message.NextAttemptAtUtc == null ||
                            message.NextAttemptAtUtc <= now
                        ) &&
                        (
                            message.ClaimExpiresAtUtc == null ||
                            message.ClaimExpiresAtUtc <= now
                        ))
                .OrderBy(
                    message =>
                        message.OccurredAtUtc)
                .Take(
                    _options.BatchSize)
                .ToArrayAsync(
                    cancellationToken);

        foreach (var message in candidates)
        {
            if (!message.TryClaim(
                workerId,
                now,
                _options.ClaimDuration))
            {
                continue;
            }
        }

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        var claimedMessages =
            candidates
                .Where(
                    message =>
                        message.ClaimedBy == workerId &&
                        message.ClaimExpiresAtUtc > now)
                .ToArray();

        var processedCount =
            0;

        foreach (var message in claimedMessages)
        {
            try
            {
                var plaintext =
                    _payloadProtector.Unprotect(
                        message.Payload);

                var eventType =
                    _eventTypeRegistry.GetEventType(
                        message.Type);

                var integrationEvent =
                    JsonSerializer.Deserialize(
                        plaintext,
                        eventType,
                        SerializerOptions)
                    as IIntegrationEvent;

                if (integrationEvent is null)
                {
                    throw new InvalidOperationException(
                        $"Unable to deserialize outbox message '{message.Id}' as '{message.Type}'.");
                }

                await _dispatcher.DispatchAsync(
                    integrationEvent,
                    cancellationToken);

                message.MarkProcessed(
                    _clock.UtcNow);

                await _dbContext.SaveChangesAsync(
                    cancellationToken);

                processedCount++;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                var retryDelay =
                    CalculateRetryDelay(
                        message.AttemptCount + 1);

                message.RecordFailure(
                    exception.Message,
                    _clock.UtcNow,
                    _options.MaximumAttempts,
                    retryDelay);

                await _dbContext.SaveChangesAsync(
                    cancellationToken);
            }
        }

        return processedCount;
    }

    private TimeSpan CalculateRetryDelay(
        int attemptNumber)
    {
        var exponent =
            Math.Max(
                attemptNumber - 1,
                0);

        var multiplier =
            Math.Pow(
                2,
                exponent);

        var milliseconds =
            _options.InitialRetryDelay.TotalMilliseconds *
            multiplier;

        var cappedMilliseconds =
            Math.Min(
                milliseconds,
                _options.MaximumRetryDelay.TotalMilliseconds);

        return TimeSpan.FromMilliseconds(
            cappedMilliseconds);
    }
}
