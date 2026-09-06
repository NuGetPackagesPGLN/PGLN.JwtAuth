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
}
