using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.Login;

public sealed record LoginCommand(
    string Email,
    string Password,
    string DeviceIdHash,
    string? DeviceName,
    string? IpAddress,
    string? UserAgent)
    : ICommand<Result<LoginResult>>;
