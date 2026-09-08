namespace PGLN.Auth.Contracts.Authentication;

public sealed record RefreshTokenResponse(
    Guid UserId,
    string Email,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);
