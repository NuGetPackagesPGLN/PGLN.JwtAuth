using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.LogoutAll;

public static class LogoutAllErrors
{
    public static readonly Error InvalidToken =
        new(
            "LogoutAll.InvalidToken",
            "The refresh token is invalid.");

    public static readonly Error ExpiredToken =
        new(
            "LogoutAll.ExpiredToken",
            "The refresh token has expired.");

    public static readonly Error RevokedToken =
        new(
            "LogoutAll.RevokedToken",
            "The refresh token has been revoked.");
}
