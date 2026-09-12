using PGLN.Auth.Domain.Sessions;

namespace PGLN.Auth.Application.Features.Sessions.GetSessions;

public sealed record SessionItem(
    AuthSessionId Id,
    string DeviceIdHash,
    string? DeviceName,
    string? IpAddress,
    string? UserAgent,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastSeenAtUtc,
    bool IsRevoked);
