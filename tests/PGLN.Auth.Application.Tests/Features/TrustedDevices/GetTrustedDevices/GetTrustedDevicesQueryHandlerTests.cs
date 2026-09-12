using PGLN.Auth.Application.Features.TrustedDevices.GetTrustedDevices;
using PGLN.Auth.Application.Tests.TestDoubles;
using PGLN.Auth.Domain.TrustedDevices;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Tests.Features.TrustedDevices.GetTrustedDevices;

public sealed class GetTrustedDevicesQueryHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(
            2026,
            9,
            12,
            13,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_ShouldReturnOnlyUsersTrustedDevices()
    {
        var userId =
            UserId.New();

        var otherUserId =
            UserId.New();

        var repository =
            new FakeTrustedDeviceRepository();

        repository.Seed(
            CreateTrustedDevice(
                userId,
                "device-one",
                "Laptop",
                Now.AddDays(-2)));

        repository.Seed(
            CreateTrustedDevice(
                userId,
                "device-two",
                "Phone",
                Now.AddDays(-1)));

        repository.Seed(
            CreateTrustedDevice(
                otherUserId,
                "device-three",
                "Other User Device",
                Now));

        var handler =
            new GetTrustedDevicesQueryHandler(
                repository);

        var result =
            await handler.HandleAsync(
                new GetTrustedDevicesQuery(
                    userId));

        Assert.Equal(
            2,
            result.Count);

        Assert.DoesNotContain(
            result,
            item =>
                item.DeviceName ==
                "Other User Device");
    }

    [Fact]
    public async Task HandleAsync_ShouldMapTrustedDevice()
    {
        var userId =
            UserId.New();

        var trustedDevice =
            CreateTrustedDevice(
                userId,
                "device-one",
                "Laptop",
                Now.AddDays(-3));

        var repository =
            new FakeTrustedDeviceRepository();

        repository.Seed(
            trustedDevice);

        var handler =
            new GetTrustedDevicesQueryHandler(
                repository);

        var result =
            await handler.HandleAsync(
                new GetTrustedDevicesQuery(
                    userId));

        var response =
            Assert.Single(
                result);

        Assert.Equal(
            trustedDevice.Id,
            response.Id);


        Assert.Equal(
            "Laptop",
            response.DeviceName);

        Assert.Equal(
            trustedDevice.TrustedAtUtc,
            response.TrustedAtUtc);

        Assert.Null(
            response.RevokedAtUtc);

        Assert.True(
            response.IsTrusted);
    }

    [Fact]
    public async Task HandleAsync_ShouldMapRevokedDevice()
    {
        var userId =
            UserId.New();

        var trustedDevice =
            CreateTrustedDevice(
                userId,
                "device-one",
                "Laptop",
                Now.AddDays(-5));

        var revokedAtUtc =
            Now.AddDays(-1);

        trustedDevice.Revoke(
            revokedAtUtc,
            "UserRevokedTrust");

        var repository =
            new FakeTrustedDeviceRepository();

        repository.Seed(
            trustedDevice);

        var handler =
            new GetTrustedDevicesQueryHandler(
                repository);

        var result =
            await handler.HandleAsync(
                new GetTrustedDevicesQuery(
                    userId));

        var response =
            Assert.Single(
                result);

        Assert.False(
            response.IsTrusted);

        Assert.Equal(
            revokedAtUtc,
            response.RevokedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WithNoTrustedDevices_ShouldReturnEmptyCollection()
    {
        var handler =
            new GetTrustedDevicesQueryHandler(
                new FakeTrustedDeviceRepository());

        var result =
            await handler.HandleAsync(
                new GetTrustedDevicesQuery(
                    UserId.New()));

        Assert.Empty(
            result);
    }

    private static TrustedDevice CreateTrustedDevice(
        UserId userId,
        string deviceIdHash,
        string deviceName,
        DateTimeOffset trustedAtUtc)
    {
        return TrustedDevice.Create(
            TrustedDeviceId.New(),
            userId,
            deviceIdHash,
            deviceName,
            trustedAtUtc);
    }
}

