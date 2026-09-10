using PGLN.Auth.Application.Features.ForgotPassword;

namespace PGLN.Auth.Application.Tests.Features.ForgotPassword;

public sealed class ForgotPasswordCommandValidatorTests
{
    private readonly ForgotPasswordCommandValidator _validator =
        new();

    [Fact]
    public void Validate_WithValidEmail_ShouldSucceed()
    {
        var result =
            _validator.Validate(
                new ForgotPasswordCommand(
                    "user@example.com"));

        Assert.True(
            result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyEmail_ShouldFail()
    {
        var result =
            _validator.Validate(
                new ForgotPasswordCommand(
                    string.Empty));

        Assert.False(
            result.IsValid);
    }

    [Fact]
    public void Validate_WithMalformedEmail_ShouldFail()
    {
        var result =
            _validator.Validate(
                new ForgotPasswordCommand(
                    "not-an-email"));

        Assert.False(
            result.IsValid);
    }

    [Fact]
    public void Validate_WithEmailLongerThan320Characters_ShouldFail()
    {
        var localPart =
            new string(
                'a',
                310);

        var email =
            $"{localPart}@example.com";

        Assert.True(
            email.Length > 320);

        var result =
            _validator.Validate(
                new ForgotPasswordCommand(
                    email));

        Assert.False(
            result.IsValid);
    }
}
