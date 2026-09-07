namespace PGLN.Auth.Domain.LoginAttempts;

public enum LoginFailureReason
{
    InvalidCredentials = 1,
    EmailNotConfirmed = 2,
    AccountDisabled = 3,
    AccountLocked = 4
}
