using Microsoft.AspNetCore.Identity;
using PGLN.Auth.Application.Abstractions.Authentication;

namespace PGLN.Auth.Infrastructure.Passwords;

public sealed class PasswordHasher : IPasswordHasher
{
    private static readonly PasswordHasher<object> InnerHasher = new();

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        return InnerHasher.HashPassword(
            user: null!,
            password);
    }

    public bool Verify(
        string password,
        string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        var result =
            InnerHasher.VerifyHashedPassword(
                user: null!,
                hashedPassword: passwordHash,
                providedPassword: password);

        return result is
            PasswordVerificationResult.Success
            or
            PasswordVerificationResult.SuccessRehashNeeded;
    }
}
