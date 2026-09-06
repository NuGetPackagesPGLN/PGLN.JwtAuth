using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.EmailConfirmation;

public sealed record ConfirmEmailCommand(
    string Token)
    : ICommand<Result<ConfirmEmailResult>>;
