using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Domain.PasswordResets;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Tests.TestDoubles;

internal sealed class FakePasswordResetTokenRepository
    : IPasswordResetTokenRepository
{
    private readonly List<PasswordResetToken> _tokens = [];

    public IReadOnlyCollection<PasswordResetToken> Tokens =>
        _tokens.AsReadOnly();

    public PasswordResetToken? AddedToken =>
        _tokens.LastOrDefault();

    public int AddCallCount { get; private set; }

    public Task<PasswordResetToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentException.ThrowIfNullOrWhiteSpace(
            tokenHash);

        var token =
            _tokens.SingleOrDefault(
                item =>
                    item.TokenHash == tokenHash);

        return Task.FromResult(
            token);
    }

    public Task<IReadOnlyCollection<PasswordResetToken>> GetActiveByUserIdAsync(
        UserId userId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<PasswordResetToken> tokens =
            _tokens
                .Where(
                    token =>
                        token.UserId == userId &&
                        token.CanBeUsed(now))
                .ToArray();

        return Task.FromResult(
            tokens);
    }

    public Task AddAsync(
        PasswordResetToken token,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(
            token);

        AddCallCount++;

        _tokens.Add(
            token);

        return Task.CompletedTask;
    }

    public void Seed(
        PasswordResetToken token)
    {
        ArgumentNullException.ThrowIfNull(
            token);

        _tokens.Add(
            token);
    }
}
