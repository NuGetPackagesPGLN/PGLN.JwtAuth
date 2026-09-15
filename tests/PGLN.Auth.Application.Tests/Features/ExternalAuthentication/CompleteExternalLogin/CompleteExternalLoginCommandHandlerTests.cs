using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Abstractions.ExternalAuthentication;
using PGLN.Auth.Application.Features.ExternalAuthentication;
using PGLN.Auth.Application.Features.ExternalAuthentication.CompleteExternalLogin;
using PGLN.Auth.Application.Tests.TestDoubles;
using PGLN.Auth.Domain.ExternalLogins;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Tests.Features.ExternalAuthentication.CompleteExternalLogin;

public sealed class CompleteExternalLoginCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenIdentityIsNew_ShouldCreateExternalUserAndIssueTokens()
    {
        var now =
            new DateTimeOffset(
                2026,
                9,
                13,
                18,
                0,
                0,
                TimeSpan.Zero);

        var provider =
            new FakeExternalIdentityProvider(
                ExternalLoginProvider.Google)
            {
                Identity =
                    new(
                        ExternalLoginProvider.Google,
                        "google-user-123",
                        "new.user@example.com",
                        true,
                        "New User")
            };

        var resolver =
            new FakeExternalIdentityProviderResolver();

        resolver.Register(
            provider);

        var stateProtector =
            new FakeExternalAuthenticationStateProtector();

        var externalLoginRepository =
            new FakeExternalLoginRepository();

        var userRepository =
            new FakeUserRepository();

        var authSessionRepository =
            new FakeAuthSessionRepository();

        var refreshTokenRepository =
            new FakeRefreshTokenRepository();

        var accessTokenGenerator =
            new FakeAccessTokenGenerator();

        var refreshTokenGenerator =
            new FakeRefreshTokenGenerator();

        var tokenHasher =
            new FakeTokenHasher();

        var unitOfWork =
            new FakeUnitOfWork();

        var clock =
            new FakeClock(
                now);

        var refreshTokenOptions =
            new RefreshTokenOptions
            {
                TokenLifetime =
                    TimeSpan.FromDays(30)
            };

        var handler =
            new CompleteExternalLoginCommandHandler(
                resolver,
                stateProtector,
                externalLoginRepository,
                userRepository,
                authSessionRepository,
                refreshTokenRepository,
                accessTokenGenerator,
                refreshTokenGenerator,
                tokenHasher,
                unitOfWork,
                clock,
                refreshTokenOptions);

        var command =
            new CompleteExternalLoginCommand(
                ExternalLoginProvider.Google,
                "authorization-code",
                "protected-state",
                "https://localhost/signin-google",
                "device-hash-123",
                "Chrome on Windows",
                "127.0.0.1",
                "Chrome/1.0",
                null);

        var result =
            await handler.HandleAsync(
                command);

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            "protected-state",
            stateProtector.LastProtectedState);

        Assert.Equal(
            "test-code-verifier",
            provider.LastCodeVerifier);

        Assert.NotNull(
            result.Value);

        Assert.True(
            result.Value.IsNewUser);

        Assert.Equal(
            "new.user@example.com",
            result.Value.Email);

        Assert.Equal(
            "fake-access-token",
            result.Value.AccessToken);

        Assert.Equal(
            "raw-refresh-token",
            result.Value.RefreshToken);

        Assert.NotNull(
            userRepository.AddedUser);

        Assert.True(
            userRepository.AddedUser.EmailConfirmed);

        Assert.False(
            userRepository.AddedUser.HasPassword);

        Assert.Single(
            externalLoginRepository.ExternalLogins);

        var externalLogin =
            externalLoginRepository.ExternalLogins.Single();

        Assert.Equal(
            ExternalLoginProvider.Google,
            externalLogin.Provider);

        Assert.Equal(
            "google-user-123",
            externalLogin.ProviderSubject);

        Assert.Equal(
            userRepository.AddedUser.Id,
            externalLogin.UserId);

        Assert.Single(
            authSessionRepository.Sessions);

        var session =
            authSessionRepository.Sessions.Single();

        Assert.Equal(
            "device-hash-123",
            session.DeviceIdHash);

        Assert.True(
            session.IsTrustedDevice);

        Assert.Single(
            refreshTokenRepository.Tokens);

        Assert.Equal(
            session.Id,
            refreshTokenRepository.Tokens.Single().SessionId);

        Assert.Equal(
            1,
            accessTokenGenerator.GenerateCallCount);

        Assert.Equal(
            1,
            refreshTokenGenerator.GenerateCallCount);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenExternalLoginAlreadyExists_ShouldUseLinkedUserAndIssueTokens()
    {
        var now =
            new DateTimeOffset(
                2026,
                9,
                13,
                19,
                0,
                0,
                TimeSpan.Zero);

        var provider =
            new FakeExternalIdentityProvider(
                ExternalLoginProvider.Google)
            {
                Identity =
                    new(
                        ExternalLoginProvider.Google,
                        "google-user-123",
                        "existing.user@example.com",
                        true,
                        "Existing User")
            };

        var resolver =
            new FakeExternalIdentityProviderResolver();

        resolver.Register(
            provider);

        var stateProtector =
            new FakeExternalAuthenticationStateProtector();

        var userRepository =
            new FakeUserRepository();

        var existingUser =
            User.RegisterExternal(
                UserId.New(),
                Email.Create("existing.user@example.com"),
                now.AddDays(-10));

        userRepository.Seed(
            existingUser);

        var externalLoginRepository =
            new FakeExternalLoginRepository();

        var existingExternalLogin =
            ExternalLogin.Create(
                existingUser.Id,
                ExternalLoginProvider.Google,
                "google-user-123",
                "existing.user@example.com",
                now.AddDays(-10));

        externalLoginRepository.Seed(
            existingExternalLogin);

        var authSessionRepository =
            new FakeAuthSessionRepository();

        var refreshTokenRepository =
            new FakeRefreshTokenRepository();

        var accessTokenGenerator =
            new FakeAccessTokenGenerator();

        var refreshTokenGenerator =
            new FakeRefreshTokenGenerator();

        var tokenHasher =
            new FakeTokenHasher();

        var unitOfWork =
            new FakeUnitOfWork();

        var clock =
            new FakeClock(
                now);

        var refreshTokenOptions =
            new RefreshTokenOptions
            {
                TokenLifetime =
                    TimeSpan.FromDays(30)
            };

        var handler =
            new CompleteExternalLoginCommandHandler(
                resolver,
                stateProtector,
                externalLoginRepository,
                userRepository,
                authSessionRepository,
                refreshTokenRepository,
                accessTokenGenerator,
                refreshTokenGenerator,
                tokenHasher,
                unitOfWork,
                clock,
                refreshTokenOptions);

        var command =
            new CompleteExternalLoginCommand(
                ExternalLoginProvider.Google,
                "authorization-code",
                "protected-state",
                "https://localhost/signin-google",
                "device-hash-456",
                "Chrome on Windows",
                "127.0.0.1",
                "Chrome/1.0",
                null);

        var result =
            await handler.HandleAsync(
                command);

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            "protected-state",
            stateProtector.LastProtectedState);

        Assert.Equal(
            "test-code-verifier",
            provider.LastCodeVerifier);

        Assert.NotNull(
            result.Value);

        Assert.False(
            result.Value.IsNewUser);

        Assert.Equal(
            existingUser.Id.Value,
            result.Value.UserId);

        Assert.Null(
            userRepository.AddedUser);

        Assert.Null(
            externalLoginRepository.AddedExternalLogin);

        Assert.Single(
            externalLoginRepository.ExternalLogins);

        Assert.Equal(
            now,
            existingExternalLogin.LastLoginAtUtc);

        Assert.Single(
            authSessionRepository.Sessions);

        Assert.Single(
            refreshTokenRepository.Tokens);

        Assert.Equal(
            1,
            accessTokenGenerator.GenerateCallCount);

        Assert.Equal(
            1,
            refreshTokenGenerator.GenerateCallCount);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenEmailAlreadyBelongsToExistingUser_ShouldRequireAccountLink()
    {
        var now =
            new DateTimeOffset(
                2026,
                9,
                13,
                20,
                0,
                0,
                TimeSpan.Zero);

        var provider =
            new FakeExternalIdentityProvider(
                ExternalLoginProvider.Google)
            {
                Identity =
                    new(
                        ExternalLoginProvider.Google,
                        "new-google-subject",
                        "existing.user@example.com",
                        true,
                        "Existing User")
            };

        var resolver =
            new FakeExternalIdentityProviderResolver();

        resolver.Register(
            provider);

        var stateProtector =
            new FakeExternalAuthenticationStateProtector();

        var userRepository =
            new FakeUserRepository();

        var existingUser =
            User.Register(
                UserId.New(),
                Email.Create("existing.user@example.com"),
                "existing-password-hash",
                now.AddDays(-30));

        userRepository.Seed(
            existingUser);

        var externalLoginRepository =
            new FakeExternalLoginRepository();

        var authSessionRepository =
            new FakeAuthSessionRepository();

        var refreshTokenRepository =
            new FakeRefreshTokenRepository();

        var accessTokenGenerator =
            new FakeAccessTokenGenerator();

        var refreshTokenGenerator =
            new FakeRefreshTokenGenerator();

        var tokenHasher =
            new FakeTokenHasher();

        var unitOfWork =
            new FakeUnitOfWork();

        var clock =
            new FakeClock(
                now);

        var refreshTokenOptions =
            new RefreshTokenOptions
            {
                TokenLifetime =
                    TimeSpan.FromDays(30)
            };

        var handler =
            new CompleteExternalLoginCommandHandler(
                resolver,
                stateProtector,
                externalLoginRepository,
                userRepository,
                authSessionRepository,
                refreshTokenRepository,
                accessTokenGenerator,
                refreshTokenGenerator,
                tokenHasher,
                unitOfWork,
                clock,
                refreshTokenOptions);

        var command =
            new CompleteExternalLoginCommand(
                ExternalLoginProvider.Google,
                "authorization-code",
                "protected-state",
                "https://localhost/signin-google",
                "device-hash-789",
                "Chrome on Windows",
                "127.0.0.1",
                "Chrome/1.0",
                null);

        var result =
            await handler.HandleAsync(
                command);

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ExternalAuthenticationErrors.AccountLinkRequired,
            result.Error);

        Assert.Null(
            userRepository.AddedUser);

        Assert.Empty(
            externalLoginRepository.ExternalLogins);

        Assert.Empty(
            authSessionRepository.Sessions);

        Assert.Empty(
            refreshTokenRepository.Tokens);

        Assert.Equal(
            0,
            accessTokenGenerator.GenerateCallCount);

        Assert.Equal(
            0,
            refreshTokenGenerator.GenerateCallCount);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenStateCannotBeUnprotected_ShouldReturnInvalidState()
    {
        var now =
            new DateTimeOffset(
                2026,
                9,
                14,
                8,
                0,
                0,
                TimeSpan.Zero);

        var provider =
            new FakeExternalIdentityProvider(
                ExternalLoginProvider.Google);

        var resolver =
            new FakeExternalIdentityProviderResolver();

        resolver.Register(
            provider);

        var stateProtector =
            new FakeExternalAuthenticationStateProtector
            {
                ThrowOnUnprotect = true
            };

        var externalLoginRepository =
            new FakeExternalLoginRepository();

        var userRepository =
            new FakeUserRepository();

        var authSessionRepository =
            new FakeAuthSessionRepository();

        var refreshTokenRepository =
            new FakeRefreshTokenRepository();

        var accessTokenGenerator =
            new FakeAccessTokenGenerator();

        var refreshTokenGenerator =
            new FakeRefreshTokenGenerator();

        var tokenHasher =
            new FakeTokenHasher();

        var unitOfWork =
            new FakeUnitOfWork();

        var clock =
            new FakeClock(
                now);

        var refreshTokenOptions =
            new RefreshTokenOptions
            {
                TokenLifetime =
                    TimeSpan.FromDays(30)
            };

        var handler =
            new CompleteExternalLoginCommandHandler(
                resolver,
                stateProtector,
                externalLoginRepository,
                userRepository,
                authSessionRepository,
                refreshTokenRepository,
                accessTokenGenerator,
                refreshTokenGenerator,
                tokenHasher,
                unitOfWork,
                clock,
                refreshTokenOptions);

        var command =
            new CompleteExternalLoginCommand(
                ExternalLoginProvider.Google,
                "authorization-code",
                "tampered-state",
                "https://localhost/signin-google",
                "device-hash-tampered",
                "Chrome on Windows",
                "127.0.0.1",
                "Chrome/1.0",
                null);

        var result =
            await handler.HandleAsync(
                command);

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ExternalAuthenticationErrors.InvalidState,
            result.Error);

        Assert.Equal(
            "tampered-state",
            stateProtector.LastProtectedState);

        Assert.Equal(
            0,
            provider.GetIdentityCallCount);

        Assert.Empty(
            authSessionRepository.Sessions);

        Assert.Empty(
            refreshTokenRepository.Tokens);

        Assert.Equal(
            0,
            accessTokenGenerator.GenerateCallCount);

        Assert.Equal(
            0,
            refreshTokenGenerator.GenerateCallCount);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }
    [Fact]
    public async Task HandleAsync_WhenStateProviderDoesNotMatchCommand_ShouldReturnInvalidState()
    {
        var now =
            new DateTimeOffset(
                2026,
                9,
                14,
                8,
                30,
                0,
                TimeSpan.Zero);

        var provider =
            new FakeExternalIdentityProvider(
                ExternalLoginProvider.Google);

        var resolver =
            new FakeExternalIdentityProviderResolver();

        resolver.Register(
            provider);

        var stateProtector =
            new FakeExternalAuthenticationStateProtector
            {
                State =
                    new(
                        "GitHub",
                        "https://localhost/signin-google",
                        "test-code-verifier")
            };

        var externalLoginRepository =
            new FakeExternalLoginRepository();

        var userRepository =
            new FakeUserRepository();

        var authSessionRepository =
            new FakeAuthSessionRepository();

        var refreshTokenRepository =
            new FakeRefreshTokenRepository();

        var accessTokenGenerator =
            new FakeAccessTokenGenerator();

        var refreshTokenGenerator =
            new FakeRefreshTokenGenerator();

        var tokenHasher =
            new FakeTokenHasher();

        var unitOfWork =
            new FakeUnitOfWork();

        var clock =
            new FakeClock(
                now);

        var refreshTokenOptions =
            new RefreshTokenOptions
            {
                TokenLifetime =
                    TimeSpan.FromDays(30)
            };

        var handler =
            new CompleteExternalLoginCommandHandler(
                resolver,
                stateProtector,
                externalLoginRepository,
                userRepository,
                authSessionRepository,
                refreshTokenRepository,
                accessTokenGenerator,
                refreshTokenGenerator,
                tokenHasher,
                unitOfWork,
                clock,
                refreshTokenOptions);

        var command =
            new CompleteExternalLoginCommand(
                ExternalLoginProvider.Google,
                "authorization-code",
                "protected-state",
                "https://localhost/signin-google",
                "device-hash-provider-mismatch",
                "Chrome on Windows",
                "127.0.0.1",
                "Chrome/1.0",
                null);

        var result =
            await handler.HandleAsync(
                command);

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ExternalAuthenticationErrors.InvalidState,
            result.Error);

        Assert.Equal(
            0,
            provider.GetIdentityCallCount);

        Assert.Empty(
            authSessionRepository.Sessions);

        Assert.Empty(
            refreshTokenRepository.Tokens);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }
    [Fact]
    public async Task HandleAsync_WhenStateRedirectUriDoesNotMatchCommand_ShouldReturnInvalidState()
    {
        var now =
            new DateTimeOffset(
                2026,
                9,
                14,
                8,
                45,
                0,
                TimeSpan.Zero);

        var provider =
            new FakeExternalIdentityProvider(
                ExternalLoginProvider.Google);

        var resolver =
            new FakeExternalIdentityProviderResolver();

        resolver.Register(
            provider);

        var stateProtector =
            new FakeExternalAuthenticationStateProtector
            {
                State =
                    new(
                        "Google",
                        "https://localhost/other-callback",
                        "test-code-verifier")
            };

        var externalLoginRepository =
            new FakeExternalLoginRepository();

        var userRepository =
            new FakeUserRepository();

        var authSessionRepository =
            new FakeAuthSessionRepository();

        var refreshTokenRepository =
            new FakeRefreshTokenRepository();

        var accessTokenGenerator =
            new FakeAccessTokenGenerator();

        var refreshTokenGenerator =
            new FakeRefreshTokenGenerator();

        var tokenHasher =
            new FakeTokenHasher();

        var unitOfWork =
            new FakeUnitOfWork();

        var clock =
            new FakeClock(
                now);

        var refreshTokenOptions =
            new RefreshTokenOptions
            {
                TokenLifetime =
                    TimeSpan.FromDays(30)
            };

        var handler =
            new CompleteExternalLoginCommandHandler(
                resolver,
                stateProtector,
                externalLoginRepository,
                userRepository,
                authSessionRepository,
                refreshTokenRepository,
                accessTokenGenerator,
                refreshTokenGenerator,
                tokenHasher,
                unitOfWork,
                clock,
                refreshTokenOptions);

        var command =
            new CompleteExternalLoginCommand(
                ExternalLoginProvider.Google,
                "authorization-code",
                "protected-state",
                "https://localhost/signin-google",
                "device-hash-redirect-mismatch",
                "Chrome on Windows",
                "127.0.0.1",
                "Chrome/1.0",
                null);

        var result =
            await handler.HandleAsync(
                command);

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ExternalAuthenticationErrors.InvalidState,
            result.Error);

        Assert.Equal(
            0,
            provider.GetIdentityCallCount);

        Assert.Empty(
            authSessionRepository.Sessions);

        Assert.Empty(
            refreshTokenRepository.Tokens);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }
    [Fact]
    public async Task HandleAsync_WhenExternalIdentityProviderThrows_ShouldReturnProviderFailure()
    {
        var now =
            new DateTimeOffset(
                2026,
                9,
                14,
                12,
                0,
                0,
                TimeSpan.Zero);

        var provider =
            new FakeExternalIdentityProvider(
                ExternalLoginProvider.Google)
            {
                ExceptionToThrow =
                    new ExternalIdentityProviderException(
                        "Google authorization-code exchange failed.")
            };

        var resolver =
            new FakeExternalIdentityProviderResolver();

        resolver.Register(
            provider);

        var stateProtector =
            new FakeExternalAuthenticationStateProtector();

        var externalLoginRepository =
            new FakeExternalLoginRepository();

        var userRepository =
            new FakeUserRepository();

        var authSessionRepository =
            new FakeAuthSessionRepository();

        var refreshTokenRepository =
            new FakeRefreshTokenRepository();

        var accessTokenGenerator =
            new FakeAccessTokenGenerator();

        var refreshTokenGenerator =
            new FakeRefreshTokenGenerator();

        var tokenHasher =
            new FakeTokenHasher();

        var unitOfWork =
            new FakeUnitOfWork();

        var clock =
            new FakeClock(
                now);

        var refreshTokenOptions =
            new RefreshTokenOptions
            {
                TokenLifetime =
                    TimeSpan.FromDays(30)
            };

        var handler =
            new CompleteExternalLoginCommandHandler(
                resolver,
                stateProtector,
                externalLoginRepository,
                userRepository,
                authSessionRepository,
                refreshTokenRepository,
                accessTokenGenerator,
                refreshTokenGenerator,
                tokenHasher,
                unitOfWork,
                clock,
                refreshTokenOptions);

        var command =
            new CompleteExternalLoginCommand(
                ExternalLoginProvider.Google,
                "invalid-authorization-code",
                "protected-state",
                "https://localhost/signin-google",
                "device-hash-provider-failure",
                "Chrome on Windows",
                "127.0.0.1",
                "Chrome/1.0",
                null);

        var result =
            await handler.HandleAsync(
                command);

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ExternalAuthenticationErrors.ProviderFailure,
            result.Error);

        Assert.Equal(
            1,
            provider.GetIdentityCallCount);

        Assert.Null(
            userRepository.AddedUser);

        Assert.Empty(
            externalLoginRepository.ExternalLogins);

        Assert.Empty(
            authSessionRepository.Sessions);

        Assert.Empty(
            refreshTokenRepository.Tokens);

        Assert.Equal(
            0,
            accessTokenGenerator.GenerateCallCount);

        Assert.Equal(
            0,
            refreshTokenGenerator.GenerateCallCount);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenStateIsInvalidAndProviderReportsAccessDenied_ShouldReturnInvalidState()
    {
        var now =
            new DateTimeOffset(
                2026,
                9,
                15,
                8,
                0,
                0,
                TimeSpan.Zero);

        var provider =
            new FakeExternalIdentityProvider(
                ExternalLoginProvider.Google);

        var resolver =
            new FakeExternalIdentityProviderResolver();

        resolver.Register(
            provider);

        var stateProtector =
            new FakeExternalAuthenticationStateProtector
            {
                ThrowOnUnprotect = true
            };

        var externalLoginRepository =
            new FakeExternalLoginRepository();

        var userRepository =
            new FakeUserRepository();

        var authSessionRepository =
            new FakeAuthSessionRepository();

        var refreshTokenRepository =
            new FakeRefreshTokenRepository();

        var accessTokenGenerator =
            new FakeAccessTokenGenerator();

        var refreshTokenGenerator =
            new FakeRefreshTokenGenerator();

        var tokenHasher =
            new FakeTokenHasher();

        var unitOfWork =
            new FakeUnitOfWork();

        var clock =
            new FakeClock(
                now);

        var refreshTokenOptions =
            new RefreshTokenOptions
            {
                TokenLifetime =
                    TimeSpan.FromDays(30)
            };

        var handler =
            new CompleteExternalLoginCommandHandler(
                resolver,
                stateProtector,
                externalLoginRepository,
                userRepository,
                authSessionRepository,
                refreshTokenRepository,
                accessTokenGenerator,
                refreshTokenGenerator,
                tokenHasher,
                unitOfWork,
                clock,
                refreshTokenOptions);

        var command =
            new CompleteExternalLoginCommand(
                ExternalLoginProvider.Google,
                null,
                "tampered-state",
                "https://localhost/signin-google",
                "device-hash-denied",
                "Chrome on Windows",
                "127.0.0.1",
                "Chrome/1.0",
                "access_denied");

        var result =
            await handler.HandleAsync(
                command);

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ExternalAuthenticationErrors.InvalidState,
            result.Error);

        Assert.Equal(
            "tampered-state",
            stateProtector.LastProtectedState);

        Assert.Equal(
            0,
            provider.GetIdentityCallCount);

        Assert.Empty(
            authSessionRepository.Sessions);

        Assert.Empty(
            refreshTokenRepository.Tokens);

        Assert.Equal(
            0,
            accessTokenGenerator.GenerateCallCount);

        Assert.Equal(
            0,
            refreshTokenGenerator.GenerateCallCount);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }
    [Fact]
    public async Task HandleAsync_WhenProviderReportsAccessDenied_ShouldReturnAuthorizationDenied()
    {
        var now =
            new DateTimeOffset(
                2026,
                9,
                15,
                8,
                30,
                0,
                TimeSpan.Zero);

        var provider =
            new FakeExternalIdentityProvider(
                ExternalLoginProvider.Google);

        var resolver =
            new FakeExternalIdentityProviderResolver();

        resolver.Register(
            provider);

        var stateProtector =
            new FakeExternalAuthenticationStateProtector();

        var externalLoginRepository =
            new FakeExternalLoginRepository();

        var userRepository =
            new FakeUserRepository();

        var authSessionRepository =
            new FakeAuthSessionRepository();

        var refreshTokenRepository =
            new FakeRefreshTokenRepository();

        var accessTokenGenerator =
            new FakeAccessTokenGenerator();

        var refreshTokenGenerator =
            new FakeRefreshTokenGenerator();

        var tokenHasher =
            new FakeTokenHasher();

        var unitOfWork =
            new FakeUnitOfWork();

        var clock =
            new FakeClock(
                now);

        var refreshTokenOptions =
            new RefreshTokenOptions
            {
                TokenLifetime =
                    TimeSpan.FromDays(30)
            };

        var handler =
            new CompleteExternalLoginCommandHandler(
                resolver,
                stateProtector,
                externalLoginRepository,
                userRepository,
                authSessionRepository,
                refreshTokenRepository,
                accessTokenGenerator,
                refreshTokenGenerator,
                tokenHasher,
                unitOfWork,
                clock,
                refreshTokenOptions);

        var command =
            new CompleteExternalLoginCommand(
                ExternalLoginProvider.Google,
                null,
                "protected-state",
                "https://localhost/signin-google",
                "device-hash-denied",
                "Chrome on Windows",
                "127.0.0.1",
                "Chrome/1.0",
                "access_denied");

        var result =
            await handler.HandleAsync(
                command);

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ExternalAuthenticationErrors.AuthorizationDenied,
            result.Error);

        Assert.Equal(
            "protected-state",
            stateProtector.LastProtectedState);

        Assert.Equal(
            0,
            provider.GetIdentityCallCount);

        Assert.Null(
            userRepository.AddedUser);

        Assert.Empty(
            externalLoginRepository.ExternalLogins);

        Assert.Empty(
            authSessionRepository.Sessions);

        Assert.Empty(
            refreshTokenRepository.Tokens);

        Assert.Equal(
            0,
            accessTokenGenerator.GenerateCallCount);

        Assert.Equal(
            0,
            refreshTokenGenerator.GenerateCallCount);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }
    [Fact]
    public async Task HandleAsync_WhenProviderReportsUnknownError_ShouldReturnProviderRejected()
    {
        var now =
            new DateTimeOffset(
                2026,
                9,
                15,
                9,
                0,
                0,
                TimeSpan.Zero);

        var provider =
            new FakeExternalIdentityProvider(
                ExternalLoginProvider.Google);

        var resolver =
            new FakeExternalIdentityProviderResolver();

        resolver.Register(
            provider);

        var stateProtector =
            new FakeExternalAuthenticationStateProtector();

        var externalLoginRepository =
            new FakeExternalLoginRepository();

        var userRepository =
            new FakeUserRepository();

        var authSessionRepository =
            new FakeAuthSessionRepository();

        var refreshTokenRepository =
            new FakeRefreshTokenRepository();

        var accessTokenGenerator =
            new FakeAccessTokenGenerator();

        var refreshTokenGenerator =
            new FakeRefreshTokenGenerator();

        var tokenHasher =
            new FakeTokenHasher();

        var unitOfWork =
            new FakeUnitOfWork();

        var clock =
            new FakeClock(
                now);

        var refreshTokenOptions =
            new RefreshTokenOptions
            {
                TokenLifetime =
                    TimeSpan.FromDays(30)
            };

        var handler =
            new CompleteExternalLoginCommandHandler(
                resolver,
                stateProtector,
                externalLoginRepository,
                userRepository,
                authSessionRepository,
                refreshTokenRepository,
                accessTokenGenerator,
                refreshTokenGenerator,
                tokenHasher,
                unitOfWork,
                clock,
                refreshTokenOptions);

        var command =
            new CompleteExternalLoginCommand(
                ExternalLoginProvider.Google,
                null,
                "protected-state",
                "https://localhost/signin-google",
                "device-hash-provider-rejected",
                "Chrome on Windows",
                "127.0.0.1",
                "Chrome/1.0",
                "server_error");

        var result =
            await handler.HandleAsync(
                command);

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ExternalAuthenticationErrors.ProviderRejected,
            result.Error);

        Assert.Equal(
            "protected-state",
            stateProtector.LastProtectedState);

        Assert.Equal(
            0,
            provider.GetIdentityCallCount);

        Assert.Null(
            userRepository.AddedUser);

        Assert.Empty(
            externalLoginRepository.ExternalLogins);

        Assert.Empty(
            authSessionRepository.Sessions);

        Assert.Empty(
            refreshTokenRepository.Tokens);

        Assert.Equal(
            0,
            accessTokenGenerator.GenerateCallCount);

        Assert.Equal(
            0,
            refreshTokenGenerator.GenerateCallCount);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }
    [Fact]
    public async Task HandleAsync_WhenAuthorizationCodeIsMissing_ShouldReturnAuthorizationCodeMissing()
    {
        var now =
            new DateTimeOffset(
                2026,
                9,
                15,
                9,
                30,
                0,
                TimeSpan.Zero);

        var provider =
            new FakeExternalIdentityProvider(
                ExternalLoginProvider.Google);

        var resolver =
            new FakeExternalIdentityProviderResolver();

        resolver.Register(
            provider);

        var stateProtector =
            new FakeExternalAuthenticationStateProtector();

        var externalLoginRepository =
            new FakeExternalLoginRepository();

        var userRepository =
            new FakeUserRepository();

        var authSessionRepository =
            new FakeAuthSessionRepository();

        var refreshTokenRepository =
            new FakeRefreshTokenRepository();

        var accessTokenGenerator =
            new FakeAccessTokenGenerator();

        var refreshTokenGenerator =
            new FakeRefreshTokenGenerator();

        var tokenHasher =
            new FakeTokenHasher();

        var unitOfWork =
            new FakeUnitOfWork();

        var clock =
            new FakeClock(
                now);

        var refreshTokenOptions =
            new RefreshTokenOptions
            {
                TokenLifetime =
                    TimeSpan.FromDays(30)
            };

        var handler =
            new CompleteExternalLoginCommandHandler(
                resolver,
                stateProtector,
                externalLoginRepository,
                userRepository,
                authSessionRepository,
                refreshTokenRepository,
                accessTokenGenerator,
                refreshTokenGenerator,
                tokenHasher,
                unitOfWork,
                clock,
                refreshTokenOptions);

        var command =
            new CompleteExternalLoginCommand(
                ExternalLoginProvider.Google,
                null,
                "protected-state",
                "https://localhost/signin-google",
                "device-hash-missing-code",
                "Chrome on Windows",
                "127.0.0.1",
                "Chrome/1.0",
                null);

        var result =
            await handler.HandleAsync(
                command);

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ExternalAuthenticationErrors.AuthorizationCodeMissing,
            result.Error);

        Assert.Equal(
            "protected-state",
            stateProtector.LastProtectedState);

        Assert.Equal(
            0,
            provider.GetIdentityCallCount);

        Assert.Null(
            userRepository.AddedUser);

        Assert.Empty(
            externalLoginRepository.ExternalLogins);

        Assert.Empty(
            authSessionRepository.Sessions);

        Assert.Empty(
            refreshTokenRepository.Tokens);

        Assert.Equal(
            0,
            accessTokenGenerator.GenerateCallCount);

        Assert.Equal(
            0,
            refreshTokenGenerator.GenerateCallCount);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }}







