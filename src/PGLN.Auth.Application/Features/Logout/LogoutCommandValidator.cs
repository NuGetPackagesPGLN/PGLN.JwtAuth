using FluentValidation;

namespace PGLN.Auth.Application.Features.Logout;

internal sealed class LogoutCommandValidator
    : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(command => command.RefreshToken)
            .NotEmpty()
            .MaximumLength(2048);
    }
}
