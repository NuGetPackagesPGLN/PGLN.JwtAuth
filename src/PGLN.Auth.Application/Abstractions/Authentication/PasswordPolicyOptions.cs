namespace PGLN.Auth.Application.Abstractions.Authentication;

public sealed class PasswordPolicyOptions
{
    public int MinimumLength { get; init; } = 12;

    public int MaximumLength { get; init; } = 128;

    public bool RequireUppercase { get; init; }

    public bool RequireLowercase { get; init; }

    public bool RequireDigit { get; init; }

    public bool RequireNonAlphanumeric { get; init; }

    public void Validate()
    {
        if (MinimumLength <= 0)
        {
            throw new InvalidOperationException(
                "Password minimum length must be greater than zero.");
        }

        if (MaximumLength < MinimumLength)
        {
            throw new InvalidOperationException(
                "Password maximum length cannot be less than the minimum length.");
        }
    }
}
