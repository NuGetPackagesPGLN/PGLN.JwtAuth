using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.ChangeEmail;

public static class RequestEmailChangeErrors
{
    public static readonly Error UserNotFound =
        new(
            "ChangeEmail.UserNotFound",
            "The user could not be found.");

    public static readonly Error InvalidCurrentPassword =
        new(
            "ChangeEmail.InvalidCurrentPassword",
            "The current password is incorrect.");

    public static readonly Error EmailAlreadyInUse =
        new(
            "ChangeEmail.EmailAlreadyInUse",
            "The requested email address is already in use.");

    public static readonly Error SameEmail =
        new(
            "ChangeEmail.SameEmail",
            "The requested email address is the same as the current email address.");

    public static readonly Error InvalidEmail =
        new(
            "ChangeEmail.InvalidEmail",
            "The requested email address is invalid.");
}
