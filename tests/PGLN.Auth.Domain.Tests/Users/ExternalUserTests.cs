using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Domain.Tests.Users;

public sealed class ExternalUserTests
{
    [Fact]
    public void RegisterExternal_ShouldCreateConfirmedUserWithoutPassword()
    {
        var now =
            new DateTimeOffset(
                2026,
                9,
                13,
                12,
                0,
                0,
                TimeSpan.Zero);

        var user =
            User.RegisterExternal(
                UserId.New(),
                Email.Create("external@example.com"),
                now);

        Assert.Equal(
            "external@example.com",
            user.Email.Value);

        Assert.Equal(
            "EXTERNAL@EXAMPLE.COM",
            user.NormalizedEmail);

        Assert.Null(
            user.PasswordHash);

        Assert.False(
            user.HasPassword);

        Assert.True(
            user.EmailConfirmed);

        Assert.Equal(
            now,
            user.EmailConfirmedAtUtc);

        Assert.Equal(
            now,
            user.CreatedAtUtc);
    }

    [Fact]
    public void Register_ShouldStillCreatePasswordUser()
    {
        var now =
            new DateTimeOffset(
                2026,
                9,
                13,
                12,
                0,
                0,
                TimeSpan.Zero);

        var user =
            User.Register(
                UserId.New(),
                Email.Create("password@example.com"),
                "PASSWORD_HASH",
                now);

        Assert.Equal(
            "PASSWORD_HASH",
            user.PasswordHash);

        Assert.True(
            user.HasPassword);

        Assert.False(
            user.EmailConfirmed);

        Assert.Null(
            user.EmailConfirmedAtUtc);
    }

    [Fact]
    public void ChangePassword_ShouldGiveExternalUserPasswordCapability()
    {
        var createdAtUtc =
            new DateTimeOffset(
                2026,
                9,
                13,
                12,
                0,
                0,
                TimeSpan.Zero);

        var changedAtUtc =
            createdAtUtc.AddMinutes(10);

        var user =
            User.RegisterExternal(
                UserId.New(),
                Email.Create("external@example.com"),
                createdAtUtc);

        user.ChangePassword(
            "NEW_PASSWORD_HASH",
            changedAtUtc);

        Assert.Equal(
            "NEW_PASSWORD_HASH",
            user.PasswordHash);

        Assert.True(
            user.HasPassword);
    }
}
