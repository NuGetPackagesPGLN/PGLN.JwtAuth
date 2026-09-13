namespace PGLN.Auth.Application.Features.StepUp.VerifyStepUpChallenge;

public sealed record VerifyStepUpChallengeResult(
    Guid UserId,
    string Email,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);
