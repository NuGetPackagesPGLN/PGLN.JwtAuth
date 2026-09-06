using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Tests.TestDoubles;

internal sealed class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = [];

    public IReadOnlyCollection<User> Users =>
        _users.AsReadOnly();

    public User? AddedUser { get; private set; }

    public Task<User?> GetByIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = _users.SingleOrDefault(
            x => x.Id == userId);

        return Task.FromResult(user);
    }

    public Task<User?> GetByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = _users.SingleOrDefault(
            x => x.Email.NormalizedValue == normalizedEmail);

        return Task.FromResult(user);
    }

    public Task<bool> ExistsByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var exists = _users.Any(
            x => x.Email.NormalizedValue == normalizedEmail);

        return Task.FromResult(exists);
    }

    public Task AddAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(user);

        _users.Add(user);
        AddedUser = user;

        return Task.CompletedTask;
    }

    public void Seed(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        _users.Add(user);
    }
}
