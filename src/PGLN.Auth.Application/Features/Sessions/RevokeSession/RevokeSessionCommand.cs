using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Common;
using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Features.Sessions.RevokeSession;

public sealed record RevokeSessionCommand(
    UserId UserId,
    AuthSessionId SessionId)
    : ICommand<Result>;
