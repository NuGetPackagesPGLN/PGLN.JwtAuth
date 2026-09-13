namespace PGLN.Auth.Application.Configuration;

public sealed class StepUpSecurityOptions
{
    public const string SectionName =
        "PGLNAuth:StepUpSecurity";

    public string HmacSecret { get; init; } =
        string.Empty;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(HmacSecret))
        {
            throw new InvalidOperationException(
                "Step-up HMAC secret is required.");
        }

        if (HmacSecret.Length < 32)
        {
            throw new InvalidOperationException(
                "Step-up HMAC secret must be at least 32 characters long.");
        }
    }
}
