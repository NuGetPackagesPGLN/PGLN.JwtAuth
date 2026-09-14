namespace PGLN.Auth.Application.Features.ExternalAuthentication.CompleteExternalLogin;

public sealed record CompleteExternalLoginResult(
    Guid UserId,
    string Email,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    bool IsNewUser);
