using PGLN.Auth.Application.Abstractions.ExternalAuthentication;
using PGLN.Auth.Domain.ExternalLogins;

namespace PGLN.Auth.Infrastructure.Authentication.External;

public sealed class ExternalAuthorizationUrlBuilderResolver
    : IExternalAuthorizationUrlBuilderResolver
{
    private readonly IReadOnlyDictionary<
        ExternalLoginProvider,
        IExternalAuthorizationUrlBuilder> _builders;

    public ExternalAuthorizationUrlBuilderResolver(
        IEnumerable<IExternalAuthorizationUrlBuilder> builders)
    {
        ArgumentNullException.ThrowIfNull(
            builders);

        _builders =
            builders.ToDictionary(
                builder => builder.Provider);
    }

    public IExternalAuthorizationUrlBuilder GetBuilder(
        ExternalLoginProvider provider)
    {
        if (_builders.TryGetValue(
                provider,
                out var builder))
        {
            return builder;
        }

        throw new InvalidOperationException(
            $"No external authorization URL builder is registered for '{provider}'.");
    }
}
