using FluentValidation;

namespace PGLN.Auth.Application.Features.ExternalAuthentication.CompleteExternalLogin;

public sealed class CompleteExternalLoginCommandValidator
    : AbstractValidator<CompleteExternalLoginCommand>
{
    public CompleteExternalLoginCommandValidator()
    {
        RuleFor(command => command.Provider)
            .IsInEnum();

        RuleFor(command => command.AuthorizationCode)
            .NotEmpty()
            .MaximumLength(4096);

        RuleFor(command => command.State)
            .NotEmpty()
            .MaximumLength(4096);

        RuleFor(command => command.RedirectUri)
            .NotEmpty()
            .MaximumLength(2048);

        RuleFor(command => command.DeviceIdHash)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(command => command.DeviceName)
            .MaximumLength(256);

        RuleFor(command => command.IpAddress)
            .MaximumLength(64);

        RuleFor(command => command.UserAgent)
            .MaximumLength(1024);
    }
}


