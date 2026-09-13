using PGLN.Auth.Application.Abstractions.Authentication;

namespace PGLN.Auth.IntegrationTests.Http;

internal sealed class HttpTestStepUpCodeGenerator
    : IStepUpCodeGenerator
{
    public string Generate()
    {
        return "123456";
    }
}
