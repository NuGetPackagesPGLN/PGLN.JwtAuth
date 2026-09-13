using PGLN.Auth.Application.Features.StepUp.VerifyStepUpChallenge;

namespace PGLN.Auth.Application.Tests.Features.StepUp.VerifyStepUpChallenge;

public sealed class VerifyStepUpChallengeCommandValidatorTests
{
    private readonly VerifyStepUpChallengeCommandValidator _validator =
        new();

    [Fact]
    public async Task ValidateAsync_WithValidCommand_ShouldSucceed()
    {
        var command =
            new VerifyStepUpChallengeCommand(
                Guid.NewGuid(),
                "123456",
                true);

        var result =
            await _validator.ValidateAsync(
                command);

        Assert.True(
            result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyChallengeId_ShouldFail()
    {
        var command =
            new VerifyStepUpChallengeCommand(
                Guid.Empty,
                "123456",
                false);

        var result =
            await _validator.ValidateAsync(
                command);

        Assert.False(
            result.IsValid);

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName ==
                nameof(
                    VerifyStepUpChallengeCommand.ChallengeId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("abcdef")]
    [InlineData("12a456")]
    [InlineData("123 456")]
    public async Task ValidateAsync_WithInvalidCode_ShouldFail(
        string code)
    {
        var command =
            new VerifyStepUpChallengeCommand(
                Guid.NewGuid(),
                code,
                false);

        var result =
            await _validator.ValidateAsync(
                command);

        Assert.False(
            result.IsValid);

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName ==
                nameof(
                    VerifyStepUpChallengeCommand.Code));
    }
}
