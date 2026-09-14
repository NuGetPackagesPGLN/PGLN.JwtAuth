using PGLN.Auth.Application.Abstractions.ExternalAuthentication;

namespace PGLN.Auth.Application.Tests.TestDoubles;

public sealed class FakeExternalAuthenticationStateProtector
    : IExternalAuthenticationStateProtector
{
    public ExternalAuthenticationState State { get; set; } =
        new(
            "Google",
            "https://localhost/signin-google",
            "test-code-verifier");

    public bool ThrowOnUnprotect { get; set; }

    public string? LastProtectedState { get; private set; }

    public string Protect(
        string provider,
        string redirectUri,
        string codeVerifier)
    {
        return "protected-state";
    }

    public ExternalAuthenticationState Unprotect(
        string protectedState)
    {
        LastProtectedState =
            protectedState;

        if (ThrowOnUnprotect)
        {
            throw new InvalidOperationException(
                "Invalid protected state.");
        }

        return State;
    }
}
