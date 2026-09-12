using PGLN.Auth.Application.Features.TrustedDevices.RevokeTrustedDevice;
using PGLN.Auth.Application.Tests.TestDoubles;
using PGLN.Auth.Domain.TrustedDevices;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Tests.Features.TrustedDevices.RevokeTrustedDevice;

public sealed class RevokeTrustedDeviceCommandHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(
            2026,
            9,
            12,
            12,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_WithOwnedTrustedDevice_ShouldRevokeTrust()
    {
        var userId =
            UserId.New();

        var trustedDevice =
            CreateTrustedDevice(
                userId);

        var repository =
            new FakeTrustedDeviceRepository();

        repository.Seed(
            trustedDevice);

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateHandler(
                repository,
                unitOfWork);

        var result =
            await handler.HandleAsync(
                new RevokeTrustedDeviceCommand(
                    userId,
                    trustedDevice.Id));

        Assert.True(
            result.IsSuccess);

        Assert.True(
            trustedDevice.IsRevoked);

        Assert.False(
            trustedDevice.IsTrusted);

        Assert.Equal(
            Now,
            trustedDevice.RevokedAtUtc);

        Assert.Equal(
            "UserRevokedTrust",
            trustedDevice.RevocationReason);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithAnotherUsersTrustedDevice_ShouldReturnNotFound()
    {
        var ownerId =
            UserId.New();

        var requestingUserId =
            UserId.New();

        var trustedDevice =
            CreateTrustedDevice(
                ownerId);

        var repository =
            new FakeTrustedDeviceRepository();

        repository.Seed(
            trustedDevice);

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateHandler(
                repository,
                unitOfWork);

        var result =
            await handler.HandleAsync(
                new RevokeTrustedDeviceCommand(
                    requestingUserId,
                    trustedDevice.Id));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            "TrustedDevices.NotFound",
            result.Error.Code);

        Assert.False(
            trustedDevice.IsRevoked);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownTrustedDevice_ShouldReturnNotFound()
    {
        var repository =
            new FakeTrustedDeviceRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateHandler(
                repository,
                unitOfWork);

        var result =
            await handler.HandleAsync(
                new RevokeTrustedDeviceCommand(
                    UserId.New(),
                    TrustedDeviceId.New()));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            "TrustedDevices.NotFound",
            result.Error.Code);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenAlreadyRevoked_ShouldRemainRevoked()
    {
        var userId =
            UserId.New();

        var trustedDevice =
            CreateTrustedDevice(
                userId);

        var firstRevocationTime =
            Now.AddDays(-1);

        trustedDevice.Revoke(
            firstRevocationTime,
            "OriginalReason");

        var repository =
            new FakeTrustedDeviceRepository();

        repository.Seed(
            trustedDevice);

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateHandler(
                repository,
                unitOfWork);

        var result =
            await handler.HandleAsync(
                new RevokeTrustedDeviceCommand(
                    userId,
                    trustedDevice.Id));

        Assert.True(
            result.IsSuccess);

        Assert.True(
            trustedDevice.IsRevoked);

        Assert.Equal(
            firstRevocationTime,
            trustedDevice.RevokedAtUtc);

        Assert.Equal(
            "OriginalReason",
            trustedDevice.RevocationReason);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    private static RevokeTrustedDeviceCommandHandler CreateHandler(
        FakeTrustedDeviceRepository repository,
        FakeUnitOfWork unitOfWork)
    {
        return new RevokeTrustedDeviceCommandHandler(
            repository,
            unitOfWork,
            new FakeClock(Now));
    }

    private static TrustedDevice CreateTrustedDevice(
        UserId userId)
    {
        return TrustedDevice.Create(
            TrustedDeviceId.New(),
            userId,
            "device-hash-001",
            "Test Device",
            Now.AddDays(-7));
    }
}
