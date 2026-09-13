using PGLN.Auth.Infrastructure.Authentication;

namespace PGLN.Auth.Infrastructure.Tests.Authentication;

public sealed class SecureStepUpCodeGeneratorTests
{
    [Fact]
    public void Generate_ShouldReturnSixDigits()
    {
        var generator =
            new SecureStepUpCodeGenerator();

        var code =
            generator.Generate();

        Assert.Equal(
            6,
            code.Length);

        Assert.All(
            code,
            character =>
                Assert.True(
                    char.IsDigit(
                        character)));
    }

    [Fact]
    public void Generate_ShouldPreserveLeadingZeros()
    {
        var generator =
            new SecureStepUpCodeGenerator();

        for (var i = 0; i < 100; i++)
        {
            var code =
                generator.Generate();

            Assert.Equal(
                6,
                code.Length);
        }
    }
}
