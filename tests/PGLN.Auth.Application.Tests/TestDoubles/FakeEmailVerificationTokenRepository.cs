using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.Domain.VerificationTokens;

namespace PGLN.Auth.Application.Tests.TestDoubles;

internal sealed class FakeEmailVerificationTokenRepository
    : IEmailVerificationTokenRepository
{
    private readonly List<EmailVerificationToken> _tokens = [];

    public IReadOnlyCollection<EmailVerificationToken> Tokens =>
        _tokens.AsReadOnly();

    public EmailVerificationToken? AddedToken { get; private set; }

    public Task<EmailVerificationToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var token =
            _tokens.SingleOrDefault(
                item =>
                    item.TokenHash == tokenHash);

        return Task.FromResult(token);
    }

    public Task<IReadOnlyCollection<EmailVerificationToken>> GetActiveByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<EmailVerificationToken> tokens =
            _tokens
                .Where(
                    item =>
                        item.UserId == userId &&
                        !item.IsUsed)
                .ToArray();

        return Task.FromResult(tokens);
    }

    public Task AddAsync(
        EmailVerificationToken token,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(token);

        _tokens.Add(token);

        AddedToken = token;

        return Task.CompletedTask;
    }

    public void Seed(
        EmailVerificationToken token)
    {
        ArgumentNullException.ThrowIfNull(token);

        _tokens.Add(token);
    }
}
