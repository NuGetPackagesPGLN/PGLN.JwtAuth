using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.ResendEmailConfirmation;

public sealed record ResendEmailConfirmationCommand(
    string Email)
    : ICommand<Result>;
