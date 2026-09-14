using PGLN.Auth.Application.Abstractions.ExternalAuthentication;
using PGLN.Auth.Domain.ExternalLogins;

namespace PGLN.Auth.Application.Tests.TestDoubles;

internal sealed class FakeExternalIdentityProviderResolver
    : IExternalIdentityProviderResolver
{
    private readonly Dictionary<
        ExternalLoginProvider,
        IExternalIdentityProvider> _providers = [];

    public void Register(
        IExternalIdentityProvider provider)
    {
        ArgumentNullException.ThrowIfNull(
            provider);

        _providers[provider.Provider] =
            provider;
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
