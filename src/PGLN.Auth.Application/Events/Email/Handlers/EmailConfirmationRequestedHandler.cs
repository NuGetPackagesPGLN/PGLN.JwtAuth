using PGLN.Auth.Application.Abstractions.Email;
using PGLN.Auth.Application.Abstractions.Events;

namespace PGLN.Auth.Application.Events.Email.Handlers;

public sealed class EmailConfirmationRequestedHandler
    : IIntegrationEventHandler<EmailConfirmationRequested>
{
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly EmailDeliveryOptions _options;

    public EmailConfirmationRequestedHandler(
        IEmailSender emailSender,
        IEmailTemplateRenderer templateRenderer,
        EmailDeliveryOptions options)
    {
        ArgumentNullException.ThrowIfNull(emailSender);
        ArgumentNullException.ThrowIfNull(templateRenderer);
        ArgumentNullException.ThrowIfNull(options);

        options.Validate();

        _emailSender = emailSender;
        _templateRenderer = templateRenderer;
        _options = options;
    }

    public async Task HandleAsync(
        EmailConfirmationRequested integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var separator =
            _options.ConfirmationBaseUrl.Contains('?')
                ? "&"
                : "?";

        var verificationUrl =
            $"{_options.ConfirmationBaseUrl}{separator}token={Uri.EscapeDataString(integrationEvent.VerificationToken)}";

        var message =
            _templateRenderer.RenderEmailConfirmation(
                integrationEvent.Email,
                verificationUrl);

        await _emailSender.SendAsync(
            message,
            cancellationToken);
    }
}
