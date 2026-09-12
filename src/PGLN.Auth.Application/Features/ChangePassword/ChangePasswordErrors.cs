using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.ChangePassword;

public static class ChangePasswordErrors
{
    public static readonly Error UserNotFound = new(
        "ChangePassword.UserNotFound",
        "The user could not be found.");

    public static readonly Error InvalidCurrentPassword = new(
        "ChangePassword.InvalidCurrentPassword",
        "The current password is incorrect.");

    public static readonly Error SamePassword = new(
        "ChangePassword.SamePassword",
        "The new password must be different from the current password.");
}
