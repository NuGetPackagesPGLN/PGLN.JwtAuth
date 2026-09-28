using PGLN.Auth.Application.Events.Dispatching;
using PGLN.Auth.Application.Events.Email;

namespace PGLN.Auth.Aws.Tests.Events;

public sealed class IntegrationEventTypeRegistryTests
{
    private readonly IntegrationEventTypeRegistry _registry =
        new();

    [Fact]
    public void GetEventType_KnownEvent_ShouldReturnRegisteredType()
    {
        // Arrange
        var eventTypeName =
            typeof(EmailConfirmationRequested).FullName!;

        // Act
        var eventType =
            _registry.GetEventType(eventTypeName);

        // Assert
        Assert.Equal(
            typeof(EmailConfirmationRequested),
            eventType);
    }

    [Fact]
    public void GetEventType_UnknownEvent_ShouldThrowInvalidOperationException()
    {
        // Arrange
        const string eventTypeName =
            "Untrusted.Namespace.ArbitraryEvent";

        // Act
        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    _registry.GetEventType(
                        eventTypeName));

        // Assert
        Assert.Equal(
            $"Unknown integration event type '{eventTypeName}'.",
            exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetEventType_NullOrWhitespace_ShouldRejectInput(
        string? eventTypeName)
    {
        Assert.ThrowsAny<ArgumentException>(
            () =>
                _registry.GetEventType(
                    eventTypeName!));
    }
}
