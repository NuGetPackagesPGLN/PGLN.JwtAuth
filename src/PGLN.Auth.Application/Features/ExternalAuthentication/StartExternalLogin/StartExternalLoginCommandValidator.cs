using FluentValidation;

namespace PGLN.Auth.Application.Features.ExternalAuthentication.StartExternalLogin;

public sealed class StartExternalLoginCommandValidator
    : AbstractValidator<StartExternalLoginCommand>
{
    public StartExternalLoginCommandValidator()
    {
        RuleFor(command => command.Provider)
            .IsInEnum();

        RuleFor(command => command.RedirectUri)
            .NotEmpty()
            .MaximumLength(2048)
            .Must(BeValidAbsoluteHttpsUri)
            .WithMessage(
                "Redirect URI must be a valid absolute HTTPS URI.");

        RuleFor(command => command.DeviceIdHash)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(command => command.DeviceName)
            .MaximumLength(256)
            .When(command =>
                !string.IsNullOrWhiteSpace(
                    command.DeviceName));
    }

    private static bool BeValidAbsoluteHttpsUri(
        string redirectUri)
    {
        if (!Uri.TryCreate(
                redirectUri,
                UriKind.Absolute,
                out var uri))
        {
            return false;
        }

        return uri.Scheme ==
               Uri.UriSchemeHttps;
    }
}
