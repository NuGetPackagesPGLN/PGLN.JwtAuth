using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.ChangeEmail;

public static class ConfirmEmailChangeErrors
{
    public static readonly Error InvalidToken =
        new(
            "ConfirmEmailChange.InvalidToken",
            "The email change token is invalid.");

    public static readonly Error ExpiredToken =
        new(
            "ConfirmEmailChange.ExpiredToken",
            "The email change token has expired.");

    public static readonly Error TokenAlreadyUsed =
        new(
            "ConfirmEmailChange.TokenAlreadyUsed",
            "The email change token has already been used.");

    public static readonly Error UserNotFound =
        new(
            "ConfirmEmailChange.UserNotFound",
            "The user associated with the email change token was not found.");

    public static readonly Error EmailAlreadyInUse =
        new(
            "ConfirmEmailChange.EmailAlreadyInUse",
            "The requested email address is already in use.");
}
