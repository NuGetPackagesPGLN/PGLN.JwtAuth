using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Configuration;
using PGLN.Auth.Application.Features.StepUp.VerifyStepUpChallenge;
using PGLN.Auth.Application.Tests.TestDoubles;
using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.StepUpChallenges;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Tests.Features.StepUp.VerifyStepUpChallenge;

public sealed class VerifyStepUpChallengeCommandHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(
            2026,
            9,
            13,
            8,
            0,
            0,
            TimeSpan.Zero);

    private const string ValidCode =
        "123456";

    private const string DeviceIdHash =
        "device-hash";

    private const string DeviceName =
        "Developer Laptop";

    [Fact]
    public async Task HandleAsync_WithValidCode_ShouldVerifyChallengeAndIssueCredentials()
    {
        var fixture =
            CreateFixture();

        var result =
            await fixture.Handler.HandleAsync(
                new VerifyStepUpChallengeCommand(
                    fixture.Challenge.Id.Value,
                    ValidCode,
                    false));

        Assert.True(
            result.IsSuccess);

        Assert.True(
            fixture.Challenge.IsVerified);

        Assert.Single(
            fixture.AuthSessionRepository.Sessions);

        Assert.Single(
            fixture.RefreshTokenRepository.Tokens);

        Assert.Equal(
            1,
            fixture.AccessTokenGenerator.GenerateCallCount);

        Assert.Equal(
            1,
            fixture.RefreshTokenGenerator.GenerateCallCount);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);

        var session =
            fixture.AuthSessionRepository
                .Sessions
                .Single();

        Assert.Equal(
            fixture.User.Id,
            session.UserId);

        Assert.Equal(
            DeviceIdHash,
            session.DeviceIdHash);

        Assert.False(
            session.IsTrustedDevice);

        var refreshToken =
            fixture.RefreshTokenRepository
                .Tokens
                .Single();

        Assert.Equal(
            session.Id,
            refreshToken.SessionId);

        Assert.Equal(
            "hashed::raw-refresh-token",
            refreshToken.TokenHash);

        Assert.Equal(
            "fake-access-token",
            result.Value.AccessToken);

        Assert.Equal(
            "raw-refresh-token",
            result.Value.RefreshToken);

        Assert.Equal(
            fixture.User.Id.Value,
            result.Value.UserId);

        Assert.Equal(
            fixture.User.Email.Value,
            result.Value.Email);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidCode_ShouldIncrementFailedAttemptsWithoutIssuingCredentials()
    {
        var fixture =
            CreateFixture();

        var result =
            await fixture.Handler.HandleAsync(
                new VerifyStepUpChallengeCommand(
                    fixture.Challenge.Id.Value,
                    "654321",
                    false));

        Assert.False(
            result.IsSuccess);

        Assert.Equal(
            "StepUpChallenge.InvalidCode",
            result.Error.Code);

        Assert.Equal(
            1,
            fixture.Challenge.FailedAttempts);

        Assert.False(
            fixture.Challenge.IsVerified);

        Assert.Empty(
            fixture.AuthSessionRepository.Sessions);

        Assert.Empty(
            fixture.RefreshTokenRepository.Tokens);

        Assert.Equal(
            0,
            fixture.AccessTokenGenerator.GenerateCallCount);

        Assert.Equal(
            0,
            fixture.RefreshTokenGenerator.GenerateCallCount);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithExpiredChallenge_ShouldRejectWithoutIssuingCredentials()
    {
        var fixture =
            CreateFixture(
                clockNow:
                    Now.AddMinutes(11));

        var result =
            await fixture.Handler.HandleAsync(
                new VerifyStepUpChallengeCommand(
                    fixture.Challenge.Id.Value,
                    ValidCode,
                    false));

        Assert.False(
            result.IsSuccess);

        Assert.Equal(
            "StepUpChallenge.Expired",
            result.Error.Code);

        Assert.False(
            fixture.Challenge.IsVerified);

        Assert.Empty(
            fixture.AuthSessionRepository.Sessions);

        Assert.Empty(
            fixture.RefreshTokenRepository.Tokens);

        Assert.Equal(
            0,
            fixture.AccessTokenGenerator.GenerateCallCount);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenMaximumAttemptsReached_ShouldRejectWithoutCheckingCode()
    {
        var fixture =
            CreateFixture();

        for (var attempt = 0;
             attempt < 5;
             attempt++)
        {
            fixture.Challenge.RecordFailedAttempt();
        }

        var result =
            await fixture.Handler.HandleAsync(
                new VerifyStepUpChallengeCommand(
                    fixture.Challenge.Id.Value,
                    ValidCode,
                    false));

        Assert.False(
            result.IsSuccess);

        Assert.Equal(
            "StepUpChallenge.MaximumAttemptsExceeded",
            result.Error.Code);

        Assert.Equal(
            0,
            fixture.StepUpCodeProtector.VerifyCallCount);

        Assert.Empty(
            fixture.AuthSessionRepository.Sessions);

        Assert.Empty(
            fixture.RefreshTokenRepository.Tokens);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenRememberDeviceIsTrue_ShouldPersistTrustAndTrustSession()
    {
        var fixture =
            CreateFixture();

        var result =
            await fixture.Handler.HandleAsync(
                new VerifyStepUpChallengeCommand(
                    fixture.Challenge.Id.Value,
                    ValidCode,
                    true));

        Assert.True(
            result.IsSuccess);

        var trustedDevice =
            Assert.Single(
                fixture.TrustedDeviceRepository.Devices);

        Assert.Equal(
            fixture.User.Id,
            trustedDevice.UserId);

        Assert.Equal(
            DeviceIdHash,
            trustedDevice.DeviceIdHash);

        Assert.True(
            trustedDevice.IsTrusted);

        var session =
            Assert.Single(
                fixture.AuthSessionRepository.Sessions);

        Assert.True(
            session.IsTrustedDevice);

        Assert.Equal(
            DeviceTrustStatus.Trusted,
            session.DeviceTrustStatus);
    }

    [Fact]
    public async Task HandleAsync_WhenRememberDeviceIsFalse_ShouldNotPersistDeviceTrust()
    {
        var fixture =
            CreateFixture();

        var result =
            await fixture.Handler.HandleAsync(
                new VerifyStepUpChallengeCommand(
                    fixture.Challenge.Id.Value,
                    ValidCode,
                    false));

        Assert.True(
            result.IsSuccess);

        Assert.Empty(
            fixture.TrustedDeviceRepository.Devices);

        var session =
            Assert.Single(
                fixture.AuthSessionRepository.Sessions);

        Assert.False(
            session.IsTrustedDevice);

        Assert.Equal(
            DeviceTrustStatus.Unknown,
            session.DeviceTrustStatus);

        Assert.Single(
            fixture.RefreshTokenRepository.Tokens);
    }

    private static TestFixture CreateFixture(
        DateTimeOffset? clockNow = null)
    {
        var user =
            User.Register(
                new UserId(
                    Guid.NewGuid()),
                Email.Create(
                    "user@example.com"),
                "hashed-password",
                Now.AddDays(-30));

        user.ConfirmEmail(
            Now.AddDays(-29));

        var challenge =
            StepUpChallenge.Create(
                StepUpChallengeId.New(),
                user.Id,
                DeviceIdHash,
                DeviceName,
                $"protected::{ValidCode}",
                Now,
                Now.AddMinutes(10));

        var userRepository =
            new FakeUserRepository();

        userRepository.Seed(
            user);

        var challengeRepository =
            new FakeStepUpChallengeRepository();

        challengeRepository.Seed(
            challenge);

        var authSessionRepository =
            new FakeAuthSessionRepository();

        var trustedDeviceRepository =
            new FakeTrustedDeviceRepository();

        var refreshTokenRepository =
            new FakeRefreshTokenRepository();

        var stepUpCodeProtector =
            new FakeStepUpCodeProtector();

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
                clockNow ?? Now);

        var handler =
            new VerifyStepUpChallengeCommandHandler(
                challengeRepository,
                userRepository,
                authSessionRepository,
                trustedDeviceRepository,
                refreshTokenRepository,
                stepUpCodeProtector,
                accessTokenGenerator,
                refreshTokenGenerator,
                tokenHasher,
                unitOfWork,
                clock,
                new RefreshTokenOptions
                {
                    TokenLifetime =
                        TimeSpan.FromDays(30)
                },
                new StepUpChallengeOptions
                {
                    Lifetime =
                        TimeSpan.FromMinutes(10),
                    MaxFailedAttempts =
                        5
                });

        return new TestFixture(
            handler,
            user,
            challenge,
            authSessionRepository,
            trustedDeviceRepository,
            refreshTokenRepository,
            stepUpCodeProtector,
            accessTokenGenerator,
            refreshTokenGenerator,
            unitOfWork);
    }

    private sealed record TestFixture(
        VerifyStepUpChallengeCommandHandler Handler,
        User User,
        StepUpChallenge Challenge,
        FakeAuthSessionRepository AuthSessionRepository,
        FakeTrustedDeviceRepository TrustedDeviceRepository,
        FakeRefreshTokenRepository RefreshTokenRepository,
        FakeStepUpCodeProtector StepUpCodeProtector,
        FakeAccessTokenGenerator AccessTokenGenerator,
        FakeRefreshTokenGenerator RefreshTokenGenerator,
        FakeUnitOfWork UnitOfWork);
}
