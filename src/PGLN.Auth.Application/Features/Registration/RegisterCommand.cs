using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.Registration;

public sealed record RegisterCommand(
    string Email,
    string Password)
    : ICommand<Result<RegisterResult>>;