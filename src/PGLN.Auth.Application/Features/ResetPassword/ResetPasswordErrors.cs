using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.ResetPassword;

public static class ResetPasswordErrors
{
    public static readonly Error InvalidRequest =
        new(
            "ResetPassword.InvalidRequest",
            "The password reset request is invalid.");

    public static readonly Error InvalidToken =
        new(
            "ResetPassword.InvalidToken",
            "The password reset token is invalid.");

    public static readonly Error ExpiredToken =
        new(
            "ResetPassword.ExpiredToken",
            "The password reset token has expired.");

    public static readonly Error UsedToken =
        new(
            "ResetPassword.UsedToken",
            "The password reset token has already been used.");
}
