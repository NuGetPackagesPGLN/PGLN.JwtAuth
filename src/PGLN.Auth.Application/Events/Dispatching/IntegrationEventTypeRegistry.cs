using PGLN.Auth.Application.Events.Email;

namespace PGLN.Auth.Application.Events.Dispatching;

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
                typeof(EmailChangedNotificationRequested),
                typeof(WelcomeEmailRequested),
                typeof(PasswordResetRequested),
                typeof(PasswordChangedNotificationRequested),
                typeof(NewDeviceLoginNotificationRequested),
                typeof(AccountLockedNotificationRequested),
                typeof(StepUpVerificationCodeRequested)
            };

        _eventTypes =
            eventTypes.ToDictionary(
                GetEventTypeName,
                type => type,
                StringComparer.Ordinal);
    }

    public static string GetEventTypeName(
        Type eventType)
    {
        ArgumentNullException.ThrowIfNull(
            eventType);

        return eventType.FullName
            ?? throw new InvalidOperationException(
                $"Unable to resolve full name for integration event type '{eventType}'.");
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
