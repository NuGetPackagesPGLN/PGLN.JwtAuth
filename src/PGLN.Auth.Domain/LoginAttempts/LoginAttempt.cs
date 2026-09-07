using PGLN.Auth.Domain.Common;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Domain.LoginAttempts;

public sealed class LoginAttempt
    : Entity<LoginAttemptId>
{
    private LoginAttempt()
        : base(default)
    {
        Email = string.Empty;
    }

    private LoginAttempt(
        LoginAttemptId id,
        string email,
        UserId? userId,
        bool succeeded,
        LoginFailureReason? failureReason,
        DateTimeOffset attemptedAtUtc)
        : base(id)
    {
        Email = email;
        UserId = userId;
        Succeeded = succeeded;
        FailureReason = failureReason;
        AttemptedAtUtc = attemptedAtUtc;
    }

    public string Email { get; private set; }

    public UserId? UserId { get; private set; }

    public bool Succeeded { get; private set; }

    public LoginFailureReason? FailureReason { get; private set; }

    public DateTimeOffset AttemptedAtUtc { get; private set; }

    public static LoginAttempt Successful(
        string email,
        UserId userId,
        DateTimeOffset attemptedAtUtc)
    {
        ValidateEmail(email);

        return new LoginAttempt(
            LoginAttemptId.New(),
            email,
            userId,
            true,
            null,
            attemptedAtUtc);
    }

    public static LoginAttempt Failed(
        string email,
        UserId? userId,
        LoginFailureReason failureReason,
        DateTimeOffset attemptedAtUtc)
    {
        ValidateEmail(email);

        return new LoginAttempt(
            LoginAttemptId.New(),
            email,
            userId,
            false,
            failureReason,
            attemptedAtUtc);
    }

    private static void ValidateEmail(
        string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            email);
    }
}
