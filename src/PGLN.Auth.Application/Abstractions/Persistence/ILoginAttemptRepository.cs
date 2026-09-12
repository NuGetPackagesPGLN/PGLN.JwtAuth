using PGLN.Auth.Domain.LoginAttempts;

namespace PGLN.Auth.Application.Abstractions.Persistence;

public interface ILoginAttemptRepository
{
    Task AddAsync(
        LoginAttempt loginAttempt,
        CancellationToken cancellationToken = default);

    Task<int> CountFailedAttemptsAsync(
        string normalizedEmail,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken = default);
}

