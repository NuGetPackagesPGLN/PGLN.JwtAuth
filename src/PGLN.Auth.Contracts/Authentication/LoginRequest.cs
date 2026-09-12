namespace PGLN.Auth.Contracts.Authentication;

public sealed record LoginRequest(
    string Email,
    string Password,
    string DeviceIdHash,
    string? DeviceName);
