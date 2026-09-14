using PGLN.Auth.Domain.ExternalLogins;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Tests.TestDoubles;

internal sealed class FakeExternalLoginRepository
    : IExternalLoginRepository
{
    private readonly List<ExternalLogin> _externalLogins = [];

    public IReadOnlyCollection<ExternalLogin> ExternalLogins =>
        _externalLogins.AsReadOnly();

    public ExternalLogin? AddedExternalLogin { get; private set; }

    public Task<ExternalLogin?> GetByIdAsync(
        ExternalLoginId id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var externalLogin =
            _externalLogins.SingleOrDefault(
                x => x.Id == id);

        return Task.FromResult(
            externalLogin);
    }

    public Task<ExternalLogin?> GetByProviderAndSubjectAsync(
        ExternalLoginProvider provider,
        string providerSubject,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var externalLogin =
            _externalLogins.SingleOrDefault(
                x =>
                    x.Provider == provider &&
                    x.ProviderSubject == providerSubject);

        return Task.FromResult(
            externalLogin);
    }

    public Task<IReadOnlyCollection<ExternalLogin>> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<ExternalLogin> result =
            _externalLogins
                .Where(x => x.UserId == userId)
                .ToArray();

        return Task.FromResult(
            result);
    }

    public Task AddAsync(
        ExternalLogin externalLogin,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(
            externalLogin);

        _externalLogins.Add(
            externalLogin);

        AddedExternalLogin =
            externalLogin;

        return Task.CompletedTask;
    }

    public void Seed(
        ExternalLogin externalLogin)
    {
        ArgumentNullException.ThrowIfNull(
            externalLogin);

        _externalLogins.Add(
            externalLogin);
    }
}
