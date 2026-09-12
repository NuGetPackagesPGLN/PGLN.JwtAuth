using PGLN.Auth.Application.Abstractions.Email;
using PGLN.Auth.Application.Abstractions.Events;

namespace PGLN.Auth.Application.Events.Email;

public sealed class NewDeviceLoginNotificationRequestedHandler
    : IIntegrationEventHandler<NewDeviceLoginNotificationRequested>
{
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateRenderer _emailTemplateRenderer;

    public NewDeviceLoginNotificationRequestedHandler(
        IEmailSender emailSender,
        IEmailTemplateRenderer emailTemplateRenderer)
    {
        ArgumentNullException.ThrowIfNull(emailSender);
        ArgumentNullException.ThrowIfNull(emailTemplateRenderer);

        _emailSender = emailSender;
        _emailTemplateRenderer = emailTemplateRenderer;
    }

    public Task HandleAsync(
        NewDeviceLoginNotificationRequested integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var message =
            _emailTemplateRenderer.RenderNewDeviceLogin(
                integrationEvent.Email,
                integrationEvent.DeviceName,
                integrationEvent.IpAddress,
                integrationEvent.UserAgent,
                integrationEvent.OccurredAtUtc);

        return _emailSender.SendAsync(
            message,
            cancellationToken);
    }
}
