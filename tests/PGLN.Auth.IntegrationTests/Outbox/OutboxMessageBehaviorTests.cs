using PGLN.Auth.EntityFrameworkCore.Outbox;

namespace PGLN.Auth.IntegrationTests.Outbox;

public sealed class OutboxMessageBehaviorTests
{
    private static readonly DateTimeOffset Now =
        new(
            2026,
            9,
            6,
            18,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public void TryClaim_WhenUnclaimed_ShouldClaimMessage()
    {
        var message =
            CreateMessage();

        var claimed =
            message.TryClaim(
                "worker-a",
                Now,
                TimeSpan.FromMinutes(2));

        Assert.True(claimed);
        Assert.Equal("worker-a", message.ClaimedBy);
        Assert.Equal(Now, message.ClaimedAtUtc);
        Assert.Equal(
            Now.AddMinutes(2),
            message.ClaimExpiresAtUtc);
    }

    [Fact]
    public void TryClaim_WhenClaimStillActive_ShouldFail()
    {
        var message =
            CreateMessage();

        message.TryClaim(
            "worker-a",
            Now,
            TimeSpan.FromMinutes(2));

        var claimed =
            message.TryClaim(
                "worker-b",
                Now.AddMinutes(1),
                TimeSpan.FromMinutes(2));

        Assert.False(claimed);
        Assert.Equal("worker-a", message.ClaimedBy);
    }

    [Fact]
    public void TryClaim_WhenClaimExpired_ShouldAllowRecovery()
    {
        var message =
            CreateMessage();

        message.TryClaim(
            "worker-a",
            Now,
            TimeSpan.FromMinutes(1));

        var claimed =
            message.TryClaim(
                "worker-b",
                Now.AddMinutes(2),
                TimeSpan.FromMinutes(1));

        Assert.True(claimed);
        Assert.Equal("worker-b", message.ClaimedBy);
    }

    [Fact]
    public void RecordFailure_ShouldScheduleRetry()
    {
        var message =
            CreateMessage();

        message.TryClaim(
            "worker-a",
            Now,
            TimeSpan.FromMinutes(1));

        message.RecordFailure(
            "temporary failure",
            Now,
            maximumAttempts: 5,
            retryDelay: TimeSpan.FromSeconds(30));

        Assert.Equal(1, message.AttemptCount);
        Assert.Equal(
            Now.AddSeconds(30),
            message.NextAttemptAtUtc);

        Assert.False(message.IsDeadLettered);
        Assert.Null(message.ClaimedBy);
    }

    [Fact]
    public void RecordFailure_WhenMaximumAttemptsReached_ShouldDeadLetter()
    {
        var message =
            CreateMessage();

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            message.TryClaim(
                $"worker-{attempt}",
                Now.AddMinutes(attempt),
                TimeSpan.FromSeconds(30));

            message.RecordFailure(
                $"failure-{attempt}",
                Now.AddMinutes(attempt),
                maximumAttempts: 3,
                retryDelay: TimeSpan.FromSeconds(10));
        }

        Assert.True(message.IsDeadLettered);
        Assert.Equal(3, message.AttemptCount);
        Assert.Equal(
            Now.AddMinutes(3),
            message.DeadLetteredAtUtc);

        Assert.Null(message.NextAttemptAtUtc);
    }

    [Fact]
    public void MarkProcessed_ShouldReleaseClaim()
    {
        var message =
            CreateMessage();

        message.TryClaim(
            "worker-a",
            Now,
            TimeSpan.FromMinutes(1));

        message.MarkProcessed(
            Now.AddSeconds(5));

        Assert.True(message.IsProcessed);
        Assert.Null(message.ClaimedBy);
        Assert.Null(message.ClaimedAtUtc);
        Assert.Null(message.ClaimExpiresAtUtc);
        Assert.Null(message.NextAttemptAtUtc);
    }

    private static OutboxMessage CreateMessage()
    {
        return OutboxMessage.Create(
            Guid.NewGuid(),
            "Test.Event",
            "protected-payload",
            Now);
    }
}
