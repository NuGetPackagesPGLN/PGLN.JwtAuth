using PGLN.Auth.Application.Abstractions.Email;
using PGLN.Auth.Application.Abstractions.Events;

namespace PGLN.Auth.Application.Events.Email;

public sealed class StepUpVerificationCodeRequestedHandler
    : IIntegrationEventHandler<StepUpVerificationCodeRequested>
{
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateRenderer _emailTemplateRenderer;

    public StepUpVerificationCodeRequestedHandler(
        IEmailSender emailSender,
        IEmailTemplateRenderer emailTemplateRenderer)
    {
        ArgumentNullException.ThrowIfNull(emailSender);
        ArgumentNullException.ThrowIfNull(emailTemplateRenderer);

        _emailSender = emailSender;
        _emailTemplateRenderer = emailTemplateRenderer;
    }

    public Task HandleAsync(
        StepUpVerificationCodeRequested integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var message =
            _emailTemplateRenderer.RenderStepUpVerificationCode(
                integrationEvent.Email,
                integrationEvent.Code,
                integrationEvent.DeviceName,
                integrationEvent.IpAddress,
                integrationEvent.UserAgent,
                integrationEvent.ExpiresAtUtc);

        return _emailSender.SendAsync(
            message,
            cancellationToken);
    }
}
