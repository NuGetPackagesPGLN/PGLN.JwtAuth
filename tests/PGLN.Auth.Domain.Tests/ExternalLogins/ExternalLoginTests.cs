using PGLN.Auth.Domain.ExternalLogins;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Domain.Tests.ExternalLogins;

public sealed class ExternalLoginTests
{
    [Fact]
    public void Create_ShouldCreateExternalLogin()
    {
        var userId =
            new UserId(Guid.NewGuid());

        var now =
            DateTimeOffset.UtcNow;

        var externalLogin =
            ExternalLogin.Create(
                userId,
                ExternalLoginProvider.Google,
                "google-subject-123",
                "User@Example.com",
                now);

        Assert.NotEqual(
            default,
            externalLogin.Id);

        Assert.Equal(
            userId,
            externalLogin.UserId);

        Assert.Equal(
            ExternalLoginProvider.Google,
            externalLogin.Provider);

        Assert.Equal(
            "google-subject-123",
            externalLogin.ProviderSubject);

        Assert.Equal(
            "user@example.com",
            externalLogin.Email);

        Assert.Equal(
            now,
            externalLogin.CreatedAtUtc);

        Assert.Equal(
            now,
            externalLogin.LastLoginAtUtc);
    }

    [Fact]
    public void Create_ShouldTrimProviderSubject()
    {
        var externalLogin =
            ExternalLogin.Create(
                new UserId(Guid.NewGuid()),
                ExternalLoginProvider.GitHub,
                "  github-user-123  ",
                null,
                DateTimeOffset.UtcNow);

        Assert.Equal(
            "github-user-123",
            externalLogin.ProviderSubject);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Create_WhenProviderSubjectIsBlank_ShouldThrow(
        string providerSubject)
    {
        var action = () =>
            ExternalLogin.Create(
                new UserId(Guid.NewGuid()),
                ExternalLoginProvider.Google,
                providerSubject,
                null,
                DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentException>(
            action);
    }

    [Fact]
    public void Create_WhenEmailIsNull_ShouldAllowNullEmail()
    {
        var externalLogin =
            ExternalLogin.Create(
                new UserId(Guid.NewGuid()),
                ExternalLoginProvider.GitHub,
                "github-subject",
                null,
                DateTimeOffset.UtcNow);

        Assert.Null(
            externalLogin.Email);
    }

    [Fact]
    public void RecordLogin_ShouldUpdateLastLoginTime()
    {
        var createdAt =
            DateTimeOffset.UtcNow;

        var loggedInAt =
            createdAt.AddHours(1);

        var externalLogin =
            ExternalLogin.Create(
                new UserId(Guid.NewGuid()),
                ExternalLoginProvider.Apple,
                "apple-subject",
                "user@example.com",
                createdAt);

        externalLogin.RecordLogin(
            loggedInAt);

        Assert.Equal(
            loggedInAt,
            externalLogin.LastLoginAtUtc);
    }

    [Fact]
    public void RecordLogin_WhenEmailProvided_ShouldUpdateEmail()
    {
        var externalLogin =
            ExternalLogin.Create(
                new UserId(Guid.NewGuid()),
                ExternalLoginProvider.Google,
                "google-subject",
                "old@example.com",
                DateTimeOffset.UtcNow);

        externalLogin.RecordLogin(
            DateTimeOffset.UtcNow.AddMinutes(5),
            "New@Example.com");

        Assert.Equal(
            "new@example.com",
            externalLogin.Email);
    }

    [Fact]
    public void RecordLogin_WhenEmailIsBlank_ShouldKeepExistingEmail()
    {
        var externalLogin =
            ExternalLogin.Create(
                new UserId(Guid.NewGuid()),
                ExternalLoginProvider.Google,
                "google-subject",
                "existing@example.com",
                DateTimeOffset.UtcNow);

        externalLogin.RecordLogin(
            DateTimeOffset.UtcNow.AddMinutes(5),
            "   ");

        Assert.Equal(
            "existing@example.com",
            externalLogin.Email);
    }
}
