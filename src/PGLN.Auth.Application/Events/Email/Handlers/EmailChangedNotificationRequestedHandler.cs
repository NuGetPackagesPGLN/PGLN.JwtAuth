using PGLN.Auth.Application.Abstractions.Email;
using PGLN.Auth.Application.Abstractions.Events;

namespace PGLN.Auth.Application.Events.Email.Handlers;

public sealed class EmailChangedNotificationRequestedHandler
    : IIntegrationEventHandler<EmailChangedNotificationRequested>
{
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateRenderer _templateRenderer;

    public EmailChangedNotificationRequestedHandler(
        IEmailSender emailSender,
        IEmailTemplateRenderer templateRenderer)
    {
        ArgumentNullException.ThrowIfNull(emailSender);
        ArgumentNullException.ThrowIfNull(templateRenderer);

        _emailSender = emailSender;
        _templateRenderer = templateRenderer;
    }

    public async Task HandleAsync(
        EmailChangedNotificationRequested integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var message =
            _templateRenderer.RenderEmailChangedNotification(
                integrationEvent.OldEmail,
                integrationEvent.NewEmail,
                integrationEvent.OccurredAtUtc);

        await _emailSender.SendAsync(
            message,
            cancellationToken);
    }
}
