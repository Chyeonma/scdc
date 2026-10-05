using System.Net;
using System.Net.Mail;
using System.Security.Authentication;
using System.Text;
using Microsoft.Extensions.Options;

namespace SCDC.Modules.Identity.Infrastructure.Email;

internal sealed record AccountEmail(Guid DeliveryId, string Recipient, string Subject, string Body);
internal sealed record EmailSendResult(bool Accepted, bool Retryable, string? ErrorCode = null, string? MessageId = null);

internal interface IAccountEmailSender
{
    Task<EmailSendResult> SendAsync(AccountEmail email, CancellationToken cancellationToken);
}

internal sealed class GmailSmtpEmailSender(IOptions<IdentityEmailOptions> options) : IAccountEmailSender
{
    public async Task<EmailSendResult> SendAsync(AccountEmail email, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var messageId = $"<{email.DeliveryId:N}@scdc.identity>";
        using var message = new MailMessage(settings.SenderAddress, email.Recipient)
        {
            Subject = email.Subject,
            Body = email.Body,
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = false
        };
        message.Headers.Add("Message-ID", messageId);
        message.Headers.Add("Auto-Submitted", "auto-generated");
        using var client = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(settings.SenderAddress, settings.AppPassword.Replace(" ", string.Empty)),
            Timeout = settings.SendTimeoutSeconds * 1000
        };

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            // SMTP acceptance is not a delivery receipt from the recipient's mailbox.
            return new EmailSendResult(true, false, MessageId: messageId);
        }
        catch (SmtpException exception)
        {
            var status = (int)exception.StatusCode;
            var authenticationFailure = exception.InnerException is AuthenticationException || status is 534 or 535;
            return new EmailSendResult(false,
                !authenticationFailure && (status < 0 || status is >= 400 and < 500),
                authenticationFailure ? "Email.AuthenticationFailed" : $"Email.Smtp.{status}");
        }
        catch (AuthenticationException)
        {
            return new EmailSendResult(false, false, "Email.AuthenticationFailed");
        }
    }
}
