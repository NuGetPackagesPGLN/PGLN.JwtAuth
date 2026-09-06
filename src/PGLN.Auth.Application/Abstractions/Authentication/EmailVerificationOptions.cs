namespace PGLN.Auth.Application.Abstractions.Authentication;

public sealed class EmailVerificationOptions
{
    public TimeSpan TokenLifetime { get; init; } =
        TimeSpan.FromHours(24);

    public void Validate()
    {
        if (TokenLifetime <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "Email verification token lifetime must be greater than zero.");
        }
    }
}
