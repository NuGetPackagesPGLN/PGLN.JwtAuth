using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using PGLN.Auth.Application.Abstractions.ExternalAuthentication;

namespace PGLN.Auth.Infrastructure.Authentication.External;

public sealed class ExternalAuthenticationStateProtector
    : IExternalAuthenticationStateProtector
{
    private const string Purpose =
        "PGLN.Auth.ExternalAuthentication.State.v1";

    private static readonly TimeSpan DefaultStateLifetime =
        TimeSpan.FromMinutes(10);

    private readonly ITimeLimitedDataProtector _protector;
    private readonly TimeSpan _stateLifetime;

    public ExternalAuthenticationStateProtector(
        IDataProtectionProvider dataProtectionProvider)
        : this(
            dataProtectionProvider,
            DefaultStateLifetime)
    {
    }

    public ExternalAuthenticationStateProtector(
        IDataProtectionProvider dataProtectionProvider,
        TimeSpan stateLifetime)
    {
        ArgumentNullException.ThrowIfNull(
            dataProtectionProvider);

        if (stateLifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stateLifetime));
        }

        _protector =
            dataProtectionProvider
                .CreateProtector(
                    Purpose)
                .ToTimeLimitedDataProtector();

        _stateLifetime =
            stateLifetime;
    }

    public string Protect(
        string provider,
        string redirectUri,
        string codeVerifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            provider);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            redirectUri);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            codeVerifier);

        var state =
            new ExternalAuthenticationState(
                provider,
                redirectUri,
                codeVerifier);

        var json =
            JsonSerializer.Serialize(
                state);

        return _protector.Protect(
            json,
            _stateLifetime);
    }

    public ExternalAuthenticationState Unprotect(
        string protectedState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            protectedState);

        var json =
            _protector.Unprotect(
                protectedState,
                out _);

        var state =
            JsonSerializer.Deserialize<
                ExternalAuthenticationState>(
                json);

        if (state is null)
        {
            throw new InvalidOperationException(
                "External authentication state is invalid.");
        }

        if (string.IsNullOrWhiteSpace(
                state.Provider) ||
            string.IsNullOrWhiteSpace(
                state.RedirectUri) ||
            string.IsNullOrWhiteSpace(
                state.CodeVerifier))
        {
            throw new InvalidOperationException(
                "External authentication state is incomplete.");
        }

        return state;
    }
}


