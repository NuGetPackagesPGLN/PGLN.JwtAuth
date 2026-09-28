using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using PGLN.Auth.Application.Abstractions.Email;

namespace PGLN.Auth.Aws.Email;

public sealed class SesEmailSender : IEmailSender
{
    private readonly IAmazonSimpleEmailServiceV2 _ses;
    private readonly string _fromAddress;

    public SesEmailSender(
        IAmazonSimpleEmailServiceV2 ses,
        string fromAddress)
    {
        ArgumentNullException.ThrowIfNull(ses);

        if (string.IsNullOrWhiteSpace(fromAddress))
        {
            throw new ArgumentException(
                "SES from address is required.",
                nameof(fromAddress));
        }

        _ses = ses;
        _fromAddress = fromAddress;
    }

    public async Task SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var body = new Body
        {
            Html = new Content
            {
                Data = message.HtmlBody,
                Charset = "UTF-8"
            }
        };

        if (!string.IsNullOrWhiteSpace(message.TextBody))
        {
            body.Text = new Content
            {
                Data = message.TextBody,
                Charset = "UTF-8"
            };
        }

        var request = new SendEmailRequest
        {
            FromEmailAddress = _fromAddress,

            Destination = new Destination
            {
                ToAddresses = new List<string>
                {
                    message.To
                }
            },

            Content = new EmailContent
            {
                Simple = new Message
                {
                    Subject = new Content
                    {
                        Data = message.Subject,
                        Charset = "UTF-8"
                    },

                    Body = body
                }
            }
        };

        await _ses.SendEmailAsync(
            request,
            cancellationToken);
    }
}
