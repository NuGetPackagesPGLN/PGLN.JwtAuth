using PGLN.Auth.Application.Abstractions.Authentication;

namespace PGLN.Auth.Application.Tests.TestDoubles;

internal sealed class FakeLoginPasswordHasher
    : IPasswordHasher
{
    public int HashCallCount { get; private set; }

    public int VerifyCallCount { get; private set; }

    public string? LastPassword { get; private set; }

    public string? LastPasswordHash { get; private set; }

    public string Hash(
        string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            password);

        HashCallCount++;

        return $"hashed::{password}";
    }

    public bool Verify(
        string password,
        string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            password);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            passwordHash);

        VerifyCallCount++;

        LastPassword =
            password;

        LastPasswordHash =
            passwordHash;

        return passwordHash ==
            $"hashed::{password}";
    }
}
