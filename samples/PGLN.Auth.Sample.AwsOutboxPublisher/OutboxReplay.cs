using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Application.Events.Dispatching;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.Sample.AwsOutboxPublisher;

internal static class OutboxReplay
{
    private static readonly Guid ExpectedMessageId =
        Guid.Parse(
            "2c934369-debc-4044-a9c4-b9e9aad7c7b7");

    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    public static async Task ReplayAsync(
        IServiceProvider serviceProvider,
        Guid messageId)
    {
        if (messageId != ExpectedMessageId)
        {
            throw new InvalidOperationException(
                "Only the designated E2E event may be replayed.");
        }

        await using var scope =
            serviceProvider.CreateAsyncScope();

        var services = scope.ServiceProvider;

        var dbContext =
            services.GetRequiredService<AuthDbContext>();

        var protector =
            services.GetRequiredService<
                IIntegrationEventPayloadProtector>();

        var registry =
            services.GetRequiredService<
                IntegrationEventTypeRegistry>();

        var dispatcher =
            services.GetRequiredService<
                IIntegrationEventDispatcher>();

        var message =
            await dbContext.OutboxMessages
                .AsNoTracking()
                .SingleAsync(
                    item => item.Id == messageId);

        if (!message.IsProcessed)
        {
            throw new InvalidOperationException(
                "Only previously processed events may be replayed.");
        }

        var alreadyProcessed =
            await dbContext.InboxMessages
                .AsNoTracking()
                .AnyAsync(
                    item =>
                        item.Id == messageId &&
                        item.ProcessedAtUtc != null);

        if (!alreadyProcessed)
        {
            throw new InvalidOperationException(
                "Replay requires an already-processed inbox record.");
        }

        var plaintext =
            protector.Unprotect(
                message.Payload);

        var eventType =
            registry.GetEventType(
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
                "Unable to deserialize the replay event.");
        }

        var context =
            new IntegrationEventDispatchContext(
                message.Id,
                message.OccurredAtUtc);

        await dispatcher.DispatchAsync(
            integrationEvent,
            context);

        Console.WriteLine(
            $"Replayed durable event {message.Id}.");
    }
}

