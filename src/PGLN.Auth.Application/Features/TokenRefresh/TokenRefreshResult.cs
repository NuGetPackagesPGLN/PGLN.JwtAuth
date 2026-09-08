namespace PGLN.Auth.Application.Features.TokenRefresh;

public sealed record TokenRefreshResult(
    Guid UserId,
    string Email,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);
