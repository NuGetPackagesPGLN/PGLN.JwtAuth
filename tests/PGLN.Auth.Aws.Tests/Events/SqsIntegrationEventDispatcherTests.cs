using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Application.Events.Dispatching;
using PGLN.Auth.Application.Events.Email;
using PGLN.Auth.Aws.Events;

namespace PGLN.Auth.Aws.Tests.Events;

public sealed class SqsIntegrationEventDispatcherTests
{
    [Fact]
    public void CreateEnvelope_Should_Preserve_Dispatch_Context()
    {
        // Arrange
        var messageId =
            Guid.Parse(
                "6a7c6ad0-62fb-46c8-9d22-91fdf35e0ab4");

        var occurredAtUtc =
            new DateTimeOffset(
                2026,
                9,
                28,
                10,
                30,
                0,
                TimeSpan.Zero);

        var context =
            new IntegrationEventDispatchContext(
                MessageId: messageId,
                OccurredAtUtc: occurredAtUtc);

        var integrationEvent =
            CreateEmailConfirmationRequested();

        // Act
        var envelope =
            SqsIntegrationEventDispatcher
                .CreateEnvelope(
                    integrationEvent,
                    context);

        // Assert
        Assert.Equal(
            messageId,
            envelope.MessageId);

        Assert.Equal(
            occurredAtUtc,
            envelope.OccurredAtUtc);

        Assert.Equal(
            IntegrationEventTypeRegistry
                .GetEventTypeName(
                    typeof(EmailConfirmationRequested)),
            envelope.EventType);

        Assert.False(
            string.IsNullOrWhiteSpace(
                envelope.Payload));
    }

    private static EmailConfirmationRequested
        CreateEmailConfirmationRequested()
    {
        var eventType =
            typeof(EmailConfirmationRequested);

        var constructors =
            eventType.GetConstructors();

        Assert.NotEmpty(
            constructors);

        var constructor =
            constructors[0];

        var arguments =
            constructor
                .GetParameters()
                .Select(CreateArgument)
                .ToArray();

        return Assert.IsType<EmailConfirmationRequested>(
            constructor.Invoke(arguments));
    }

    private static object? CreateArgument(
        System.Reflection.ParameterInfo parameter)
    {
        var type =
            parameter.ParameterType;

        if (type == typeof(Guid))
        {
            return Guid.Parse(
                "45a7cb1a-d035-4824-a3ea-f286805de51e");
        }

        if (type == typeof(string))
        {
            return parameter.Name?.Contains(
                "email",
                StringComparison.OrdinalIgnoreCase) == true
                    ? "test@example.com"
                    : "test-value";
        }

        if (type == typeof(DateTimeOffset))
        {
            return new DateTimeOffset(
                2026,
                9,
                28,
                10,
                0,
                0,
                TimeSpan.Zero);
        }

        if (type == typeof(DateTime))
        {
            return new DateTime(
                2026,
                9,
                28,
                10,
                0,
                0,
                DateTimeKind.Utc);
        }

        if (type == typeof(bool))
        {
            return false;
        }

        if (type.IsValueType)
        {
            return Activator.CreateInstance(
                type);
        }

        return null;
    }
}
