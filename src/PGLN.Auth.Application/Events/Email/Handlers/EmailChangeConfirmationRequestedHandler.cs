using PGLN.Auth.Application.Abstractions.Email;
using PGLN.Auth.Application.Abstractions.Events;

namespace PGLN.Auth.Application.Events.Email.Handlers;

public sealed class EmailChangeConfirmationRequestedHandler
    : IIntegrationEventHandler<EmailChangeConfirmationRequested>
{
    private readonly IEmailSender
        _emailSender;

    private readonly IEmailTemplateRenderer
        _templateRenderer;

    private readonly EmailDeliveryOptions
        _options;

    public EmailChangeConfirmationRequestedHandler(
        IEmailSender emailSender,
        IEmailTemplateRenderer templateRenderer,
        EmailDeliveryOptions options)
    {
        ArgumentNullException.ThrowIfNull(
            emailSender);

        ArgumentNullException.ThrowIfNull(
            templateRenderer);

        ArgumentNullException.ThrowIfNull(
            options);

        options.Validate();

        _emailSender =
            emailSender;

        _templateRenderer =
            templateRenderer;

        _options =
            options;
    }

    public async Task HandleAsync(
        EmailChangeConfirmationRequested integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            integrationEvent);

        var separator =
            _options
                .EmailChangeConfirmationBaseUrl
                .Contains('?')
                    ? "&"
                    : "?";

        var verificationUrl =
            $"{_options.EmailChangeConfirmationBaseUrl}{separator}token={Uri.EscapeDataString(integrationEvent.VerificationToken)}";

        var message =
            _templateRenderer
                .RenderEmailChangeConfirmation(
                    integrationEvent.NewEmail,
                    verificationUrl);

        await _emailSender
            .SendAsync(
                message,
                cancellationToken);
    }
}
