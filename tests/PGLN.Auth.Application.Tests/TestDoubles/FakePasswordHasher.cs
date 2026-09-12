using PGLN.Auth.Application.Abstractions.Authentication;

namespace PGLN.Auth.Application.Tests.TestDoubles;

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string? LastPasswordHashed { get; private set; }

    public string? LastPasswordVerified { get; private set; }

    public string? LastPasswordHashVerified { get; private set; }

    public string HashResult { get; set; } =
        "hashed-password";

    public bool? VerifyResult { get; set; }

    public string Hash(
        string password)
    {
        LastPasswordHashed =
            password;

        return HashResult;
    }

    public bool Verify(
        string password,
        string passwordHash)
    {
        LastPasswordVerified =
            password;

        LastPasswordHashVerified =
            passwordHash;

        if (VerifyResult.HasValue)
        {
            return VerifyResult.Value;
        }

        return passwordHash ==
            Hash(
                password);
    }
}
