using PGLN.Auth.Application.Abstractions.Email;
using PGLN.Auth.Application.Abstractions.Events;

namespace PGLN.Auth.Application.Events.Email;

public sealed class AccountLockedNotificationRequestedHandler
    : IIntegrationEventHandler<AccountLockedNotificationRequested>
{
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateRenderer _emailTemplateRenderer;

    public AccountLockedNotificationRequestedHandler(
        IEmailSender emailSender,
        IEmailTemplateRenderer emailTemplateRenderer)
    {
        ArgumentNullException.ThrowIfNull(emailSender);
        ArgumentNullException.ThrowIfNull(emailTemplateRenderer);

        _emailSender = emailSender;
        _emailTemplateRenderer = emailTemplateRenderer;
    }

    public Task HandleAsync(
        AccountLockedNotificationRequested integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var message =
            _emailTemplateRenderer.RenderAccountLocked(
                integrationEvent.Email,
                integrationEvent.LockedUntilUtc,
                integrationEvent.OccurredAtUtc);

        return _emailSender.SendAsync(
            message,
            cancellationToken);
    }
}
