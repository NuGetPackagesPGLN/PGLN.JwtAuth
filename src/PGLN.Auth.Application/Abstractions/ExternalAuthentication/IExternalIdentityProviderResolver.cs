using PGLN.Auth.Domain.ExternalLogins;

namespace PGLN.Auth.Application.Abstractions.ExternalAuthentication;

public interface IExternalIdentityProviderResolver
{
    IExternalIdentityProvider GetProvider(
        ExternalLoginProvider provider);
}
