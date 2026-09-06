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
            "Confirm your email",
            $"<a href=\"{verificationUrl}\">Confirm email</a>",
            $"Confirm your email: {verificationUrl}");
    }

    public EmailMessage RenderWelcomeEmail(
        string email)
    {
        return new EmailMessage(
            email,
            "Welcome",
            "<p>Welcome to PGLN.Auth.</p>",
            "Welcome to PGLN.Auth.");
    }
}
