using PGLN.Auth.Domain.Users;
using PGLN.Auth.Domain.Users.Events;

namespace PGLN.Auth.Domain.Tests.Users;

public sealed class UserTests
{
    [Fact]
    public void Register_Should_Create_User()
    {
        var id = UserId.New();
        var email = Email.Create("user@example.com");
        var createdAt = DateTimeOffset.UtcNow;

        var user = User.Register(
            id,
            email,
            "hashed-password",
            createdAt);

        Assert.Equal(id, user.Id);
        Assert.Equal(email, user.Email);
        Assert.Equal("hashed-password", user.PasswordHash);
        Assert.False(user.EmailConfirmed);
        Assert.Equal(createdAt, user.CreatedAtUtc);
    }

    [Fact]
    public void Register_Should_Raise_UserRegistered_Event()
    {
        var user = User.Register(
            UserId.New(),
            Email.Create("user@example.com"),
            "hashed-password",
            DateTimeOffset.UtcNow);

        var domainEvent = Assert.Single(user.DomainEvents);

        Assert.IsType<UserRegistered>(domainEvent);
    }

    [Fact]
    public void ConfirmEmail_Should_Confirm_User()
    {
        var user = User.Register(
            UserId.New(),
            Email.Create("user@example.com"),
            "hashed-password",
            DateTimeOffset.UtcNow);

        user.ClearDomainEvents();

        var confirmedAt = DateTimeOffset.UtcNow;

        user.ConfirmEmail(confirmedAt);

        Assert.True(user.EmailConfirmed);
        Assert.Equal(confirmedAt, user.EmailConfirmedAtUtc);

        var domainEvent = Assert.Single(user.DomainEvents);

        Assert.IsType<EmailConfirmed>(domainEvent);
    }

    [Fact]
    public void ChangePassword_Should_Update_Hash()
    {
        var user = User.Register(
            UserId.New(),
            Email.Create("user@example.com"),
            "old-hash",
            DateTimeOffset.UtcNow);

        user.ClearDomainEvents();

        user.ChangePassword(
            "new-hash",
            DateTimeOffset.UtcNow);

        Assert.Equal("new-hash", user.PasswordHash);

        var domainEvent = Assert.Single(user.DomainEvents);

        Assert.IsType<PasswordChanged>(domainEvent);
    }
}