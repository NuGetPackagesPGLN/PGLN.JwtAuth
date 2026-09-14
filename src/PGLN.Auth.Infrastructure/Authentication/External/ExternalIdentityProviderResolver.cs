using PGLN.Auth.Application.Abstractions.ExternalAuthentication;
using PGLN.Auth.Domain.ExternalLogins;

namespace PGLN.Auth.Infrastructure.Authentication.External;

public sealed class ExternalIdentityProviderResolver
    : IExternalIdentityProviderResolver
{
    private readonly IReadOnlyDictionary<
        ExternalLoginProvider,
        IExternalIdentityProvider> _providers;

    public ExternalIdentityProviderResolver(
        IEnumerable<IExternalIdentityProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(
            providers);

        _providers =
            providers.ToDictionary(
                provider => provider.Provider);
    }

    public IExternalIdentityProvider GetProvider(
        ExternalLoginProvider provider)
    {
        if (_providers.TryGetValue(
                provider,
                out var identityProvider))
        {
            return identityProvider;
        }

        throw new InvalidOperationException(
            $"No external identity provider is registered for '{provider}'.");
    }
}
