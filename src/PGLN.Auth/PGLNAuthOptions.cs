using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Abstractions.Email;
using PGLN.Auth.Application.Configuration;
using PGLN.Auth.Application.Features.ForgotPassword;
using PGLN.Auth.Application.Features.Login;

namespace PGLN.Auth;

/// <summary>
/// Configures application-level behavior for PGLN.Auth.
/// </summary>
public sealed class PGLNAuthOptions
{
    /// <summary>
    /// Configures password validation requirements.
    /// </summary>
    public PasswordPolicyOptions PasswordPolicy { get; set; } =
        new();

    /// <summary>
    /// Configures email verification token behavior.
    /// </summary>
    public EmailVerificationOptions EmailVerification { get; set; } =
        new();

    /// <summary>
    /// Configures URLs used in authentication emails.
    /// </summary>
    public EmailDeliveryOptions EmailDelivery { get; set; } =
        new();

    /// <summary>
    /// Configures refresh token behavior.
    /// </summary>
    public RefreshTokenOptions RefreshTokens { get; set; } =
        new();

    /// <summary>
    /// Configures account lockout behavior after failed login attempts.
    /// </summary>
    public AccountLockoutOptions AccountLockout { get; set; } =
        new();

    /// <summary>
    /// Configures per-email login throttling.
    /// </summary>
    public LoginEmailThrottleOptions LoginEmailThrottle { get; set; } =
        new();

    /// <summary>
    /// Configures step-up authentication challenges.
    /// </summary>
    public StepUpChallengeOptions StepUp { get; set; } =
        new();

    /// <summary>
    /// Configures password reset token behavior.
    /// </summary>
    public PasswordResetOptions PasswordReset { get; set; } =
        new();
}
