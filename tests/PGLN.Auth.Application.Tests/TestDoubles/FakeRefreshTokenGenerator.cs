using PGLN.Auth.Application.Abstractions.Authentication;

namespace PGLN.Auth.Application.Tests.TestDoubles;

internal sealed class FakeRefreshTokenGenerator
    : IRefreshTokenGenerator
{
    public string Token { get; set; } =
        "raw-refresh-token";

    public int GenerateCallCount { get; private set; }

    public string Generate()
    {
        GenerateCallCount++;

        return Token;
    }
}
