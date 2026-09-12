using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.ChangeEmail;

public sealed record ConfirmEmailChangeCommand(
    string Token)
    : ICommand<Result>;
