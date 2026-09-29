using PGLN.Auth.EntityFrameworkCore.Inbox;

namespace PGLN.Auth.IntegrationTests.Inbox;

public sealed class InboxMessageTests
{
    private static readonly DateTimeOffset UtcNow =
        new(
            2026,
            9,
            28,
            12,
            0,
            0,
            TimeSpan.Zero);

    private static readonly TimeSpan ClaimDuration =
        TimeSpan.FromMinutes(2);

    [Fact]
    public void CreateClaimed_Should_Create_Active_Claim()
    {
        // Arrange
        var messageId =
            Guid.Parse(
                "9bb3b43e-bb0a-48a5-858d-c32e21a31c84");

        const string workerId =
            "worker-a";

        // Act
        var message =
            InboxMessage.CreateClaimed(
                messageId,
                workerId,
                UtcNow,
                ClaimDuration);

        // Assert
        Assert.Equal(
            messageId,
            message.Id);

        Assert.Equal(
            workerId,
            message.ClaimedBy);

        Assert.Equal(
            UtcNow,
            message.ClaimedAtUtc);

        Assert.Equal(
            UtcNow.Add(ClaimDuration),
            message.ClaimExpiresAtUtc);

        Assert.True(
            message.IsClaimed);

        Assert.False(
            message.IsProcessed);
    }

    [Fact]
    public void TryClaim_Should_Reject_Active_Claim()
    {
        // Arrange
        var message =
            CreateMessage();

        // Act
        var claimed =
            message.TryClaim(
                "worker-b",
                UtcNow.AddSeconds(30),
                ClaimDuration);

        // Assert
        Assert.False(
            claimed);

        Assert.Equal(
            "worker-a",
            message.ClaimedBy);
    }

    [Fact]
    public void TryClaim_Should_Allow_Expired_Claim_To_Be_Taken_Over()
    {
        // Arrange
        var message =
            CreateMessage();

        var takeoverAtUtc =
            UtcNow.Add(ClaimDuration);

        // Act
        var claimed =
            message.TryClaim(
                "worker-b",
                takeoverAtUtc,
                ClaimDuration);

        // Assert
        Assert.True(
            claimed);

        Assert.Equal(
            "worker-b",
            message.ClaimedBy);

        Assert.Equal(
            takeoverAtUtc,
            message.ClaimedAtUtc);

        Assert.Equal(
            takeoverAtUtc.Add(ClaimDuration),
            message.ClaimExpiresAtUtc);
    }

    [Fact]
    public void Release_Should_Reject_NonOwning_Worker()
    {
        // Arrange
        var message =
            CreateMessage();

        // Act
        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    message.Release(
                        "worker-b"));

        // Assert
        Assert.Equal(
            "Inbox message is not claimed by this worker.",
            exception.Message);

        Assert.True(
            message.IsClaimed);

        Assert.Equal(
            "worker-a",
            message.ClaimedBy);
    }

    [Fact]
    public void MarkProcessed_Should_Reject_NonOwning_Worker()
    {
        // Arrange
        var message =
            CreateMessage();

        // Act
        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    message.MarkProcessed(
                        "worker-b",
                        UtcNow.AddMinutes(1)));

        // Assert
        Assert.Equal(
            "Inbox message is not claimed by this worker.",
            exception.Message);

        Assert.False(
            message.IsProcessed);

        Assert.Equal(
            "worker-a",
            message.ClaimedBy);
    }

    [Fact]
    public void Release_Should_Allow_Message_To_Be_Claimed_Again()
    {
        // Arrange
        var message =
            CreateMessage();

        message.Release(
            "worker-a");

        // Act
        var claimed =
            message.TryClaim(
                "worker-b",
                UtcNow.AddSeconds(30),
                ClaimDuration);

        // Assert
        Assert.True(
            claimed);

        Assert.Equal(
            "worker-b",
            message.ClaimedBy);
    }

    [Fact]
    public void MarkProcessed_Should_Prevent_Future_Claims()
    {
        // Arrange
        var message =
            CreateMessage();

        var processedAtUtc =
            UtcNow.AddMinutes(1);

        message.MarkProcessed(
            "worker-a",
            processedAtUtc);

        // Act
        var claimed =
            message.TryClaim(
                "worker-b",
                UtcNow.AddHours(1),
                ClaimDuration);

        // Assert
        Assert.False(
            claimed);

        Assert.True(
            message.IsProcessed);

        Assert.Equal(
            processedAtUtc,
            message.ProcessedAtUtc);

        Assert.False(
            message.IsClaimed);

        Assert.Null(
            message.ClaimedBy);

        Assert.Null(
            message.ClaimedAtUtc);

        Assert.Null(
            message.ClaimExpiresAtUtc);
    }

    private static InboxMessage CreateMessage()
    {
        return InboxMessage.CreateClaimed(
            Guid.Parse(
                "0c45813a-66ec-43a9-a6ed-d2f27e4897cf"),
            "worker-a",
            UtcNow,
            ClaimDuration);
    }
}
