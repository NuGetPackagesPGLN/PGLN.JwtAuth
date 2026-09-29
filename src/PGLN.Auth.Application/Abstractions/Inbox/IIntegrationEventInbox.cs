namespace PGLN.Auth.Application.Abstractions.Inbox;

public interface IIntegrationEventInbox
{
    Task<IntegrationEventInboxClaimResult> TryClaimAsync(
        Guid messageId,
        string workerId,
        DateTimeOffset utcNow,
        TimeSpan claimDuration,
        CancellationToken cancellationToken = default);

    Task MarkProcessedAsync(
        Guid messageId,
        string workerId,
        DateTimeOffset processedAtUtc,
        CancellationToken cancellationToken = default);

    Task ReleaseAsync(
        Guid messageId,
        string workerId,
        CancellationToken cancellationToken = default);
}

public enum IntegrationEventInboxClaimResult
{
    Claimed = 0,
    AlreadyProcessed = 1,
    AlreadyClaimed = 2
}
