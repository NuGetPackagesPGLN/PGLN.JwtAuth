using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Events.Email;

public sealed record NewDeviceLoginNotificationRequested(
    Guid EventId,
    UserId UserId,
    string Email,
    string DeviceIdHash,
    string? DeviceName,
    string? IpAddress,
    string? UserAgent,
    DateTimeOffset OccurredAtUtc)
    : IIntegrationEvent;
