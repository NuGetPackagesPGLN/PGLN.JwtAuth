using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.StepUp.VerifyStepUpChallenge;

public sealed record VerifyStepUpChallengeCommand(
    Guid ChallengeId,
    string Code,
    bool RememberDevice)
    : ICommand<Result<VerifyStepUpChallengeResult>>;
