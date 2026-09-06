using System.Text.Json;
using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.EntityFrameworkCore.Outbox;

public sealed class OutboxIntegrationEventPublisher
    : IIntegrationEventPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly AuthDbContext _dbContext;
    private readonly IIntegrationEventPayloadProtector _payloadProtector;

    public OutboxIntegrationEventPublisher(
        AuthDbContext dbContext,
        IIntegrationEventPayloadProtector payloadProtector)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(payloadProtector);

        _dbContext = dbContext;
        _payloadProtector = payloadProtector;
    }

    public async Task PublishAsync<TEvent>(
        TEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var eventType =
            typeof(TEvent).FullName
            ?? throw new InvalidOperationException(
                $"Unable to resolve event type for {typeof(TEvent)}.");

        var serialized =
            JsonSerializer.Serialize(
                integrationEvent,
                SerializerOptions);

        var protectedPayload =
            _payloadProtector.Protect(
                serialized);

        var message =
            OutboxMessage.Create(
                integrationEvent.EventId,
                eventType,
                protectedPayload,
                integrationEvent.OccurredAtUtc);

        await _dbContext
            .OutboxMessages
            .AddAsync(
                message,
                cancellationToken);
    }
}
