using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Application.Events.Email;

namespace PGLN.Auth.EntityFrameworkCore.Outbox;

public sealed class IntegrationEventTypeRegistry
{
    private readonly IReadOnlyDictionary<string, Type> _eventTypes;

    public IntegrationEventTypeRegistry()
    {
        var eventTypes =
            new[]
            {
                typeof(EmailConfirmationRequested),
                typeof(EmailChangeConfirmationRequested),
                typeof(WelcomeEmailRequested),
                typeof(PasswordResetRequested),
                typeof(PasswordChangedNotificationRequested),
                typeof(NewDeviceLoginNotificationRequested),
                typeof(AccountLockedNotificationRequested)
            };

        _eventTypes =
            eventTypes.ToDictionary(
                type =>
                    type.FullName
                    ?? throw new InvalidOperationException(
                        $"Unable to resolve full name for integration event type '{type}'."),
                type => type,
                StringComparer.Ordinal);
    }

    public Type GetEventType(
        string eventTypeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            eventTypeName);

        if (!_eventTypes.TryGetValue(
            eventTypeName,
            out var eventType))
        {
            throw new InvalidOperationException(
                $"Unknown integration event type '{eventTypeName}'.");
        }

        return eventType;
    }
}
