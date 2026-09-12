using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Common;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Features.ChangeEmail;

public sealed record RequestEmailChangeCommand(
    UserId UserId,
    string NewEmail,
    string CurrentPassword)
    : ICommand<Result>;
