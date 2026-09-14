using PGLN.Auth.Application.Abstractions.ExternalAuthentication;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.ExternalAuthentication.StartExternalLogin;

public sealed class StartExternalLoginCommandHandler
    : ICommandHandler<
        StartExternalLoginCommand,
        Result<StartExternalLoginResult>>
{
    private readonly IExternalAuthorizationUrlBuilderResolver
        _authorizationUrlBuilderResolver;

    private readonly IPkceGenerator
        _pkceGenerator;

    private readonly IExternalAuthenticationStateProtector
        _stateProtector;

    public StartExternalLoginCommandHandler(
        IExternalAuthorizationUrlBuilderResolver authorizationUrlBuilderResolver,
        IPkceGenerator pkceGenerator,
        IExternalAuthenticationStateProtector stateProtector)
    {
        _authorizationUrlBuilderResolver =
            authorizationUrlBuilderResolver
            ?? throw new ArgumentNullException(
                nameof(authorizationUrlBuilderResolver));

        _pkceGenerator =
            pkceGenerator
            ?? throw new ArgumentNullException(
                nameof(pkceGenerator));

        _stateProtector =
            stateProtector
            ?? throw new ArgumentNullException(
                nameof(stateProtector));
    }

    public Task<Result<StartExternalLoginResult>> HandleAsync(
        StartExternalLoginCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        cancellationToken.ThrowIfCancellationRequested();

        IExternalAuthorizationUrlBuilder authorizationUrlBuilder;

        try
        {
            authorizationUrlBuilder =
                _authorizationUrlBuilderResolver.GetBuilder(
                    command.Provider);
        }
        catch (InvalidOperationException)
        {
            return Task.FromResult(
                Result<StartExternalLoginResult>.Failure(
                    ExternalAuthenticationErrors.ProviderNotSupported));
        }

        var pkce =
            _pkceGenerator.Generate();

        var state =
            _stateProtector.Protect(
                command.Provider.ToString(),
                command.RedirectUri,
                pkce.CodeVerifier);

        string authorizationUrl;

        try
        {
            authorizationUrl =
                authorizationUrlBuilder.Build(
                    command.RedirectUri,
                    state,
                    pkce.CodeChallenge);
        }
        catch (InvalidOperationException)
        {
            return Task.FromResult(
                Result<StartExternalLoginResult>.Failure(
                    ExternalAuthenticationErrors.RedirectUriNotAllowed));
        }

        return Task.FromResult(
            Result<StartExternalLoginResult>.Success(
                new StartExternalLoginResult(
                    authorizationUrl)));
    }
}


