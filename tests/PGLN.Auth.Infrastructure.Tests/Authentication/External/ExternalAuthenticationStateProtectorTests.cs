using Microsoft.AspNetCore.DataProtection;
using PGLN.Auth.Infrastructure.Authentication.External;

namespace PGLN.Auth.Infrastructure.Tests.Authentication.External;

public sealed class ExternalAuthenticationStateProtectorTests
{
    [Fact]
    public void ProtectAndUnprotect_ShouldRoundTripState()
    {
        var dataProtectionProvider =
            DataProtectionProvider.Create(
                new DirectoryInfo(
                    Path.Combine(
                        Path.GetTempPath(),
                        $"pgln-auth-tests-{Guid.NewGuid():N}")));

        var protector =
            new ExternalAuthenticationStateProtector(
                dataProtectionProvider);

        var protectedState =
            protector.Protect(
                "Google",
                "https://example.com/auth/google/callback",
                "test-code-verifier");

        var result =
            protector.Unprotect(
                protectedState);

        Assert.Equal(
            "Google",
            result.Provider);

        Assert.Equal(
            "https://example.com/auth/google/callback",
            result.RedirectUri);

        Assert.Equal(
            "test-code-verifier",
            result.CodeVerifier);
    }

    [Fact]
    public void Protect_ShouldNotExposePlaintextState()
    {
        var dataProtectionProvider =
            DataProtectionProvider.Create(
                new DirectoryInfo(
                    Path.Combine(
                        Path.GetTempPath(),
                        $"pgln-auth-tests-{Guid.NewGuid():N}")));

        var protector =
            new ExternalAuthenticationStateProtector(
                dataProtectionProvider);

        var protectedState =
            protector.Protect(
                "Google",
                "https://example.com/auth/google/callback",
                "secret-code-verifier");

        Assert.DoesNotContain(
            "Google",
            protectedState);

        Assert.DoesNotContain(
            "secret-code-verifier",
            protectedState);
    }

    [Fact]
    public void Unprotect_WhenStateIsTamperedWith_ShouldThrow()
    {
        var dataProtectionProvider =
            DataProtectionProvider.Create(
                new DirectoryInfo(
                    Path.Combine(
                        Path.GetTempPath(),
                        $"pgln-auth-tests-{Guid.NewGuid():N}")));

        var protector =
            new ExternalAuthenticationStateProtector(
                dataProtectionProvider);

        var protectedState =
            protector.Protect(
                "Google",
                "https://example.com/auth/google/callback",
                "test-code-verifier");

        var tamperedState =
            protectedState + "tampered";

        Assert.ThrowsAny<Exception>(
            () => protector.Unprotect(
                tamperedState));
    }

    [Fact]
    public async Task Unprotect_WhenStateHasExpired_ShouldThrow()
    {
        var dataProtectionProvider =
            DataProtectionProvider.Create(
                new DirectoryInfo(
                    Path.Combine(
                        Path.GetTempPath(),
                        $"pgln-auth-tests-{Guid.NewGuid():N}")));

        var protector =
            new ExternalAuthenticationStateProtector(
                dataProtectionProvider,
                TimeSpan.FromMilliseconds(50));

        var protectedState =
            protector.Protect(
                "Google",
                "https://example.com/auth/google/callback",
                "test-code-verifier");

        await Task.Delay(
            150);

        Assert.ThrowsAny<Exception>(
            () =>
                protector.Unprotect(
                    protectedState));
    }}

