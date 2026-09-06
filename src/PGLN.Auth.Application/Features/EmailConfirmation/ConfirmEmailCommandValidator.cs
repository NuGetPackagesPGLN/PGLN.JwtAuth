using FluentValidation;

namespace PGLN.Auth.Application.Features.EmailConfirmation;

public sealed class ConfirmEmailCommandValidator
    : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(command => command.Token)
            .NotEmpty()
            .WithMessage(
                "Email confirmation token is required.");
    }
}
