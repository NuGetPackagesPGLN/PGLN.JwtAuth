namespace PGLN.Auth.Application.Abstractions.ExternalAuthentication;

public interface IExternalAuthenticationStateProtector
{
    string Protect(
        string provider,
        string redirectUri,
        string codeVerifier,
        string deviceIdHash,
        string? deviceName);

    ExternalAuthenticationState Unprotect(
        string protectedState);
}

public sealed record ExternalAuthenticationState(
    string Provider,
    string RedirectUri,
    string CodeVerifier,
    string DeviceIdHash,
    string? DeviceName);
