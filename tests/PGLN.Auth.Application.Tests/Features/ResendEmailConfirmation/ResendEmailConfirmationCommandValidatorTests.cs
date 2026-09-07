using PGLN.Auth.Application.Features.ResendEmailConfirmation;

namespace PGLN.Auth.Application.Tests.Features.ResendEmailConfirmation;

public sealed class ResendEmailConfirmationCommandValidatorTests
{
    private readonly ResendEmailConfirmationCommandValidator _validator =
        new();

    [Fact]
    public void Validate_WithValidEmail_ShouldSucceed()
    {
        var command =
            new ResendEmailConfirmationCommand(
                "user@example.com");

        var result =
            _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyEmail_ShouldFail()
    {
        var command =
            new ResendEmailConfirmationCommand(
                string.Empty);

        var result =
            _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WithInvalidEmail_ShouldFail()
    {
        var command =
            new ResendEmailConfirmationCommand(
                "not-an-email");

        var result =
            _validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
