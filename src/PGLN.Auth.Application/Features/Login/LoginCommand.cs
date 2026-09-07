using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.Login;

public sealed record LoginCommand(
    string Email,
    string Password)
    : ICommand<Result<LoginResult>>;
