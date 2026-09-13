using PGLN.Auth.Application.Abstractions.Email;

namespace PGLN.Auth.Application.Tests.TestDoubles;

internal sealed class FakeEmailTemplateRenderer
    : IEmailTemplateRenderer
{
    public string? LastConfirmationUrl { get; private set; }

    public EmailMessage RenderEmailConfirmation(
        string email,
        string verificationUrl)
    {
        LastConfirmationUrl =
            verificationUrl;

        return new EmailMessage(
            email,
            "Confirm",
            $"<p>{verificationUrl}</p>",
            verificationUrl);
    }

    public EmailMessage RenderEmailChangeConfirmation(
        string email,
        string verificationUrl)
    {
        return new EmailMessage(
            email,
            "Confirm your new email",
            $"<p>{verificationUrl}</p>",
            verificationUrl);
    }
    public EmailMessage RenderWelcomeEmail(
        string email)
    {
        return new EmailMessage(
            email,
            "Welcome",
            "<p>Welcome</p>",
            "Welcome");
    }

    public EmailMessage RenderPasswordReset(
        string email,
        string resetUrl)
    {
        return new EmailMessage(
            email,
            "Reset password",
            $"<p>{resetUrl}</p>",
            resetUrl);
    }

    public EmailMessage RenderPasswordChanged(
        string email,
        DateTimeOffset changedAtUtc)
    {
        return new EmailMessage(
            email,
            "Your password was changed",
            $"<p>{changedAtUtc:O}</p>",
            changedAtUtc.ToString("O"));
    }

    public EmailMessage RenderNewDeviceLogin(
        string email,
        string? deviceName,
        string? ipAddress,
        string? userAgent,
        DateTimeOffset occurredAtUtc)
    {
        return new EmailMessage(
            email,
            "New device login detected",
            $"<p>{deviceName} | {ipAddress} | {userAgent} | {occurredAtUtc:O}</p>",
            $"{deviceName} | {ipAddress} | {userAgent} | {occurredAtUtc:O}");
    }

    public EmailMessage RenderStepUpVerificationCode(
        string email,
        string code,
        string? deviceName,
        string? ipAddress,
        string? userAgent,
        DateTimeOffset expiresAtUtc)
    {
        return new EmailMessage(
            email,
            "Verify your sign-in",
            $"<p>{code}</p>",
            code);
    }
    public EmailMessage RenderAccountLocked(
        string email,
        DateTimeOffset lockedUntilUtc,
        DateTimeOffset occurredAtUtc)
    {
        return new EmailMessage(
            email,
            "Your account has been temporarily locked",
            $"<p>{occurredAtUtc:O} | {lockedUntilUtc:O}</p>",
            $"{occurredAtUtc:O} | {lockedUntilUtc:O}");
    }

    public EmailMessage RenderEmailChangedNotification(
        string oldEmail,
        string newEmail,
        DateTimeOffset changedAtUtc)
    {
        return new EmailMessage(
            oldEmail,
            "Your email address was changed",
            $"<p>{oldEmail} -> {newEmail}</p>",
            $"{oldEmail} -> {newEmail}");
    }}



