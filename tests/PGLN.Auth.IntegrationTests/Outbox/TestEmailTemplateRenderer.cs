using PGLN.Auth.Application.Abstractions.Email;

namespace PGLN.Auth.IntegrationTests.Outbox;

internal sealed class TestEmailTemplateRenderer
    : IEmailTemplateRenderer
{
    public EmailMessage RenderEmailConfirmation(
        string email,
        string verificationUrl)
    {
        return new EmailMessage(
            email,
            "Confirm your email",
            $"<a href=\"{verificationUrl}\">Confirm</a>",
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
            "<p>Welcome.</p>",
            "Welcome.");
    }

    public EmailMessage RenderPasswordReset(
        string email,
        string resetUrl)
    {
        return new EmailMessage(
            email,
            "Reset your password",
            $"<p>Reset password: {resetUrl}</p>",
            $"Reset password: {resetUrl}");
    }

    public EmailMessage RenderPasswordChanged(
        string email,
        DateTimeOffset changedAtUtc)
    {
        return new EmailMessage(
            email,
            "Your password was changed",
            $"<p>Password changed at {changedAtUtc:O}</p>",
            $"Password changed at {changedAtUtc:O}");
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
}

