namespace PGLN.Auth.Application.Features.Login;

public sealed record LoginResult(
    LoginStatus Status,
    Guid UserId,
    string Email,
    string? AccessToken,
    DateTimeOffset? AccessTokenExpiresAtUtc,
    string? RefreshToken,
    DateTimeOffset? RefreshTokenExpiresAtUtc,
    Guid? StepUpChallengeId)
{
    public static LoginResult AuthenticationComplete(
        Guid userId,
        string email,
        string accessToken,
        DateTimeOffset accessTokenExpiresAtUtc,
        string refreshToken,
        DateTimeOffset refreshTokenExpiresAtUtc)
    {
        return new LoginResult(
            LoginStatus.AuthenticationComplete,
            userId,
            email,
            accessToken,
            accessTokenExpiresAtUtc,
            refreshToken,
            refreshTokenExpiresAtUtc,
            null);
    }

    public static LoginResult StepUpRequired(
        Guid userId,
        string email,
        Guid stepUpChallengeId)
    {
        return new LoginResult(
            LoginStatus.StepUpRequired,
            userId,
            email,
            null,
            null,
            null,
            null,
            stepUpChallengeId);
    }
}
