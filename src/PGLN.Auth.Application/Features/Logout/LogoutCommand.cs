using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.Logout;

public sealed record LogoutCommand(
    string RefreshToken)
    : ICommand<Result>;
