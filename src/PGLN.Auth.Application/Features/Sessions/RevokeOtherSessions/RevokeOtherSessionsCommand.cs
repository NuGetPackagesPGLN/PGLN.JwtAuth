using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Common;
using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Features.Sessions.RevokeOtherSessions;

public sealed record RevokeOtherSessionsCommand(
    UserId UserId,
    AuthSessionId CurrentSessionId)
    : ICommand<Result>;
