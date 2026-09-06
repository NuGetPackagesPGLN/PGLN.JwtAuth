using PGLN.Auth.Application.Abstractions.Email;
using PGLN.Auth.Application.Abstractions.Events;

namespace PGLN.Auth.Application.Events.Email.Handlers;

public sealed class WelcomeEmailRequestedHandler
    : IIntegrationEventHandler<WelcomeEmailRequested>
{
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateRenderer _templateRenderer;

    public WelcomeEmailRequestedHandler(
        IEmailSender emailSender,
        IEmailTemplateRenderer templateRenderer)
    {
        ArgumentNullException.ThrowIfNull(emailSender);
        ArgumentNullException.ThrowIfNull(templateRenderer);

        _emailSender = emailSender;
        _templateRenderer = templateRenderer;
    }

    public async Task HandleAsync(
        WelcomeEmailRequested integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var message =
            _templateRenderer.RenderWelcomeEmail(
                integrationEvent.Email);

        await _emailSender.SendAsync(
            message,
            cancellationToken);
    }
}
