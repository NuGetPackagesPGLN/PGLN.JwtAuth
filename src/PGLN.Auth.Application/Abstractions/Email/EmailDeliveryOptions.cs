namespace PGLN.Auth.Application.Abstractions.Email;

public sealed class EmailDeliveryOptions
{
    public string ConfirmationBaseUrl { get; init; } =
        "https://localhost/confirm-email";

    public string EmailChangeConfirmationBaseUrl { get; init; } =
        "https://localhost/confirm-email-change";

    public string ResetPasswordBaseUrl { get; init; } =
        "https://localhost/reset-password";

    public void Validate()
    {
        if (!Uri.TryCreate(
            ConfirmationBaseUrl,
            UriKind.Absolute,
            out _))
        {
            throw new InvalidOperationException(
                "Email confirmation base URL must be a valid absolute URI.");
        }

        if (!Uri.TryCreate(
            EmailChangeConfirmationBaseUrl,
            UriKind.Absolute,
            out _))
        {
            throw new InvalidOperationException(
                "Email change confirmation base URL must be a valid absolute URI.");
        }

        if (!Uri.TryCreate(
            ResetPasswordBaseUrl,
            UriKind.Absolute,
            out _))
        {
            throw new InvalidOperationException(
                "Password reset base URL must be a valid absolute URI.");
        }
    }
}
