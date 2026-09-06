using PGLN.Auth.Application.Abstractions.Authentication;

namespace PGLN.Auth.Application.Tests.TestDoubles;

internal sealed class FakeTokenHasher
    : ITokenHasher
{
    public string Hash(string token)
    {
        return $"hashed::{token}";
    }
}
