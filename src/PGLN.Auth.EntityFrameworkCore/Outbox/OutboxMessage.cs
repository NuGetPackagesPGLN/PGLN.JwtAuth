namespace PGLN.Auth.EntityFrameworkCore.Outbox;

public sealed class OutboxMessage
{
    private OutboxMessage()
    {
        Type = string.Empty;
        Payload = string.Empty;
    }

    private OutboxMessage(
        Guid id,
        string type,
        string payload,
        DateTimeOffset occurredAtUtc)
    {
        Id = id;
        Type = type;
        Payload = payload;
        OccurredAtUtc = occurredAtUtc;
        NextAttemptAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }

    public string Type { get; private set; }

    public string Payload { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    public DateTimeOffset? NextAttemptAtUtc { get; private set; }

    public DateTimeOffset? DeadLetteredAtUtc { get; private set; }

    public DateTimeOffset? ClaimedAtUtc { get; private set; }

    public DateTimeOffset? ClaimExpiresAtUtc { get; private set; }

    public string? ClaimedBy { get; private set; }

    public int AttemptCount { get; private set; }

    public string? LastError { get; private set; }

    public bool IsProcessed =>
        ProcessedAtUtc.HasValue;

    public bool IsDeadLettered =>
        DeadLetteredAtUtc.HasValue;

    public bool IsClaimed =>
        ClaimedAtUtc.HasValue &&
        ClaimExpiresAtUtc.HasValue;

    public static OutboxMessage Create(
        Guid id,
        string type,
        string payload,
        DateTimeOffset occurredAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Outbox message ID cannot be empty.",
                nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        return new OutboxMessage(
            id,
            type,
            payload,
            occurredAtUtc);
    }

    public bool CanBeClaimed(
        DateTimeOffset utcNow)
    {
        if (IsProcessed || IsDeadLettered)
        {
            return false;
        }

        if (NextAttemptAtUtc.HasValue &&
            NextAttemptAtUtc.Value > utcNow)
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
        ArgumentException.ThrowIfNullOrWhiteSpace(workerId);

        if (claimDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(claimDuration));
        }

        if (!CanBeClaimed(utcNow))
        {
            return false;
        }

        ClaimedBy = workerId;
        ClaimedAtUtc = utcNow;
        ClaimExpiresAtUtc = utcNow.Add(claimDuration);

        return true;
    }

    public void RecordFailure(
        string error,
        DateTimeOffset utcNow,
        int maximumAttempts,
        TimeSpan retryDelay)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        if (maximumAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumAttempts));
        }

        if (retryDelay <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(retryDelay));
        }

        AttemptCount++;
        LastError = error;

        ReleaseClaim();

        if (AttemptCount >= maximumAttempts)
        {
            DeadLetteredAtUtc = utcNow;
            NextAttemptAtUtc = null;

            return;
        }

        NextAttemptAtUtc =
            utcNow.Add(retryDelay);
    }

    public void MarkProcessed(
        DateTimeOffset processedAtUtc)
    {
        if (IsProcessed)
        {
            return;
        }

        AttemptCount++;
        ProcessedAtUtc = processedAtUtc;
        LastError = null;
        NextAttemptAtUtc = null;

        ReleaseClaim();
    }

    private void ReleaseClaim()
    {
        ClaimedBy = null;
        ClaimedAtUtc = null;
        ClaimExpiresAtUtc = null;
    }
}
