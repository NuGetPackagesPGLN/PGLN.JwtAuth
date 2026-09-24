using FluentValidation;

namespace PGLN.Auth.Application.Features.Login;

internal sealed class LoginCommandValidator
    : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(command => command.Password)
            .NotEmpty()
            .MaximumLength(128);

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
