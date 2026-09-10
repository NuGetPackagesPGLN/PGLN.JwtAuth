namespace PGLN.Auth.Application.Abstractions.Email;

public interface IEmailTemplateRenderer
{
    EmailMessage RenderEmailConfirmation(
        string email,
        string verificationUrl);

    EmailMessage RenderWelcomeEmail(
        string email);

    EmailMessage RenderPasswordReset(
        string email,
        string resetUrl);
}
