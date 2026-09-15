using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.ExternalAuthentication;

public static class ExternalAuthenticationErrors
{
    public static readonly Error ProviderNotSupported =
        new(
            "ExternalAuthentication.ProviderNotSupported",
            "The external authentication provider is not supported.");

    public static readonly Error IdentityInvalid =
        new(
            "ExternalAuthentication.IdentityInvalid",
            "The external identity could not be validated.");

    public static readonly Error ProviderFailure =
        new(
            "ExternalAuthentication.ProviderFailure",
            "The external authentication provider could not complete the authentication request.");

    public static readonly Error ProviderRejected =
        new(
            "ExternalAuthentication.ProviderRejected",
            "The external authentication provider rejected the request.");

    public static readonly Error RedirectUriNotAllowed =
        new(
            "ExternalAuthentication.RedirectUriNotAllowed",
            "The external authentication redirect URI is not allowed.");

    public static readonly Error InvalidState =
        new(
            "ExternalAuthentication.InvalidState",
            "The external authentication state is invalid or has been tampered with.");

    public static readonly Error AuthorizationDenied =
        new(
            "ExternalAuthentication.AuthorizationDenied",
            "The external authentication request was denied.");

    public static readonly Error AuthorizationCodeMissing =
        new(
            "ExternalAuthentication.AuthorizationCodeMissing",
            "The external authentication authorization code is required.");

    public static readonly Error EmailRequired =
        new(
            "ExternalAuthentication.EmailRequired",
            "The external provider did not provide an email address.");

    public static readonly Error EmailNotVerified =
        new(
            "ExternalAuthentication.EmailNotVerified",
            "The external provider email address is not verified.");

    public static readonly Error AccountLinkRequired =
        new(
            "ExternalAuthentication.AccountLinkRequired",
            "An account already exists with this email address. Sign in to the existing account before linking this external provider.");
}

