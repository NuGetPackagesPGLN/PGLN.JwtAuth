using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Domain.Sessions;

public sealed class AuthSession
{
    private AuthSession()
    {
    }

    private AuthSession(
        AuthSessionId id,
        UserId userId,
        string deviceIdHash,
        string? deviceName,
        string? ipAddress,
        string? userAgent,
        DateTimeOffset createdAtUtc)
    {
        Id =
            id;

        UserId =
            userId;

        DeviceIdHash =
            deviceIdHash;

        DeviceName =
            deviceName;

        IpAddress =
            ipAddress;

        UserAgent =
            userAgent;

        CreatedAtUtc =
            createdAtUtc;

        LastSeenAtUtc =
            createdAtUtc;
    }

    public AuthSessionId Id { get; private set; }

    public UserId UserId { get; private set; }

    public string DeviceIdHash { get; private set; } =
        string.Empty;

    public string? DeviceName { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset LastSeenAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public string? RevocationReason { get; private set; }

    public bool IsRevoked =>
        RevokedAtUtc.HasValue;

    public bool IsActive =>
        !IsRevoked;

    public static AuthSession Create(
        AuthSessionId id,
        UserId userId,
        string deviceIdHash,
        string? deviceName,
        string? ipAddress,
        string? userAgent,
        DateTimeOffset createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            deviceIdHash);

        return new AuthSession(
            id,
            userId,
            deviceIdHash,
            deviceName,
            ipAddress,
            userAgent,
            createdAtUtc);
    }

    public void Touch(
        DateTimeOffset seenAtUtc,
        string? ipAddress = null,
        string? userAgent = null)
    {
        if (IsRevoked)
        {
            return;
        }

        if (seenAtUtc < CreatedAtUtc)
        {
            throw new ArgumentException(
                "Session last-seen time cannot be earlier than its creation time.",
                nameof(seenAtUtc));
        }

        LastSeenAtUtc =
            seenAtUtc;

        if (!string.IsNullOrWhiteSpace(
                ipAddress))
        {
            IpAddress =
                ipAddress;
        }

        if (!string.IsNullOrWhiteSpace(
                userAgent))
        {
            UserAgent =
                userAgent;
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

        if (revokedAtUtc < CreatedAtUtc)
        {
            throw new ArgumentException(
                "Session revocation time cannot be earlier than its creation time.",
                nameof(revokedAtUtc));
        }

        RevokedAtUtc =
            revokedAtUtc;

        RevocationReason =
            reason;
    }
}
