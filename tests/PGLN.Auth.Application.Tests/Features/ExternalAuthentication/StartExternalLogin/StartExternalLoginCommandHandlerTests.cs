using PGLN.Auth.Application.Abstractions.ExternalAuthentication;
using PGLN.Auth.Application.Features.ExternalAuthentication;
using PGLN.Auth.Application.Features.ExternalAuthentication.StartExternalLogin;
using PGLN.Auth.Domain.ExternalLogins;

namespace PGLN.Auth.Application.Tests.Features.ExternalAuthentication.StartExternalLogin;

public sealed class StartExternalLoginCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenProviderIsSupported_ShouldReturnAuthorizationUrl()
    {
        var urlBuilder =
            new FakeAuthorizationUrlBuilder(
                ExternalLoginProvider.Google);

        var resolver =
            new FakeAuthorizationUrlBuilderResolver(
                urlBuilder);

        var pkceGenerator =
            new FakePkceGenerator(
                new PkcePair(
                    "test-code-verifier",
                    "test-code-challenge"));

        var stateProtector =
            new FakeExternalAuthenticationStateProtector();

        var handler =
            new StartExternalLoginCommandHandler(
                resolver,
                pkceGenerator,
                stateProtector);

        var command =
            new StartExternalLoginCommand(
                ExternalLoginProvider.Google,
                "https://example.com/auth/google/callback",
                "test-device-id-hash",
                "Test Device");

        var result =
            await handler.HandleAsync(
                command);

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            "https://provider.test/authorize",
            result.Value.AuthorizationUrl);

        Assert.Equal(
            ExternalLoginProvider.Google,
            resolver.LastProvider);

        Assert.Equal(
            "Google",
            stateProtector.LastProvider);

        Assert.Equal(
            command.RedirectUri,
            stateProtector.LastRedirectUri);

        Assert.Equal(
            "test-code-verifier",
            stateProtector.LastCodeVerifier);

        Assert.Equal(
            "test-device-id-hash",
            stateProtector.LastDeviceIdHash);

        Assert.Equal(
            "Test Device",
            stateProtector.LastDeviceName);

        Assert.Equal(
            "test-code-challenge",
            urlBuilder.LastCodeChallenge);

        Assert.Equal(
            "protected-state",
            urlBuilder.LastState);
    }

    [Fact]
    public async Task HandleAsync_WhenProviderIsNotSupported_ShouldReturnFailure()
    {
        var resolver =
            new FakeAuthorizationUrlBuilderResolver();

        var pkceGenerator =
            new FakePkceGenerator(
                new PkcePair(
                    "test-code-verifier",
                    "test-code-challenge"));

        var stateProtector =
            new FakeExternalAuthenticationStateProtector();

        var handler =
            new StartExternalLoginCommandHandler(
                resolver,
                pkceGenerator,
                stateProtector);

        var command =
            new StartExternalLoginCommand(
                ExternalLoginProvider.Google,
                "https://example.com/auth/google/callback",
                "test-device-id-hash",
                "Test Device");

        var result =
            await handler.HandleAsync(
                command);

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ExternalAuthenticationErrors.ProviderNotSupported,
            result.Error);
    }

    private sealed class FakeAuthorizationUrlBuilder
        : IExternalAuthorizationUrlBuilder
    {
        public FakeAuthorizationUrlBuilder(
            ExternalLoginProvider provider)
        {
            Provider = provider;
        }

        public ExternalLoginProvider Provider { get; }

        public string? LastRedirectUri { get; private set; }

        public string? LastState { get; private set; }

        public string? LastCodeChallenge { get; private set; }

        public string Build(
            string redirectUri,
            string state,
            string codeChallenge)
        {
            LastRedirectUri = redirectUri;
            LastState = state;
            LastCodeChallenge = codeChallenge;

            return "https://provider.test/authorize";
        }
    }

    private sealed class FakeAuthorizationUrlBuilderResolver
        : IExternalAuthorizationUrlBuilderResolver
    {
        private readonly IExternalAuthorizationUrlBuilder? _builder;

        public FakeAuthorizationUrlBuilderResolver(
            IExternalAuthorizationUrlBuilder? builder = null)
        {
            _builder = builder;
        }

        public ExternalLoginProvider? LastProvider { get; private set; }

        public IExternalAuthorizationUrlBuilder GetBuilder(
            ExternalLoginProvider provider)
        {
            LastProvider = provider;

            if (_builder is null ||
                _builder.Provider != provider)
            {
                throw new InvalidOperationException(
                    "Provider is not registered.");
            }

            return _builder;
        }
    }

    private sealed class FakePkceGenerator
        : IPkceGenerator
    {
        private readonly PkcePair _pair;

        public FakePkceGenerator(
            PkcePair pair)
        {
            _pair = pair;
        }

        public PkcePair Generate()
        {
            return _pair;
        }
    }

    private sealed class FakeExternalAuthenticationStateProtector
        : IExternalAuthenticationStateProtector
    {
        public string? LastProvider { get; private set; }

        public string? LastRedirectUri { get; private set; }

        public string? LastCodeVerifier { get; private set; }

        public string? LastDeviceIdHash { get; private set; }

        public string? LastDeviceName { get; private set; }

        public string Protect(
            string provider,
            string redirectUri,
            string codeVerifier,
            string deviceIdHash,
            string? deviceName)
        {
            LastProvider = provider;
            LastRedirectUri = redirectUri;
            LastCodeVerifier = codeVerifier;
            LastDeviceIdHash = deviceIdHash;
            LastDeviceName = deviceName;

            return "protected-state";
        }

        public ExternalAuthenticationState Unprotect(
            string protectedState)
        {
            throw new NotSupportedException();
        }
    }
}
