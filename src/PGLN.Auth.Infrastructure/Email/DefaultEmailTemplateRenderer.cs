using PGLN.Auth.Application.Abstractions.Email;

namespace PGLN.Auth.Infrastructure.Email;

public sealed class DefaultEmailTemplateRenderer
    : IEmailTemplateRenderer
{
    public EmailMessage RenderEmailConfirmation(
        string email,
        string verificationUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(verificationUrl);

        const string subject =
            "Confirm your email address";

        var textBody =
            $"""
            Welcome to PGLN Auth.

            Please confirm your email address by opening the following link:

            {verificationUrl}

            If you did not create this account, you can ignore this email.
            """;

        var htmlBody =
            $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport"
                      content="width=device-width, initial-scale=1">
                <title>Confirm your email</title>
            </head>
            <body>
                <h1>Confirm your email address</h1>

                <p>
                    Welcome to PGLN Auth.
                </p>

                <p>
                    Please confirm your email address to finish
                    setting up your account.
                </p>

                <p>
                    <a href="{verificationUrl}">
                        Confirm email
                    </a>
                </p>

                <p>
                    If the button does not work, copy and paste
                    this URL into your browser:
                </p>

                <p>
                    {verificationUrl}
                </p>

                <p>
                    If you did not create this account,
                    you can ignore this email.
                </p>
            </body>
            </html>
            """;

        return CreateMessage(
            email,
            subject,
            textBody,
            htmlBody);
    }

    public EmailMessage RenderWelcomeEmail(
        string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        const string subject =
            "Welcome to PGLN Auth";

        const string textBody =
            """
            Your email address has been confirmed.

            Welcome to PGLN Auth.

            Your account is now ready to use.
            """;

        const string htmlBody =
            """
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport"
                      content="width=device-width, initial-scale=1">
                <title>Welcome</title>
            </head>
            <body>
                <h1>Welcome to PGLN Auth</h1>

                <p>
                    Your email address has been confirmed.
                </p>

                <p>
                    Your account is now ready to use.
                </p>
            </body>
            </html>
            """;

        return CreateMessage(
            email,
            subject,
            textBody,
            htmlBody);
    }

    public EmailMessage RenderPasswordReset(
        string email,
        string resetUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(resetUrl);

        const string subject =
            "Reset your password";

        var textBody =
            $"""
            We received a request to reset your password.

            Open the following link to choose a new password:

            {resetUrl}

            If you did not request a password reset,
            you can ignore this email.
            """;

        var htmlBody =
            $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport"
                      content="width=device-width, initial-scale=1">
                <title>Reset your password</title>
            </head>
            <body>
                <h1>Reset your password</h1>

                <p>
                    We received a request to reset the password
                    for your account.
                </p>

                <p>
                    <a href="{resetUrl}">
                        Reset password
                    </a>
                </p>

                <p>
                    If the button does not work, copy and paste
                    this URL into your browser:
                </p>

                <p>
                    {resetUrl}
                </p>

                <p>
                    If you did not request a password reset,
                    you can safely ignore this email.
                </p>
            </body>
            </html>
            """;

        return CreateMessage(
            email,
            subject,
            textBody,
            htmlBody);
    }

    private static EmailMessage CreateMessage(
        string email,
        string subject,
        string textBody,
        string htmlBody)
    {
        return new EmailMessage(
            email,
            subject,
            htmlBody,
            textBody);
    }
}

