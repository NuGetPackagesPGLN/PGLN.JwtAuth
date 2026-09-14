using PGLN.Auth.Domain.Common;
using PGLN.Auth.Domain.Users.Events;

namespace PGLN.Auth.Domain.Users;

public sealed class User : AggregateRoot<UserId>
{
    private string _normalizedEmail = string.Empty;

    private User()
        : base(default)
    {
        Email = null!;
    }

    private User(
        UserId id,
        Email email,
        string? passwordHash,
        DateTimeOffset createdAtUtc,
        bool emailConfirmed)
        : base(id)
    {
        Email = email;
        _normalizedEmail = email.NormalizedValue;
        PasswordHash = passwordHash;
        CreatedAtUtc = createdAtUtc;
        EmailConfirmed = emailConfirmed;

        if (emailConfirmed)
        {
            EmailConfirmedAtUtc = createdAtUtc;
        }
    }

    public Email Email { get; private set; }

    public string NormalizedEmail =>
        _normalizedEmail;

    public string? PasswordHash { get; private set; }

    public bool HasPassword =>
        !string.IsNullOrWhiteSpace(PasswordHash);

    public bool EmailConfirmed { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? EmailConfirmedAtUtc { get; private set; }

    public int FailedLoginAttempts { get; private set; }

    public DateTimeOffset? LastFailedLoginAtUtc { get; private set; }

    public DateTimeOffset? LockoutEndUtc { get; private set; }

    public bool IsLockedOut(
        DateTimeOffset now)
    {
        return LockoutEndUtc.HasValue &&
               now < LockoutEndUtc.Value;
    }

    public static User Register(
        UserId id,
        Email email,
        string passwordHash,
        DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        var user =
            new User(
                id,
                email,
                passwordHash,
                createdAtUtc,
                emailConfirmed: false);

        user.RaiseDomainEvent(
            new UserRegistered(
                user.Id,
                user.Email.Value,
                createdAtUtc));

        return user;
    }

    public static User RegisterExternal(
        UserId id,
        Email email,
        DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(email);

        return new User(
            id,
            email,
            passwordHash: null,
            createdAtUtc,
            emailConfirmed: true);
    }

    public void ConfirmEmail(
        DateTimeOffset confirmedAtUtc)
    {
        if (EmailConfirmed)
        {
            return;
        }

        EmailConfirmed = true;
        EmailConfirmedAtUtc = confirmedAtUtc;

        RaiseDomainEvent(
            new EmailConfirmed(
                Id,
                Email.Value,
                confirmedAtUtc));
    }

    public void ConfirmEmailChange(
        Email newEmail,
        DateTimeOffset confirmedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(newEmail);

        Email = newEmail;
        _normalizedEmail =
            newEmail.NormalizedValue;

        EmailConfirmed = true;
        EmailConfirmedAtUtc =
            confirmedAtUtc;
    }

    public void RecordFailedLoginAttempt(
        DateTimeOffset attemptedAtUtc)
    {
        FailedLoginAttempts++;

        LastFailedLoginAtUtc =
            attemptedAtUtc;
    }

    public void LockOutUntil(
        DateTimeOffset lockoutEndUtc)
    {
        LockoutEndUtc =
            lockoutEndUtc;
    }

    public void ResetFailedLoginAttempts()
    {
        FailedLoginAttempts =
            0;

        LastFailedLoginAtUtc =
            null;

        LockoutEndUtc =
            null;
    }

    public void ChangePassword(
        string passwordHash,
        DateTimeOffset changedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        PasswordHash = passwordHash;

        RaiseDomainEvent(
            new PasswordChanged(
                Id,
                changedAtUtc));
    }
}
