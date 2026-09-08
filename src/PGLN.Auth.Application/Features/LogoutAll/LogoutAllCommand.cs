using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.LogoutAll;

public sealed record LogoutAllCommand(
    string RefreshToken)
    : ICommand<Result>;
