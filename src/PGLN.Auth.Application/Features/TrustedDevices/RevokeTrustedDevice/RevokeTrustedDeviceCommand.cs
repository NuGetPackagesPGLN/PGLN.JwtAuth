using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Common;
using PGLN.Auth.Domain.TrustedDevices;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Features.TrustedDevices.RevokeTrustedDevice;

public sealed record RevokeTrustedDeviceCommand(
    UserId UserId,
    TrustedDeviceId TrustedDeviceId)
    : ICommand<Result>;
