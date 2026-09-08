using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Domain.RefreshTokens;

namespace PGLN.Auth.Application.Tests.TestDoubles;

internal sealed class FakeRefreshTokenRepository
    : IRefreshTokenRepository
{
    private readonly List<RefreshToken> _tokens = [];

    public IReadOnlyCollection<RefreshToken> Tokens =>
        _tokens;

    public RefreshToken? AddedToken =>
        _tokens.LastOrDefault();

    public Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            tokenHash);

        return Task.FromResult(
            _tokens.SingleOrDefault(
                token =>
                    token.TokenHash == tokenHash));
    }

    public Task AddAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            refreshToken);

        _tokens.Add(
            refreshToken);

        return Task.CompletedTask;
    }

    public void Seed(
        RefreshToken refreshToken)
    {
        ArgumentNullException.ThrowIfNull(
            refreshToken);

        _tokens.Add(
            refreshToken);
    }
}

