using PGLN.Auth.Domain.ExternalLogins;

namespace PGLN.Auth.Application.Abstractions.ExternalAuthentication;

public interface IExternalAuthorizationUrlBuilderResolver
{
    IExternalAuthorizationUrlBuilder GetBuilder(
        ExternalLoginProvider provider);
}
