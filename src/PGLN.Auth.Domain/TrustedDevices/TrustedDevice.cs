using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Domain.TrustedDevices;

public sealed class TrustedDevice
{
    private TrustedDevice()
    {
    }

    private TrustedDevice(
        TrustedDeviceId id,
        UserId userId,
        string deviceIdHash,
        string? deviceName,
        DateTimeOffset trustedAtUtc)
    {
        Id =
            id;

        UserId =
            userId;

        DeviceIdHash =
            deviceIdHash;

        DeviceName =
            deviceName;

        TrustedAtUtc =
            trustedAtUtc;
    }

    public TrustedDeviceId Id { get; private set; }

    public UserId UserId { get; private set; }

    public string DeviceIdHash { get; private set; } =
        string.Empty;

    public string? DeviceName { get; private set; }

    public DateTimeOffset TrustedAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public string? RevocationReason { get; private set; }

    public bool IsRevoked =>
        RevokedAtUtc.HasValue;

    public bool IsTrusted =>
        !IsRevoked;

    public static TrustedDevice Create(
        TrustedDeviceId id,
        UserId userId,
        string deviceIdHash,
        string? deviceName,
        DateTimeOffset trustedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            deviceIdHash);

        return new TrustedDevice(
            id,
            userId,
            deviceIdHash,
            deviceName,
            trustedAtUtc);
    }

    public void TrustAgain(
        DateTimeOffset trustedAtUtc,
        string? deviceName = null)
    {
        if (!IsRevoked)
        {
            return;
        }

        if (trustedAtUtc < TrustedAtUtc)
        {
            throw new ArgumentException(
                "The new trust time cannot be earlier than the original trust time.",
                nameof(trustedAtUtc));
        }

        TrustedAtUtc =
            trustedAtUtc;

        RevokedAtUtc =
            null;

        RevocationReason =
            null;

        if (!string.IsNullOrWhiteSpace(
                deviceName))
        {
            DeviceName =
                deviceName;
        }
    }

    public void Revoke(
        DateTimeOffset revokedAtUtc,
        string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            reason);

        if (IsRevoked)
        {
            return;
        }

        if (revokedAtUtc < TrustedAtUtc)
        {
            throw new ArgumentException(
                "The revocation time cannot be earlier than the trust time.",
                nameof(revokedAtUtc));
        }

        RevokedAtUtc =
            revokedAtUtc;

        RevocationReason =
            reason;
    }
}

