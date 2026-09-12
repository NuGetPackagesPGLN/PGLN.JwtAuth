using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Domain.EmailChangeTokens;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Tests.TestDoubles;

public sealed class FakeEmailChangeTokenRepository
    : IEmailChangeTokenRepository
{
    private readonly List<EmailChangeToken>
        _tokens = [];

    public void Seed(
        EmailChangeToken token)
    {
        ArgumentNullException.ThrowIfNull(
            token);

        _tokens.Add(
            token);
    }

    public Task<EmailChangeToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var token =
            _tokens.SingleOrDefault(
                x => x.TokenHash == tokenHash);

        return Task.FromResult(
            token);
    }

    public Task<IReadOnlyCollection<EmailChangeToken>> GetActiveByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<EmailChangeToken> tokens =
            _tokens
                .Where(
                    x =>
                        x.UserId == userId &&
                        !x.IsUsed)
                .ToArray();

        return Task.FromResult(
            tokens);
    }

    public Task AddAsync(
        EmailChangeToken token,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            token);

        _tokens.Add(
            token);

        return Task.CompletedTask;
    }
}
