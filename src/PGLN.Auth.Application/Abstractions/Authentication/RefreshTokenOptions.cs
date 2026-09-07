namespace PGLN.Auth.Application.Abstractions.Authentication;

public sealed class RefreshTokenOptions
{
    public TimeSpan TokenLifetime { get; init; } =
        TimeSpan.FromDays(30);

    public void Validate()
    {
        if (TokenLifetime <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "Refresh token lifetime must be greater than zero.");
        }
    }
}
