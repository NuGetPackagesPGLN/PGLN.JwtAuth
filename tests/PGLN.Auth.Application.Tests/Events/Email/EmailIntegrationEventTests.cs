using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Application.Events.Email;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Tests.Events.Email;

public sealed class EmailIntegrationEventTests
{
    [Fact]
    public void EmailConfirmationRequested_ShouldImplementIntegrationEvent()
    {
        var integrationEvent =
            new EmailConfirmationRequested(
                Guid.NewGuid(),
                UserId.New(),
                "user@example.com",
                "raw-verification-token",
                DateTimeOffset.UtcNow);

        Assert.IsAssignableFrom<IIntegrationEvent>(
            integrationEvent);
    }

    [Fact]
    public void EmailConfirmationRequested_ShouldPreserveVerificationToken()
    {
        var occurredAt =
            new DateTimeOffset(
                2026,
                9,
                6,
                14,
                0,
                0,
                TimeSpan.Zero);

        var integrationEvent =
            new EmailConfirmationRequested(
                Guid.NewGuid(),
                UserId.New(),
                "user@example.com",
                "raw-verification-token",
                occurredAt);

        Assert.Equal(
            "raw-verification-token",
            integrationEvent.VerificationToken);

        Assert.Equal(
            occurredAt,
            integrationEvent.OccurredAtUtc);
    }

    [Fact]
    public void WelcomeEmailRequested_ShouldImplementIntegrationEvent()
    {
        var integrationEvent =
            new WelcomeEmailRequested(
                Guid.NewGuid(),
                UserId.New(),
                "user@example.com",
                DateTimeOffset.UtcNow);

        Assert.IsAssignableFrom<IIntegrationEvent>(
            integrationEvent);
    }

    [Fact]
    public void IntegrationEvents_ShouldHaveUniqueEventIdentifiers()
    {
        var first =
            new WelcomeEmailRequested(
                Guid.NewGuid(),
                UserId.New(),
                "user@example.com",
                DateTimeOffset.UtcNow);

        var second =
            new WelcomeEmailRequested(
                Guid.NewGuid(),
                UserId.New(),
                "user@example.com",
                DateTimeOffset.UtcNow);

        Assert.NotEqual(
            first.EventId,
            second.EventId);
    }
}
