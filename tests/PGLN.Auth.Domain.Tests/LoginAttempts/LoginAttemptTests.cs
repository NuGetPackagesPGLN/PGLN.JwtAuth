using PGLN.Auth.Domain.LoginAttempts;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Domain.Tests.LoginAttempts;

public sealed class LoginAttemptTests
{
    private static readonly DateTimeOffset Now =
        new(
            2026,
            9,
            7,
            10,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public void Successful_ShouldCreateSuccessfulAttempt()
    {
        var userId =
            UserId.New();

        var attempt =
            LoginAttempt.Successful(
                "user@example.com",
                userId,
                Now);

        Assert.True(
            attempt.Succeeded);

        Assert.Equal(
            "user@example.com",
            attempt.Email);

        Assert.Equal(
            userId,
            attempt.UserId);

        Assert.Null(
            attempt.FailureReason);

        Assert.Equal(
            Now,
            attempt.AttemptedAtUtc);
    }

    [Fact]
    public void Failed_WithKnownUser_ShouldCreateFailedAttempt()
    {
        var userId =
            UserId.New();

        var attempt =
            LoginAttempt.Failed(
                "user@example.com",
                userId,
                LoginFailureReason.InvalidCredentials,
                Now);

        Assert.False(
            attempt.Succeeded);

        Assert.Equal(
            userId,
            attempt.UserId);

        Assert.Equal(
            LoginFailureReason.InvalidCredentials,
            attempt.FailureReason);
    }

    [Fact]
    public void Failed_WithUnknownUser_ShouldAllowNullUserId()
    {
        var attempt =
            LoginAttempt.Failed(
                "missing@example.com",
                null,
                LoginFailureReason.InvalidCredentials,
                Now);

        Assert.False(
            attempt.Succeeded);

        Assert.Null(
            attempt.UserId);

        Assert.Equal(
            LoginFailureReason.InvalidCredentials,
            attempt.FailureReason);
    }

    [Fact]
    public void Successful_WhenEmailIsEmpty_ShouldThrow()
    {
        Assert.Throws<
            ArgumentException>(
            () =>
                LoginAttempt.Successful(
                    string.Empty,
                    UserId.New(),
                    Now));
    }

    [Fact]
    public void Failed_WhenEmailIsEmpty_ShouldThrow()
    {
        Assert.Throws<
            ArgumentException>(
            () =>
                LoginAttempt.Failed(
                    string.Empty,
                    null,
                    LoginFailureReason.InvalidCredentials,
                    Now));
    }
}
