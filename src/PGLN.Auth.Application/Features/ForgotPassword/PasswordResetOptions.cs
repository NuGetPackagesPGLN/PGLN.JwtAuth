namespace PGLN.Auth.Application.Features.ForgotPassword;

public sealed class PasswordResetOptions
{
    public TimeSpan TokenLifetime { get; init; } =
        TimeSpan.FromMinutes(30);

    public void Validate()
    {
        if (TokenLifetime <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "Password reset token lifetime must be greater than zero.");
        }

        if (TokenLifetime > TimeSpan.FromHours(24))
        {
            throw new InvalidOperationException(
                "Password reset token lifetime cannot exceed 24 hours.");
        }
    }
}
