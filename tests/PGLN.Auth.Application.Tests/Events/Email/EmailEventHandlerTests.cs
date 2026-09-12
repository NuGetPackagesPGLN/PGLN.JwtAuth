using PGLN.Auth.Application.Abstractions.Email;
using PGLN.Auth.Application.Events.Email;
using PGLN.Auth.Application.Events.Email.Handlers;
using PGLN.Auth.Application.Tests.TestDoubles;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Tests.Events.Email;

public sealed class EmailEventHandlerTests
{
    [Fact]
    public async Task EmailConfirmationRequestedHandler_ShouldSendConfirmationEmail()
    {
        var sender =
            new FakeEmailSender();

        var renderer =
            new FakeEmailTemplateRenderer();

        var handler =
            new EmailConfirmationRequestedHandler(
                sender,
                renderer,
                new EmailDeliveryOptions
                {
                    ConfirmationBaseUrl =
                        "https://app.example.com/confirm-email"
                });

        var integrationEvent =
            new EmailConfirmationRequested(
                Guid.NewGuid(),
                UserId.New(),
                "user@example.com",
                "raw token/value",
                DateTimeOffset.UtcNow);

        await handler.HandleAsync(
            integrationEvent);

        var message =
            Assert.Single(
                sender.Messages);

        Assert.Equal(
            "user@example.com",
            message.To);

        Assert.NotNull(
            renderer.LastConfirmationUrl);

        Assert.Contains(
            "https://app.example.com/confirm-email?token=",
            renderer.LastConfirmationUrl,
            StringComparison.Ordinal);

        Assert.Contains(
            "raw%20token%2Fvalue",
            renderer.LastConfirmationUrl,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task WelcomeEmailRequestedHandler_ShouldSendWelcomeEmail()
    {
        var sender =
            new FakeEmailSender();

        var renderer =
            new FakeEmailTemplateRenderer();

        var handler =
            new WelcomeEmailRequestedHandler(
                sender,
                renderer);

        var integrationEvent =
            new WelcomeEmailRequested(
                Guid.NewGuid(),
                UserId.New(),
                "user@example.com",
                DateTimeOffset.UtcNow);

        await handler.HandleAsync(
            integrationEvent);

        var message =
            Assert.Single(
                sender.Messages);

        Assert.Equal(
            "user@example.com",
            message.To);

        Assert.Equal(
            "Welcome",
            message.Subject);
    }

    [Fact]
    public async Task PasswordChangedNotificationRequestedHandler_ShouldSendSecurityNotificationEmail()
    {
        var sender =
            new FakeEmailSender();

        var renderer =
            new FakeEmailTemplateRenderer();

        var handler =
            new PasswordChangedNotificationRequestedHandler(
                sender,
                renderer);

        var occurredAtUtc =
            new DateTimeOffset(
                2026,
                9,
                10,
                6,
                30,
                0,
                TimeSpan.Zero);

        var integrationEvent =
            new PasswordChangedNotificationRequested(
                Guid.NewGuid(),
                UserId.New(),
                "user@example.com",
                occurredAtUtc);

        await handler.HandleAsync(
            integrationEvent);

        var message =
            Assert.Single(
                sender.Messages);

        Assert.Equal(
            "user@example.com",
            message.To);

        Assert.Equal(
            "Your password was changed",
            message.Subject);
    }

    [Fact]
    public async Task NewDeviceLoginNotificationRequestedHandler_ShouldSendSecurityNotificationEmail()
    {
        var sender =
            new FakeEmailSender();

        var renderer =
            new FakeEmailTemplateRenderer();

        var handler =
            new NewDeviceLoginNotificationRequestedHandler(
                sender,
                renderer);

        var occurredAtUtc =
            new DateTimeOffset(
                2026,
                9,
                11,
                14,
                30,
                0,
                TimeSpan.Zero);

        var integrationEvent =
            new NewDeviceLoginNotificationRequested(
                Guid.NewGuid(),
                UserId.New(),
                "user@example.com",
                "device-hash-001",
                "Chrome on Windows",
                "192.168.1.25",
                "Mozilla/5.0",
                occurredAtUtc);

        await handler.HandleAsync(
            integrationEvent);

        var message =
            Assert.Single(
                sender.Messages);

        Assert.Equal(
            "user@example.com",
            message.To);

        Assert.Equal(
            "New device login detected",
            message.Subject);

        Assert.Contains(
            "Chrome on Windows",
            message.TextBody,
            StringComparison.Ordinal);

        Assert.Contains(
            "192.168.1.25",
            message.TextBody,
            StringComparison.Ordinal);

        Assert.Contains(
            "Mozilla/5.0",
            message.TextBody,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task AccountLockedNotificationRequestedHandler_ShouldSendSecurityNotificationEmail()
    {
        var sender =
            new FakeEmailSender();

        var renderer =
            new FakeEmailTemplateRenderer();

        var handler =
            new AccountLockedNotificationRequestedHandler(
                sender,
                renderer);

        var occurredAtUtc =
            new DateTimeOffset(
                2026,
                9,
                11,
                15,
                0,
                0,
                TimeSpan.Zero);

        var lockedUntilUtc =
            occurredAtUtc.AddMinutes(10);

        var integrationEvent =
            new AccountLockedNotificationRequested(
                Guid.NewGuid(),
                UserId.New(),
                "user@example.com",
                lockedUntilUtc,
                occurredAtUtc);

        await handler.HandleAsync(
            integrationEvent);

        var message =
            Assert.Single(
                sender.Messages);

        Assert.Equal(
            "user@example.com",
            message.To);

        Assert.Equal(
            "Your account has been temporarily locked",
            message.Subject);

        Assert.Contains(
            occurredAtUtc.ToString("O"),
            message.TextBody,
            StringComparison.Ordinal);

        Assert.Contains(
            lockedUntilUtc.ToString("O"),
            message.TextBody,
            StringComparison.Ordinal);
    }
}




