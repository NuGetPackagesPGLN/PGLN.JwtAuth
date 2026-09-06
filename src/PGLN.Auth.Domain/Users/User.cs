using PGLN.Auth.Domain.Common;
using PGLN.Auth.Domain.Users.Events;

namespace PGLN.Auth.Domain.Users;

public sealed class User : AggregateRoot<UserId>
{
    private User(
        UserId id,
        Email email,
        string passwordHash,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        CreatedAtUtc = createdAtUtc;
    }

    public Email Email { get; private set; }

    public string PasswordHash { get; private set; }

    public bool EmailConfirmed { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? EmailConfirmedAtUtc { get; private set; }

    public static User Register(
        UserId id,
        Email email,
        string passwordHash,
        DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        var user = new User(
            id,
            email,
            passwordHash,
            createdAtUtc);

        user.RaiseDomainEvent(
            new UserRegistered(
                user.Id,
                user.Email.Value,
                createdAtUtc));

        return user;
    }

    public void ConfirmEmail(DateTimeOffset confirmedAtUtc)
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