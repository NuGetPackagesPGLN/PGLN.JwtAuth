using PGLN.Auth.Application.Abstractions.Authentication;

namespace PGLN.Auth.Application.Tests.TestDoubles;

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string? LastPasswordHashed { get; private set; }

    public string HashResult { get; set; } = "hashed-password";

    public string Hash(string password)
    {
        LastPasswordHashed = password;

        return HashResult;
    }

    public bool Verify(
        string password,
        string passwordHash)
    {
        return passwordHash == Hash(password);
    }
}
