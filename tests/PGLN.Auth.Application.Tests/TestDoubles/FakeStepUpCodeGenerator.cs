using PGLN.Auth.Application.Abstractions.Authentication;

namespace PGLN.Auth.Application.Tests.TestDoubles;

public sealed class FakeStepUpCodeGenerator
    : IStepUpCodeGenerator
{
    public string Code { get; set; } =
        "123456";

    public int GenerateCallCount { get; private set; }

    public string Generate()
    {
        GenerateCallCount++;

        return Code;
    }
}
