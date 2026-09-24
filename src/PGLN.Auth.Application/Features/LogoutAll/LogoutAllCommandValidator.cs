using FluentValidation;

namespace PGLN.Auth.Application.Features.LogoutAll;

internal sealed class LogoutAllCommandValidator
    : AbstractValidator<LogoutAllCommand>
{
    public LogoutAllCommandValidator()
    {
        RuleFor(
                command =>
                    command.RefreshToken)
            .NotEmpty();
    }
}
