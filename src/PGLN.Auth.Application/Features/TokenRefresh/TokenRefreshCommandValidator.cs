using FluentValidation;

namespace PGLN.Auth.Application.Features.TokenRefresh;

public sealed class TokenRefreshCommandValidator
    : AbstractValidator<TokenRefreshCommand>
{
    public TokenRefreshCommandValidator()
    {
        RuleFor(command => command.RefreshToken)
            .NotEmpty()
            .MaximumLength(2048);
    }
}
