using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Domain.LoginAttempts;

namespace PGLN.Auth.Application.Tests.TestDoubles;

internal sealed class FakeLoginAttemptRepository
    : ILoginAttemptRepository
{
    private readonly List<LoginAttempt> _attempts = [];

    public IReadOnlyCollection<LoginAttempt> Attempts =>
        _attempts;

    public Task AddAsync(
        LoginAttempt loginAttempt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            loginAttempt);

        _attempts.Add(
            loginAttempt);

        return Task.CompletedTask;
    }
}
