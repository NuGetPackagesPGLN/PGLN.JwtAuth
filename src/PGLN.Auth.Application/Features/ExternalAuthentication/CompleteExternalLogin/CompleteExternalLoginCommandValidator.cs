using FluentValidation;

namespace PGLN.Auth.Application.Features.ExternalAuthentication.CompleteExternalLogin;

internal sealed class CompleteExternalLoginCommandValidator
    : AbstractValidator<CompleteExternalLoginCommand>
{
    public CompleteExternalLoginCommandValidator()
    {
        RuleFor(command => command.Provider)
            .IsInEnum();

        RuleFor(command => command.AuthorizationCode)
            .MaximumLength(4096);

        RuleFor(command => command.ProviderError)
            .MaximumLength(256);

        RuleFor(command => command.State)
            .NotEmpty()
            .MaximumLength(4096);

        RuleFor(command => command.IpAddress)
            .MaximumLength(64);

        RuleFor(command => command.UserAgent)
            .MaximumLength(1024);
    }
}
