using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.EmailConfirmation;

public static class EmailConfirmationErrors
{
    public static readonly Error InvalidToken =
        new(
            "EmailConfirmation.InvalidToken",
            "The email confirmation token is invalid.");

    public static readonly Error ExpiredToken =
        new(
            "EmailConfirmation.ExpiredToken",
            "The email confirmation token has expired.");

    public static readonly Error TokenAlreadyUsed =
        new(
            "EmailConfirmation.TokenAlreadyUsed",
            "The email confirmation token has already been used.");

    public static readonly Error UserNotFound =
        new(
            "EmailConfirmation.UserNotFound",
            "The user associated with this email confirmation token could not be found.");
}
