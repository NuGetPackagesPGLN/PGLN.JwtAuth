using Microsoft.EntityFrameworkCore;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Domain.StepUpChallenges;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.EntityFrameworkCore.Persistence.Repositories;

public sealed class StepUpChallengeRepository
    : IStepUpChallengeRepository
{
    private readonly AuthDbContext _dbContext;

    public StepUpChallengeRepository(
        AuthDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(
            dbContext);

        _dbContext = dbContext;
    }

    public Task<StepUpChallenge?> GetByIdAsync(
        StepUpChallengeId id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext
            .StepUpChallenges
            .SingleOrDefaultAsync(
                challenge =>
                    challenge.Id == id,
                cancellationToken);
    }

    public async Task<StepUpChallenge?> GetActiveByUserAndDeviceHashAsync(
        UserId userId,
        string deviceIdHash,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            deviceIdHash);

        var challenges =
            await _dbContext
                .StepUpChallenges
                .Where(
                    challenge =>
                        challenge.UserId == userId &&
                        challenge.DeviceIdHash == deviceIdHash &&
                        challenge.VerifiedAtUtc == null)
                .ToListAsync(
                    cancellationToken);

        return challenges
            .Where(
                challenge =>
                    !challenge.IsExpired(now))
            .OrderByDescending(
                challenge =>
                    challenge.CreatedAtUtc)
            .FirstOrDefault();
    }

    public async Task AddAsync(
        StepUpChallenge challenge,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            challenge);

        await _dbContext
            .StepUpChallenges
            .AddAsync(
                challenge,
                cancellationToken);
    }
}
