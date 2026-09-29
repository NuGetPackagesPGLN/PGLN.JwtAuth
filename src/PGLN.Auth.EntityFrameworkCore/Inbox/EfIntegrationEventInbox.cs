using Microsoft.EntityFrameworkCore;
using PGLN.Auth.Application.Abstractions.Inbox;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.EntityFrameworkCore.Inbox;

public sealed class EfIntegrationEventInbox
    : IIntegrationEventInbox
{
    private readonly AuthDbContext _dbContext;

    public EfIntegrationEventInbox(
        AuthDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(
            dbContext);

        _dbContext = dbContext;
    }

    public async Task<IntegrationEventInboxClaimResult> TryClaimAsync(
        Guid messageId,
        string workerId,
        DateTimeOffset utcNow,
        TimeSpan claimDuration,
        CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException(
                "Inbox message ID cannot be empty.",
                nameof(messageId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            workerId);

        if (claimDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(claimDuration));
        }

        var claimExpiresAtUtc =
            utcNow.Add(claimDuration);

        var updatedRows =
            await _dbContext
                .InboxMessages
                .Where(
                    message =>
                        message.Id == messageId &&
                        message.ProcessedAtUtc == null &&
                        (
                            message.ClaimExpiresAtUtc == null ||
                            message.ClaimExpiresAtUtc <= utcNow
                        ))
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(
                                message =>
                                    message.ClaimedBy,
                                workerId)
                            .SetProperty(
                                message =>
                                    message.ClaimedAtUtc,
                                utcNow)
                            .SetProperty(
                                message =>
                                    message.ClaimExpiresAtUtc,
                                claimExpiresAtUtc),
                    cancellationToken);

        if (updatedRows == 1)
        {
            return IntegrationEventInboxClaimResult.Claimed;
        }

        var existingMessage =
            await _dbContext
                .InboxMessages
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    message =>
                        message.Id == messageId,
                    cancellationToken);

        if (existingMessage is not null)
        {
            return existingMessage.IsProcessed
                ? IntegrationEventInboxClaimResult.AlreadyProcessed
                : IntegrationEventInboxClaimResult.AlreadyClaimed;
        }

        var newMessage =
            InboxMessage.CreateClaimed(
                messageId,
                workerId,
                utcNow,
                claimDuration);

        _dbContext.InboxMessages.Add(
            newMessage);

        try
        {
            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return IntegrationEventInboxClaimResult.Claimed;
        }
        catch (DbUpdateException exception)
        {
            _dbContext.Entry(
                    newMessage)
                .State =
                EntityState.Detached;

            var racedMessage =
                await _dbContext
                    .InboxMessages
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        message =>
                            message.Id == messageId,
                        cancellationToken);

            if (racedMessage is null)
            {
                throw new InvalidOperationException(
                    "Unable to persist the inbox claim.",
                    exception);
            }

            return racedMessage.IsProcessed
                ? IntegrationEventInboxClaimResult.AlreadyProcessed
                : IntegrationEventInboxClaimResult.AlreadyClaimed;
        }
    }

    public async Task MarkProcessedAsync(
        Guid messageId,
        string workerId,
        DateTimeOffset processedAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException(
                "Inbox message ID cannot be empty.",
                nameof(messageId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            workerId);

        var updatedRows =
            await _dbContext
                .InboxMessages
                .Where(
                    message =>
                        message.Id == messageId &&
                        message.ProcessedAtUtc == null &&
                        message.ClaimedBy == workerId)
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(
                                message =>
                                    message.ProcessedAtUtc,
                                processedAtUtc)
                            .SetProperty(
                                message =>
                                    message.ClaimedBy,
                                (string?)null)
                            .SetProperty(
                                message =>
                                    message.ClaimedAtUtc,
                                (DateTimeOffset?)null)
                            .SetProperty(
                                message =>
                                    message.ClaimExpiresAtUtc,
                                (DateTimeOffset?)null),
                    cancellationToken);

        if (updatedRows != 1)
        {
            throw new InvalidOperationException(
                "Inbox message is not claimed by this worker.");
        }
    }

    public async Task ReleaseAsync(
        Guid messageId,
        string workerId,
        CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException(
                "Inbox message ID cannot be empty.",
                nameof(messageId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            workerId);

        var updatedRows =
            await _dbContext
                .InboxMessages
                .Where(
                    message =>
                        message.Id == messageId &&
                        message.ProcessedAtUtc == null &&
                        message.ClaimedBy == workerId)
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(
                                message =>
                                    message.ClaimedBy,
                                (string?)null)
                            .SetProperty(
                                message =>
                                    message.ClaimedAtUtc,
                                (DateTimeOffset?)null)
                            .SetProperty(
                                message =>
                                    message.ClaimExpiresAtUtc,
                                (DateTimeOffset?)null),
                    cancellationToken);

        if (updatedRows != 1)
        {
            throw new InvalidOperationException(
                "Inbox message is not claimed by this worker.");
        }
    }
}

