using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace MYAcademy_IdentityEmailProject.Services
{
   
    public class MailService : IMailService
    {
        private readonly MailSettings _settings;
        private readonly ILogger<MailService> _logger;

        public MailService(IOptions<MailSettings> settings, ILogger<MailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody, string? replyToEmail = null, string? replyToName = null)
        {
            if (string.IsNullOrWhiteSpace(_settings.SenderEmail) || string.IsNullOrWhiteSpace(_settings.Password))
                throw new InvalidOperationException("Mail ayarları eksik. appsettings.Secrets.json dosyasına Gmail adresinizi ve uygulama şifrenizi girin.");

            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            mimeMessage.To.Add(new MailboxAddress(string.IsNullOrWhiteSpace(toName) ? toEmail : toName, toEmail));
            if (!string.IsNullOrWhiteSpace(replyToEmail))
                mimeMessage.ReplyTo.Add(new MailboxAddress(replyToName ?? replyToEmail, replyToEmail));
            mimeMessage.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody,
                TextBody = Helpers.HtmlSanitizer.ToPlainText(htmlBody)
            };
            mimeMessage.Body = bodyBuilder.ToMessageBody();

            using var smtpClient = new SmtpClient();
            await smtpClient.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
            await smtpClient.AuthenticateAsync(_settings.SenderEmail, _settings.Password);
            smtpClient.Send(mimeMessage);
            await smtpClient.DisconnectAsync(true);

            _logger.LogInformation("E-posta gönderildi: {To} - {Subject}", toEmail, subject);
        }
    }
}
