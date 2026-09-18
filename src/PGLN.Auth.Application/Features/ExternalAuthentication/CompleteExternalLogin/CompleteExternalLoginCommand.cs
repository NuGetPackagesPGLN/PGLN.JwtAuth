using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Common;
using PGLN.Auth.Domain.ExternalLogins;

namespace PGLN.Auth.Application.Features.ExternalAuthentication.CompleteExternalLogin;

public sealed record CompleteExternalLoginCommand(
    ExternalLoginProvider Provider,
    string? AuthorizationCode,
    string State,
    string? IpAddress,
    string? UserAgent,
    string? ProviderError)
    : ICommand<Result<CompleteExternalLoginResult>>;
