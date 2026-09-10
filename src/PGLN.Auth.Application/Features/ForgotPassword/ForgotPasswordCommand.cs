using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.ForgotPassword;

public sealed record ForgotPasswordCommand(
    string Email)
    : ICommand<Result>;
