using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.Login;

public static class LoginErrors
{
    public static readonly Error InvalidCredentials =
        new(
            "Login.InvalidCredentials",
            "The email address or password is incorrect.");

    public static readonly Error EmailNotConfirmed =
        new(
            "Login.EmailNotConfirmed",
            "The email address must be confirmed before signing in.");

    public static readonly Error AccountLocked =
        new(
            "Login.AccountLocked",
            "The account is temporarily locked. Please try again later.");

    public static readonly Error TooManyAttempts =
        new(
            "Login.TooManyAttempts",
            "Too many login attempts. Please try again later.");
}


