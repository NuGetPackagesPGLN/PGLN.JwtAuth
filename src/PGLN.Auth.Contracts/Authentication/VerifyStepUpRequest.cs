namespace PGLN.Auth.Contracts.Authentication;

public sealed record VerifyStepUpRequest(
    Guid ChallengeId,
    string Code,
    bool RememberDevice);
