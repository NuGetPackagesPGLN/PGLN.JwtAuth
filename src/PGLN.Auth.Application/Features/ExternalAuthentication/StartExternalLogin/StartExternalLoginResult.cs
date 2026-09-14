namespace PGLN.Auth.Application.Features.ExternalAuthentication.StartExternalLogin;

public sealed record StartExternalLoginResult(
    string AuthorizationUrl);
