using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Domain.StepUpChallenges;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Tests.TestDoubles;

public sealed class FakeStepUpChallengeRepository
    : IStepUpChallengeRepository
{
    private readonly List<StepUpChallenge> _challenges =
        [];

    public IReadOnlyCollection<StepUpChallenge> Challenges =>
        _challenges;

    public Task<StepUpChallenge?> GetByIdAsync(
        StepUpChallengeId id,
        CancellationToken cancellationToken = default)
    {
        var challenge =
            _challenges.SingleOrDefault(
                item => item.Id == id);

        return Task.FromResult(
            challenge);
    }

    public Task<StepUpChallenge?> GetActiveByUserAndDeviceHashAsync(
        UserId userId,
        string deviceIdHash,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var challenge =
            _challenges
                .Where(
                    item =>
                        item.UserId == userId &&
                        item.DeviceIdHash == deviceIdHash &&
                        !item.IsVerified &&
                        !item.IsExpired(now))
                .OrderByDescending(
                    item =>
                        item.CreatedAtUtc)
                .FirstOrDefault();

        return Task.FromResult(
            challenge);
    }

    public Task AddAsync(
        StepUpChallenge challenge,
        CancellationToken cancellationToken = default)
    {
        _challenges.Add(
            challenge);

        return Task.CompletedTask;
    }

    public void Seed(
        StepUpChallenge challenge)
    {
        _challenges.Add(
            challenge);
    }
}
