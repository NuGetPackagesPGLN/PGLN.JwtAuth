namespace PGLN.Auth.EntityFrameworkCore.Inbox;

public sealed class InboxMessage
{
    private InboxMessage()
    {
    }

    private InboxMessage(
        Guid id,
        string claimedBy,
        DateTimeOffset claimedAtUtc,
        DateTimeOffset claimExpiresAtUtc)
    {
        Id = id;
        ClaimedBy = claimedBy;
        ClaimedAtUtc = claimedAtUtc;
        ClaimExpiresAtUtc = claimExpiresAtUtc;
    }

    public Guid Id { get; private set; }

    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    public DateTimeOffset? ClaimedAtUtc { get; private set; }

    public DateTimeOffset? ClaimExpiresAtUtc { get; private set; }

    public string? ClaimedBy { get; private set; }

    public bool IsProcessed =>
        ProcessedAtUtc.HasValue;

    public bool IsClaimed =>
        ClaimedAtUtc.HasValue &&
        ClaimExpiresAtUtc.HasValue &&
        !string.IsNullOrWhiteSpace(ClaimedBy);

    public static InboxMessage CreateClaimed(
        Guid id,
        string workerId,
        DateTimeOffset utcNow,
        TimeSpan claimDuration)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Inbox message ID cannot be empty.",
                nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            workerId);

        if (claimDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(claimDuration));
        }

        return new InboxMessage(
            id,
            workerId,
            utcNow,
            utcNow.Add(claimDuration));
    }

    public bool CanBeClaimed(
        DateTimeOffset utcNow)
    {
        if (IsProcessed)
        {
            return false;
        }

        if (!IsClaimed)
        {
            return true;
        }

        return ClaimExpiresAtUtc <= utcNow;
    }

    public bool TryClaim(
        string workerId,
        DateTimeOffset utcNow,
        TimeSpan claimDuration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            workerId);

        if (claimDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(claimDuration));
        }

        if (!CanBeClaimed(
            utcNow))
        {
            return false;
        }

        ClaimedBy = workerId;
        ClaimedAtUtc = utcNow;
        ClaimExpiresAtUtc =
            utcNow.Add(claimDuration);

        return true;
    }

    public void MarkProcessed(
        string workerId,
        DateTimeOffset processedAtUtc)
    {
        EnsureClaimOwnedBy(
            workerId);

        if (IsProcessed)
        {
            return;
        }

        ProcessedAtUtc = processedAtUtc;

        ReleaseClaim();
    }

    public void Release(
        string workerId)
    {
        EnsureClaimOwnedBy(
            workerId);

        ReleaseClaim();
    }

    private void EnsureClaimOwnedBy(
        string workerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            workerId);

        if (!IsClaimed ||
            !string.Equals(
                ClaimedBy,
                workerId,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Inbox message is not claimed by this worker.");
        }
    }

    private void ReleaseClaim()
    {
        ClaimedBy = null;
        ClaimedAtUtc = null;
        ClaimExpiresAtUtc = null;
    }
}
