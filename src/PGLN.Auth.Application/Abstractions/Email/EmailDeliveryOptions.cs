namespace PGLN.Auth.Application.Abstractions.Email;

public sealed class EmailDeliveryOptions
{
    public string ConfirmationBaseUrl { get; init; } =
        "https://localhost/confirm-email";

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
    }
}
