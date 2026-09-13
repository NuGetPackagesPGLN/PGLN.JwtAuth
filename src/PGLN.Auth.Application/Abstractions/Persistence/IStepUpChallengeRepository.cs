using PGLN.Auth.Domain.StepUpChallenges;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Abstractions.Persistence;

public interface IStepUpChallengeRepository
{
    Task<StepUpChallenge?> GetByIdAsync(
        StepUpChallengeId id,
        CancellationToken cancellationToken = default);

    Task<StepUpChallenge?> GetActiveByUserAndDeviceHashAsync(
        UserId userId,
        string deviceIdHash,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        StepUpChallenge challenge,
        CancellationToken cancellationToken = default);
}
