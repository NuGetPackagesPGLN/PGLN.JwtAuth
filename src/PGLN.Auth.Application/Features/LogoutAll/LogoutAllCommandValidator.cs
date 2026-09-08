using FluentValidation;

namespace PGLN.Auth.Application.Features.LogoutAll;

public sealed class LogoutAllCommandValidator
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
