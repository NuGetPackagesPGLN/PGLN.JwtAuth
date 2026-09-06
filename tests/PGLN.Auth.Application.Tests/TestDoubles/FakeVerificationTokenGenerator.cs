using PGLN.Auth.Application.Abstractions.Authentication;

namespace PGLN.Auth.Application.Tests.TestDoubles;

internal sealed class FakeVerificationTokenGenerator
    : IVerificationTokenGenerator
{
    public string Token { get; set; } =
        "raw-verification-token";

    public int GenerateCallCount { get; private set; }

    public string Generate()
    {
        GenerateCallCount++;

        return Token;
    }
}
