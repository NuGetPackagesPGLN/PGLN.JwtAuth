using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Features.TrustedDevices.GetTrustedDevices;

public sealed record GetTrustedDevicesQuery(
    UserId UserId)
    : IQuery<IReadOnlyCollection<TrustedDeviceResponse>>;
