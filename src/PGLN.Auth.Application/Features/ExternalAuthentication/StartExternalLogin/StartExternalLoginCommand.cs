using PGLN.Auth.Application.Common;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Domain.ExternalLogins;

namespace PGLN.Auth.Application.Features.ExternalAuthentication.StartExternalLogin;

public sealed record StartExternalLoginCommand(
    ExternalLoginProvider Provider,
    string RedirectUri)
    : ICommand<Result<StartExternalLoginResult>>;

