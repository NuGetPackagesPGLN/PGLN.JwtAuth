using PGLN.Auth.Application.Abstractions.Email;
using PGLN.Auth.Application.Abstractions.Events;

namespace PGLN.Auth.Application.Events.Email.Handlers;

public sealed class PasswordResetRequestedHandler
    : IIntegrationEventHandler<PasswordResetRequested>
{
    private readonly IEmailSender _emailSender;

    private readonly IEmailTemplateRenderer
        _templateRenderer;

    private readonly EmailDeliveryOptions
        _options;

    public PasswordResetRequestedHandler(
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
        PasswordResetRequested integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            integrationEvent);

        var separator =
            _options.ResetPasswordBaseUrl.Contains(
                '?')
                ? "&"
                : "?";

        var resetUrl =
            $"{_options.ResetPasswordBaseUrl}" +
            $"{separator}token=" +
            $"{Uri.EscapeDataString(integrationEvent.ResetToken)}";

        var message =
            _templateRenderer.RenderPasswordReset(
                integrationEvent.Email,
                resetUrl);

        await _emailSender.SendAsync(
            message,
            cancellationToken);
    }
}
