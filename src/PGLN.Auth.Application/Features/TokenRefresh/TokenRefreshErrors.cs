using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.TokenRefresh;

public static class TokenRefreshErrors
{
    public static readonly Error InvalidToken =
        new(
            "TokenRefresh.InvalidToken",
            "The refresh token is invalid.");

    public static readonly Error ExpiredToken =
        new(
            "TokenRefresh.ExpiredToken",
            "The refresh token has expired.");

    public static readonly Error RevokedToken =
        new(
            "TokenRefresh.RevokedToken",
            "The refresh token has already been revoked.");

    public static readonly Error UserNotFound =
        new(
            "TokenRefresh.UserNotFound",
            "The user associated with the refresh token could not be found.");
}
