using PGLN.Auth.Application.Features.Sessions.TrustDevice;
using PGLN.Auth.Application.Tests.TestDoubles;
using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.TrustedDevices;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Tests.Features.Sessions.TrustDevice;

public sealed class TrustDeviceCommandHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(
            2026,
            9,
            12,
            10,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_WithOwnedActiveSession_ShouldCreateTrustedDevice()
    {
        var userId =
            UserId.New();

        var session =
            CreateSession(
                userId,
                "device-one");

        var sessionRepository =
            new FakeAuthSessionRepository();

        sessionRepository.Seed(
            session);

        var trustedDeviceRepository =
            new FakeTrustedDeviceRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateHandler(
                sessionRepository,
                trustedDeviceRepository,
                unitOfWork);

        var result =
            await handler.HandleAsync(
                new TrustDeviceCommand(
                    userId,
                    session.Id));

        Assert.True(
            result.IsSuccess);

        var trustedDevice =
            Assert.Single(
                trustedDeviceRepository.Devices);

        Assert.Equal(
            userId,
            trustedDevice.UserId);

        Assert.Equal(
            session.DeviceIdHash,
            trustedDevice.DeviceIdHash);

        Assert.Equal(
            session.DeviceName,
            trustedDevice.DeviceName);

        Assert.Equal(
            Now,
            trustedDevice.TrustedAtUtc);

        Assert.True(
            trustedDevice.IsTrusted);

        Assert.Equal(
            DeviceTrustStatus.Trusted,
            session.DeviceTrustStatus);

        Assert.True(
            session.IsTrustedDevice);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithExistingTrustedDevice_ShouldNotCreateDuplicate()
    {
        var userId =
            UserId.New();

        var session =
            CreateSession(
                userId,
                "device-one");

        var sessionRepository =
            new FakeAuthSessionRepository();

        sessionRepository.Seed(
            session);

        var existingDevice =
            TrustedDevice.Create(
                TrustedDeviceId.New(),
                userId,
                session.DeviceIdHash,
                session.DeviceName,
                Now.AddDays(-5));

        var trustedDeviceRepository =
            new FakeTrustedDeviceRepository();

        trustedDeviceRepository.Seed(
            existingDevice);

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateHandler(
                sessionRepository,
                trustedDeviceRepository,
                unitOfWork);

        var result =
            await handler.HandleAsync(
                new TrustDeviceCommand(
                    userId,
                    session.Id));

        Assert.True(
            result.IsSuccess);

        Assert.Single(
            trustedDeviceRepository.Devices);

        Assert.Same(
            existingDevice,
            trustedDeviceRepository.Devices.Single());

        Assert.True(
            session.IsTrustedDevice);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithRevokedTrustedDevice_ShouldTrustDeviceAgain()
    {
        var userId =
            UserId.New();

        var session =
            CreateSession(
                userId,
                "device-one");

        var sessionRepository =
            new FakeAuthSessionRepository();

        sessionRepository.Seed(
            session);

        var trustedDevice =
            TrustedDevice.Create(
                TrustedDeviceId.New(),
                userId,
                session.DeviceIdHash,
                "Old Device Name",
                Now.AddDays(-5));

        trustedDevice.Revoke(
            Now.AddDays(-2),
            "UserRevokedTrust");

        var trustedDeviceRepository =
            new FakeTrustedDeviceRepository();

        trustedDeviceRepository.Seed(
            trustedDevice);

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateHandler(
                sessionRepository,
                trustedDeviceRepository,
                unitOfWork);

        var result =
            await handler.HandleAsync(
                new TrustDeviceCommand(
                    userId,
                    session.Id));

        Assert.True(
            result.IsSuccess);

        Assert.Single(
            trustedDeviceRepository.Devices);

        Assert.True(
            trustedDevice.IsTrusted);

        Assert.False(
            trustedDevice.IsRevoked);

        Assert.Equal(
            Now,
            trustedDevice.TrustedAtUtc);

        Assert.Equal(
            session.DeviceName,
            trustedDevice.DeviceName);

        Assert.Null(
            trustedDevice.RevokedAtUtc);

        Assert.Null(
            trustedDevice.RevocationReason);

        Assert.True(
            session.IsTrustedDevice);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithAnotherUsersSession_ShouldReturnFailure()
    {
        var sessionOwnerId =
            UserId.New();

        var requestingUserId =
            UserId.New();

        var session =
            CreateSession(
                sessionOwnerId,
                "device-one");

        var sessionRepository =
            new FakeAuthSessionRepository();

        sessionRepository.Seed(
            session);

        var trustedDeviceRepository =
            new FakeTrustedDeviceRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateHandler(
                sessionRepository,
                trustedDeviceRepository,
                unitOfWork);

        var result =
            await handler.HandleAsync(
                new TrustDeviceCommand(
                    requestingUserId,
                    session.Id));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            "Sessions.NotFound",
            result.Error.Code);

        Assert.Empty(
            trustedDeviceRepository.Devices);

        Assert.False(
            session.IsTrustedDevice);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownSession_ShouldReturnFailure()
    {
        var trustedDeviceRepository =
            new FakeTrustedDeviceRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateHandler(
                new FakeAuthSessionRepository(),
                trustedDeviceRepository,
                unitOfWork);

        var result =
            await handler.HandleAsync(
                new TrustDeviceCommand(
                    UserId.New(),
                    AuthSessionId.New()));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            "Sessions.NotFound",
            result.Error.Code);

        Assert.Empty(
            trustedDeviceRepository.Devices);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithRevokedSession_ShouldReturnFailure()
    {
        var userId =
            UserId.New();

        var session =
            CreateSession(
                userId,
                "device-one");

        session.Revoke(
            Now,
            "UserRevokedSession");

        var sessionRepository =
            new FakeAuthSessionRepository();

        sessionRepository.Seed(
            session);

        var trustedDeviceRepository =
            new FakeTrustedDeviceRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateHandler(
                sessionRepository,
                trustedDeviceRepository,
                unitOfWork);

        var result =
            await handler.HandleAsync(
                new TrustDeviceCommand(
                    userId,
                    session.Id));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            "Sessions.Revoked",
            result.Error.Code);

        Assert.Empty(
            trustedDeviceRepository.Devices);

        Assert.False(
            session.IsTrustedDevice);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    private static TrustDeviceCommandHandler CreateHandler(
        FakeAuthSessionRepository sessionRepository,
        FakeTrustedDeviceRepository trustedDeviceRepository,
        FakeUnitOfWork unitOfWork)
    {
        return new TrustDeviceCommandHandler(
            sessionRepository,
            trustedDeviceRepository,
            unitOfWork,
            new FakeClock(Now));
    }

    private static AuthSession CreateSession(
        UserId userId,
        string deviceIdHash)
    {
        return AuthSession.Create(
            AuthSessionId.New(),
            userId,
            deviceIdHash,
            "Test Device",
            "127.0.0.1",
            "PGLN.Auth.Application.Tests",
            Now.AddDays(-1));
    }
}
