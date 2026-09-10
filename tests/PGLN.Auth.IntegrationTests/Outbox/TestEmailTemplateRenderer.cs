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
}

