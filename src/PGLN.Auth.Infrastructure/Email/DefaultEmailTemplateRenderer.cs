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

    public EmailMessage RenderEmailChangeConfirmation(
        string email,
        string verificationUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(verificationUrl);

        const string subject =
            "Confirm your new email address";

        var textBody =
            $"""
            We received a request to change the email address
            for your PGLN Auth account.

            Confirm your new email address by opening the following link:

            {verificationUrl}

            Your account email will not change until this link
            has been confirmed.

            If you did not request this change,
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
                <title>Confirm your new email address</title>
            </head>
            <body>
                <h1>Confirm your new email address</h1>

                <p>
                    We received a request to change the email
                    address for your PGLN Auth account.
                </p>

                <p>
                    <a href="{verificationUrl}">
                        Confirm new email
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
                    Your account email will not change until
                    this link has been confirmed.
                </p>

                <p>
                    If you did not request this change,
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


    public EmailMessage RenderPasswordChanged(
        string email,
        DateTimeOffset changedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        const string subject =
            "Your password was changed";

        var textBody =
            $"""
            The password for your PGLN Auth account was changed.

            Time:
            {changedAtUtc:yyyy-MM-dd HH:mm:ss} UTC

            If you made this change, no further action is required.

            If you did not change your password,
            secure your account immediately.
            """;

        var htmlBody =
            $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport"
                      content="width=device-width, initial-scale=1">
                <title>Password changed</title>
            </head>
            <body>
                <h1>Your password was changed</h1>

                <p>
                    The password for your PGLN Auth account
                    was successfully changed.
                </p>

                <p>
                    <strong>Time:</strong>
                    {changedAtUtc:yyyy-MM-dd HH:mm:ss} UTC
                </p>

                <p>
                    If you made this change,
                    no further action is required.
                </p>

                <p>
                    If you did not change your password,
                    secure your account immediately.
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
    public EmailMessage RenderNewDeviceLogin(
        string email,
        string? deviceName,
        string? ipAddress,
        string? userAgent,
        DateTimeOffset occurredAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        const string subject =
            "New device login detected";

        var displayDevice =
            string.IsNullOrWhiteSpace(deviceName)
                ? "Unknown device"
                : deviceName;

        var displayIpAddress =
            string.IsNullOrWhiteSpace(ipAddress)
                ? "Unknown"
                : ipAddress;

        var displayUserAgent =
            string.IsNullOrWhiteSpace(userAgent)
                ? "Unknown"
                : userAgent;

        var textBody =
            $"""
            A new device signed in to your PGLN Auth account.

            Device:
            {displayDevice}

            IP address:
            {displayIpAddress}

            Browser / client:
            {displayUserAgent}

            Time:
            {occurredAtUtc:yyyy-MM-dd HH:mm:ss} UTC

            If this was you, no further action is required.

            If you do not recognize this login,
            secure your account immediately.
            """;

        var htmlBody =
            $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport"
                      content="width=device-width, initial-scale=1">
                <title>New device login detected</title>
            </head>
            <body>
                <h1>New device login detected</h1>

                <p>
                    A new device signed in to your
                    PGLN Auth account.
                </p>

                <p>
                    <strong>Device:</strong>
                    {displayDevice}
                </p>

                <p>
                    <strong>IP address:</strong>
                    {displayIpAddress}
                </p>

                <p>
                    <strong>Browser / client:</strong>
                    {displayUserAgent}
                </p>

                <p>
                    <strong>Time:</strong>
                    {occurredAtUtc:yyyy-MM-dd HH:mm:ss} UTC
                </p>

                <p>
                    If this was you,
                    no further action is required.
                </p>

                <p>
                    If you do not recognize this login,
                    secure your account immediately.
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
    public EmailMessage RenderStepUpVerificationCode(
        string email,
        string code,
        string? deviceName,
        string? ipAddress,
        string? userAgent,
        DateTimeOffset expiresAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            email);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            code);

        const string subject =
            "Verify your sign-in";

        var displayDevice =
            string.IsNullOrWhiteSpace(deviceName)
                ? "Unknown device"
                : deviceName;

        var displayIpAddress =
            string.IsNullOrWhiteSpace(ipAddress)
                ? "Unknown"
                : ipAddress;

        var displayUserAgent =
            string.IsNullOrWhiteSpace(userAgent)
                ? "Unknown"
                : userAgent;

        var textBody =
            $"""
            We need to verify this sign-in to your PGLN Auth account.

            Verification code:

            {code}

            Device:
            {displayDevice}

            IP address:
            {displayIpAddress}

            Browser / client:
            {displayUserAgent}

            This code expires at:
            {expiresAtUtc:yyyy-MM-dd HH:mm:ss} UTC

            Do not share this code with anyone.

            If you did not attempt to sign in,
            you can ignore this email and secure your account.
            """;

        var htmlBody =
            $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport"
                      content="width=device-width, initial-scale=1">
                <title>Verify your sign-in</title>
            </head>
            <body>
                <h1>Verify your sign-in</h1>

                <p>
                    We need to verify this sign-in to your
                    PGLN Auth account.
                </p>

                <p>
                    <strong>Verification code:</strong>
                </p>

                <p>
                    <strong>{code}</strong>
                </p>

                <p>
                    <strong>Device:</strong>
                    {System.Net.WebUtility.HtmlEncode(displayDevice)}
                </p>

                <p>
                    <strong>IP address:</strong>
                    {System.Net.WebUtility.HtmlEncode(displayIpAddress)}
                </p>

                <p>
                    <strong>Browser / client:</strong>
                    {System.Net.WebUtility.HtmlEncode(displayUserAgent)}
                </p>

                <p>
                    <strong>Expires:</strong>
                    {expiresAtUtc:yyyy-MM-dd HH:mm:ss} UTC
                </p>

                <p>
                    Do not share this code with anyone.
                </p>

                <p>
                    If you did not attempt to sign in,
                    you can ignore this email and secure your account.
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
    public EmailMessage RenderAccountLocked(
        string email,
        DateTimeOffset lockedUntilUtc,
        DateTimeOffset occurredAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        const string subject =
            "Your account has been temporarily locked";

        var textBody =
            $"""
            Your PGLN Auth account has been temporarily locked
            because of repeated unsuccessful login attempts.

            Lock occurred:
            {occurredAtUtc:yyyy-MM-dd HH:mm:ss} UTC

            Account unlocks:
            {lockedUntilUtc:yyyy-MM-dd HH:mm:ss} UTC

            You can try signing in again after the lockout expires.

            If these login attempts were not yours,
            secure your account before signing in again.
            """;

        var htmlBody =
            $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport"
                      content="width=device-width, initial-scale=1">
                <title>Account temporarily locked</title>
            </head>
            <body>
                <h1>Your account has been temporarily locked</h1>

                <p>
                    Your PGLN Auth account has been temporarily
                    locked because of repeated unsuccessful
                    login attempts.
                </p>

                <p>
                    <strong>Lock occurred:</strong>
                    {occurredAtUtc:yyyy-MM-dd HH:mm:ss} UTC
                </p>

                <p>
                    <strong>Account unlocks:</strong>
                    {lockedUntilUtc:yyyy-MM-dd HH:mm:ss} UTC
                </p>

                <p>
                    You can try signing in again after
                    the lockout expires.
                </p>

                <p>
                    If these login attempts were not yours,
                    secure your account before signing in again.
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

    public EmailMessage RenderEmailChangedNotification(
        string oldEmail,
        string newEmail,
        DateTimeOffset changedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            oldEmail);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            newEmail);

        var subject =
            "Your email address was changed";

        var textBody =
            $"""
            Your account email address was changed.

            Previous email:
            {oldEmail}

            New email:
            {newEmail}

            Changed at:
            {changedAtUtc:O}

            If you made this change, no action is required.

            If you did not make this change, secure your account immediately.
            """;

        var htmlBody =
            $"""
            <p>Your account email address was changed.</p>

            <p>
                <strong>Previous email:</strong><br />
                {System.Net.WebUtility.HtmlEncode(oldEmail)}
            </p>

            <p>
                <strong>New email:</strong><br />
                {System.Net.WebUtility.HtmlEncode(newEmail)}
            </p>

            <p>
                <strong>Changed at:</strong><br />
                {changedAtUtc:O}
            </p>

            <p>
                If you made this change, no action is required.
            </p>

            <p>
                <strong>
                    If you did not make this change, secure your account immediately.
                </strong>
            </p>
            """;

        return new EmailMessage(
            oldEmail,
            subject,
            htmlBody,
            textBody);
    }}







