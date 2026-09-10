using PGLN.Auth.Infrastructure.Email;

namespace PGLN.Auth.Infrastructure.Tests.EmailTemplates;

public sealed class DefaultEmailTemplateRendererTests
{
    private readonly DefaultEmailTemplateRenderer _sut =
        new();

    [Fact]
    public void RenderEmailConfirmation_ReturnsExpectedMessage()
    {
        // Arrange
        const string email =
            "user@example.com";

        const string verificationUrl =
            "https://example.com/confirm?token=test-token";

        // Act
        var message =
            _sut.RenderEmailConfirmation(
                email,
                verificationUrl);

        // Assert
        Assert.Equal(
            email,
            message.To);

        Assert.Equal(
            "Confirm your email address",
            message.Subject);

        Assert.Contains(
            verificationUrl,
            message.HtmlBody);

        Assert.NotNull(
            message.TextBody);

        Assert.Contains(
            verificationUrl,
            message.TextBody!);
    }

    [Fact]
    public void RenderWelcomeEmail_ReturnsExpectedMessage()
    {
        // Arrange
        const string email =
            "user@example.com";

        // Act
        var message =
            _sut.RenderWelcomeEmail(
                email);

        // Assert
        Assert.Equal(
            email,
            message.To);

        Assert.Equal(
            "Welcome to PGLN Auth",
            message.Subject);

        Assert.Contains(
            "confirmed",
            message.HtmlBody,
            StringComparison.OrdinalIgnoreCase);

        Assert.NotNull(
            message.TextBody);

        Assert.Contains(
            "confirmed",
            message.TextBody!,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RenderPasswordReset_ReturnsExpectedMessage()
    {
        // Arrange
        const string email =
            "user@example.com";

        const string resetUrl =
            "https://example.com/reset-password?token=test-token";

        // Act
        var message =
            _sut.RenderPasswordReset(
                email,
                resetUrl);

        // Assert
        Assert.Equal(
            email,
            message.To);

        Assert.Equal(
            "Reset your password",
            message.Subject);

        Assert.Contains(
            resetUrl,
            message.HtmlBody);

        Assert.NotNull(
            message.TextBody);

        Assert.Contains(
            resetUrl,
            message.TextBody!);
    }

    [Fact]
    public void RenderPasswordReset_WithBlankEmail_Throws()
    {
        // Act
        var action =
            () => _sut.RenderPasswordReset(
                "",
                "https://example.com/reset-password?token=test");

        // Assert
        Assert.Throws<ArgumentException>(
            action);
    }

    [Fact]
    public void RenderPasswordReset_WithBlankResetUrl_Throws()
    {
        // Act
        var action =
            () => _sut.RenderPasswordReset(
                "user@example.com",
                "");

        // Assert
        Assert.Throws<ArgumentException>(
            action);
    }
}

