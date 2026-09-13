using PGLN.Auth.Domain.StepUpChallenges;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Domain.Tests.StepUpChallenges;

public sealed class StepUpChallengeTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreatePendingChallenge()
    {
        var createdAtUtc =
            new DateTimeOffset(
                2026,
                9,
                12,
                20,
                0,
                0,
                TimeSpan.Zero);

        var challenge =
            StepUpChallenge.Create(
                StepUpChallengeId.New(),
                UserId.New(),
                "device-hash-001",
                "Chrome on Windows",
                "code-hash",
                createdAtUtc,
                createdAtUtc.AddMinutes(10));

        Assert.False(
            challenge.IsVerified);

        Assert.Equal(
            0,
            challenge.FailedAttempts);

        Assert.Equal(
            "code-hash",
            challenge.CodeHash);

        Assert.False(
            challenge.IsExpired(
                createdAtUtc));
    }

    [Fact]
    public void Create_WithBlankDeviceIdHash_ShouldThrow()
    {
        var now =
            DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(
            () =>
                StepUpChallenge.Create(
                    StepUpChallengeId.New(),
                    UserId.New(),
                    " ",
                    null,
                    "code-hash",
                    now,
                    now.AddMinutes(10)));
    }

    [Fact]
    public void Create_WithBlankCodeHash_ShouldThrow()
    {
        var now =
            DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(
            () =>
                StepUpChallenge.Create(
                    StepUpChallengeId.New(),
                    UserId.New(),
                    "device-hash-001",
                    null,
                    " ",
                    now,
                    now.AddMinutes(10)));
    }

    [Fact]
    public void Create_WithInvalidExpiry_ShouldThrow()
    {
        var now =
            DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(
            () =>
                StepUpChallenge.Create(
                    StepUpChallengeId.New(),
                    UserId.New(),
                    "device-hash-001",
                    null,
                    "code-hash",
                    now,
                    now));
    }

    [Fact]
    public void RecordFailedAttempt_ShouldIncrementCount()
    {
        var now =
            DateTimeOffset.UtcNow;

        var challenge =
            StepUpChallenge.Create(
                StepUpChallengeId.New(),
                UserId.New(),
                "device-hash-001",
                null,
                "code-hash",
                now,
                now.AddMinutes(10));

        challenge.RecordFailedAttempt();

        Assert.Equal(
            1,
            challenge.FailedAttempts);
    }

    [Fact]
    public void MarkVerified_BeforeExpiry_ShouldVerifyChallenge()
    {
        var now =
            DateTimeOffset.UtcNow;

        var challenge =
            StepUpChallenge.Create(
                StepUpChallengeId.New(),
                UserId.New(),
                "device-hash-001",
                null,
                "code-hash",
                now,
                now.AddMinutes(10));

        challenge.MarkVerified(
            now.AddMinutes(1));

        Assert.True(
            challenge.IsVerified);

        Assert.Equal(
            now.AddMinutes(1),
            challenge.VerifiedAtUtc);
    }

    [Fact]
    public void MarkVerified_AfterExpiry_ShouldThrow()
    {
        var now =
            DateTimeOffset.UtcNow;

        var challenge =
            StepUpChallenge.Create(
                StepUpChallengeId.New(),
                UserId.New(),
                "device-hash-001",
                null,
                "code-hash",
                now,
                now.AddMinutes(10));

        Assert.Throws<InvalidOperationException>(
            () =>
                challenge.MarkVerified(
                    now.AddMinutes(10)));
    }
}
