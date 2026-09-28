using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using PGLN.Auth.Application.Abstractions.Email;
using PGLN.Auth.Aws.Email;

namespace PGLN.Auth.Aws.Tests.Email;

public sealed class SesEmailSenderTests
{
    [Fact]
    public async Task SendAsync_WithHtmlAndText_ShouldMapEmailMessageToSesRequest()
    {
        // Arrange
        using var ses =
            new CapturingSesClient();

        var sender =
            new SesEmailSender(
                ses,
                "auth@example.com");

        var message =
            new EmailMessage(
                To:
                    "user@example.com",
                Subject:
                    "Confirm your email",
                HtmlBody:
                    "<p>Confirm your email.</p>",
                TextBody:
                    "Confirm your email.");

        // Act
        await sender.SendAsync(
            message);

        // Assert
        var request =
            Assert.IsType<SendEmailRequest>(
                ses.CapturedRequest);

        Assert.Equal(
            "auth@example.com",
            request.FromEmailAddress);

        Assert.Single(
            request.Destination.ToAddresses);

        Assert.Equal(
            "user@example.com",
            request.Destination.ToAddresses[0]);

        Assert.Equal(
            "Confirm your email",
            request.Content.Simple.Subject.Data);

        Assert.Equal(
            "UTF-8",
            request.Content.Simple.Subject.Charset);

        Assert.Equal(
            "<p>Confirm your email.</p>",
            request.Content.Simple.Body.Html.Data);

        Assert.Equal(
            "UTF-8",
            request.Content.Simple.Body.Html.Charset);

        Assert.NotNull(
            request.Content.Simple.Body.Text);

        Assert.Equal(
            "Confirm your email.",
            request.Content.Simple.Body.Text.Data);

        Assert.Equal(
            "UTF-8",
            request.Content.Simple.Body.Text.Charset);
    }

    [Fact]
    public async Task SendAsync_WithoutTextBody_ShouldOmitSesTextBody()
    {
        // Arrange
        using var ses =
            new CapturingSesClient();

        var sender =
            new SesEmailSender(
                ses,
                "auth@example.com");

        var message =
            new EmailMessage(
                To:
                    "user@example.com",
                Subject:
                    "Welcome",
                HtmlBody:
                    "<p>Welcome.</p>");

        // Act
        await sender.SendAsync(
            message);

        // Assert
        var request =
            Assert.IsType<SendEmailRequest>(
                ses.CapturedRequest);

        Assert.Equal(
            "<p>Welcome.</p>",
            request.Content.Simple.Body.Html.Data);

        Assert.Null(
            request.Content.Simple.Body.Text);
    }

    [Fact]
    public async Task SendAsync_ShouldForwardCancellationToken()
    {
        // Arrange
        using var ses =
            new CapturingSesClient();

        var sender =
            new SesEmailSender(
                ses,
                "auth@example.com");

        var message =
            new EmailMessage(
                To:
                    "user@example.com",
                Subject:
                    "Test",
                HtmlBody:
                    "<p>Test</p>");

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        // Act
        await sender.SendAsync(
            message,
            cancellationToken);

        // Assert
        Assert.Equal(
            cancellationToken,
            ses.CapturedCancellationToken);
    }

    private sealed class CapturingSesClient
        : AmazonSimpleEmailServiceV2Client
    {
        public SendEmailRequest? CapturedRequest
        {
            get;
            private set;
        }

        public CancellationToken CapturedCancellationToken
        {
            get;
            private set;
        }

        public override Task<SendEmailResponse> SendEmailAsync(
            SendEmailRequest request,
            CancellationToken cancellationToken = default)
        {
            CapturedRequest =
                request;

            CapturedCancellationToken =
                cancellationToken;

            return Task.FromResult(
                new SendEmailResponse
                {
                    MessageId =
                        "synthetic-ses-message-id"
                });
        }
    }
}
