using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.ResetPassword;

public sealed record ResetPasswordCommand(
    string Email,
    string ResetToken,
    string NewPassword)
    : ICommand<Result>;
