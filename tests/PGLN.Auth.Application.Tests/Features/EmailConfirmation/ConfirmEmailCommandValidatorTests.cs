using PGLN.Auth.Application.Features.EmailConfirmation;

namespace PGLN.Auth.Application.Tests.Features.EmailConfirmation;

public sealed class ConfirmEmailCommandValidatorTests
{
    private readonly ConfirmEmailCommandValidator _validator =
        new();

    [Fact]
    public async Task ValidateAsync_WithToken_ShouldSucceed()
    {
        var result =
            await _validator.ValidateAsync(
                new ConfirmEmailCommand(
                    "valid-token"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyToken_ShouldFail()
    {
        var result =
            await _validator.ValidateAsync(
                new ConfirmEmailCommand(
                    string.Empty));

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName ==
                nameof(ConfirmEmailCommand.Token));
    }
}
