namespace PGLN.Auth.Contracts.Authentication;

public sealed record LoginResponse(
    string Status,
    Guid UserId,
    string Email,
    string? AccessToken,
    DateTimeOffset? AccessTokenExpiresAtUtc,
    string? RefreshToken,
    DateTimeOffset? RefreshTokenExpiresAtUtc,
    Guid? StepUpChallengeId);
