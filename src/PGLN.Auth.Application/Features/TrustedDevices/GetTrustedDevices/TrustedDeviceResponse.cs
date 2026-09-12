using PGLN.Auth.Domain.TrustedDevices;

namespace PGLN.Auth.Application.Features.TrustedDevices.GetTrustedDevices;

public sealed record TrustedDeviceResponse(
    TrustedDeviceId Id,
    string? DeviceName,
    DateTimeOffset TrustedAtUtc,
    DateTimeOffset? RevokedAtUtc,
    bool IsTrusted);
