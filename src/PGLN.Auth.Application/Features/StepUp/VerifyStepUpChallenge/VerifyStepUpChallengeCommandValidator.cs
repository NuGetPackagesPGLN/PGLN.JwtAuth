using FluentValidation;

namespace PGLN.Auth.Application.Features.StepUp.VerifyStepUpChallenge;

public sealed class VerifyStepUpChallengeCommandValidator
    : AbstractValidator<VerifyStepUpChallengeCommand>
{
    public VerifyStepUpChallengeCommandValidator()
    {
        RuleFor(command => command.ChallengeId)
            .NotEmpty();

        RuleFor(command => command.Code)
            .NotEmpty()
            .Matches(@"^\d{6}$");
    }
}
