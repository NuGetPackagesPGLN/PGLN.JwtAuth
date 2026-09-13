using PGLN.Auth.Application.Configuration;

namespace PGLN.Auth.Application.Tests.Configuration;

public sealed class StepUpChallengeOptionsTests
{
    [Fact]
    public void Validate_WithValidDefaults_ShouldNotThrow()
    {
        var options =
            new StepUpChallengeOptions();

        var exception =
            Record.Exception(
                options.Validate);

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WithZeroLifetime_ShouldThrow()
    {
        var options =
            new StepUpChallengeOptions
            {
                Lifetime = TimeSpan.Zero
            };

        var exception =
            Assert.Throws<InvalidOperationException>(
                options.Validate);

        Assert.Equal(
            "Step-up challenge lifetime must be greater than zero.",
            exception.Message);
    }

    [Fact]
    public void Validate_WithNegativeLifetime_ShouldThrow()
    {
        var options =
            new StepUpChallengeOptions
            {
                Lifetime = TimeSpan.FromMinutes(-1)
            };

        Assert.Throws<InvalidOperationException>(
            options.Validate);
    }

    [Fact]
    public void Validate_WithZeroMaxFailedAttempts_ShouldThrow()
    {
        var options =
            new StepUpChallengeOptions
            {
                MaxFailedAttempts = 0
            };

        var exception =
            Assert.Throws<InvalidOperationException>(
                options.Validate);

        Assert.Equal(
            "Step-up maximum failed attempts must be greater than zero.",
            exception.Message);
    }

    [Fact]
    public void Validate_WithNegativeMaxFailedAttempts_ShouldThrow()
    {
        var options =
            new StepUpChallengeOptions
            {
                MaxFailedAttempts = -1
            };

        Assert.Throws<InvalidOperationException>(
            options.Validate);
    }
}
