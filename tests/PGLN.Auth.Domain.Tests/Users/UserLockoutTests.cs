using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Domain.Tests.Users;

public sealed class UserLockoutTests
{
    private static readonly DateTimeOffset Now =
        new(
            2026,
            9,
            10,
            15,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public void RecordFailedLoginAttempt_ShouldIncrementFailedAttempts()
    {
        var user =
            CreateUser();

        user.RecordFailedLoginAttempt(
            Now);

        Assert.Equal(
            1,
            user.FailedLoginAttempts);

        Assert.Equal(
            Now,
            user.LastFailedLoginAtUtc);
    }

    [Fact]
    public void RecordFailedLoginAttempt_MultipleTimes_ShouldIncrementEachTime()
    {
        var user =
            CreateUser();

        user.RecordFailedLoginAttempt(
            Now.AddMinutes(-2));

        user.RecordFailedLoginAttempt(
            Now.AddMinutes(-1));

        user.RecordFailedLoginAttempt(
            Now);

        Assert.Equal(
            3,
            user.FailedLoginAttempts);

        Assert.Equal(
            Now,
            user.LastFailedLoginAtUtc);
    }

    [Fact]
    public void LockOutUntil_WithFutureTime_ShouldMarkUserAsLockedOut()
    {
        var user =
            CreateUser();

        var lockoutEnd =
            Now.AddMinutes(15);

        user.LockOutUntil(
            lockoutEnd);

        Assert.True(
            user.IsLockedOut(
                Now));

        Assert.Equal(
            lockoutEnd,
            user.LockoutEndUtc);
    }

    [Fact]
    public void IsLockedOut_AfterLockoutExpires_ShouldReturnFalse()
    {
        var user =
            CreateUser();

        user.LockOutUntil(
            Now.AddMinutes(15));

        var afterLockout =
            Now.AddMinutes(16);

        Assert.False(
            user.IsLockedOut(
                afterLockout));
    }

    [Fact]
    public void IsLockedOut_AtExactLockoutEnd_ShouldReturnFalse()
    {
        var user =
            CreateUser();

        var lockoutEnd =
            Now.AddMinutes(15);

        user.LockOutUntil(
            lockoutEnd);

        Assert.False(
            user.IsLockedOut(
                lockoutEnd));
    }

    [Fact]
    public void ResetFailedLoginAttempts_ShouldClearFailureState()
    {
        var user =
            CreateUser();

        user.RecordFailedLoginAttempt(
            Now.AddMinutes(-2));

        user.RecordFailedLoginAttempt(
            Now.AddMinutes(-1));

        user.LockOutUntil(
            Now.AddMinutes(15));

        user.ResetFailedLoginAttempts();

        Assert.Equal(
            0,
            user.FailedLoginAttempts);

        Assert.Null(
            user.LastFailedLoginAtUtc);

        Assert.Null(
            user.LockoutEndUtc);

        Assert.False(
            user.IsLockedOut(
                Now));
    }

    private static User CreateUser()
    {
        var user =
            User.Register(
                UserId.New(),
                Email.Create(
                    "user@example.com"),
                "hashed-password",
                Now.AddDays(-30));

        user.ClearDomainEvents();

        return user;
    }
}
