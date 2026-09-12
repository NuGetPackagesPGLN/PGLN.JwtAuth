using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Common;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Features.Sessions.GetSessions;

public sealed record GetSessionsQuery(
    UserId UserId)
    : ICommand<Result<IReadOnlyCollection<SessionItem>>>;
