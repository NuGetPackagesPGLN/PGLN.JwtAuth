namespace PGLN.Auth.Application.Abstractions.ExternalAuthentication;

public interface IExternalAuthenticationStateProtector
{
    string Protect(
        string provider,
        string redirectUri,
        string codeVerifier);

    ExternalAuthenticationState Unprotect(
        string protectedState);
}

public sealed record ExternalAuthenticationState(
    string Provider,
    string RedirectUri,
    string CodeVerifier);
