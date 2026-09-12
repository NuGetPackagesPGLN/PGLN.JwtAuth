namespace PGLN.Auth.Application.Abstractions.Email;

public interface IEmailTemplateRenderer
{
    EmailMessage RenderEmailConfirmation(
        string email,
        string verificationUrl);

    EmailMessage RenderEmailChangeConfirmation(
        string email,
        string verificationUrl);

    EmailMessage RenderWelcomeEmail(
        string email);

    EmailMessage RenderPasswordReset(
        string email,
        string resetUrl);

    EmailMessage RenderPasswordChanged(
        string email,
        DateTimeOffset changedAtUtc);

    EmailMessage RenderNewDeviceLogin(
        string email,
        string? deviceName,
        string? ipAddress,
        string? userAgent,
        DateTimeOffset occurredAtUtc);

    EmailMessage RenderAccountLocked(
        string email,
        DateTimeOffset lockedUntilUtc,
        DateTimeOffset occurredAtUtc);
}
