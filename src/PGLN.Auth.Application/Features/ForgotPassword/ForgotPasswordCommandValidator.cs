using FluentValidation;

namespace PGLN.Auth.Application.Features.ForgotPassword;

public sealed class ForgotPasswordCommandValidator
    : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);
    }
}
