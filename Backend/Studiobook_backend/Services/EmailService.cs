using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Studiobook_backend.Services.Interfaces;
using System.Net;
using System.Net.Mail;

namespace Studiobook_backend.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendSignupVerificationEmailAsync(string toEmail, string verifyUrl)
        {
            var host = _configuration["Mailtrap:Host"];
            var portText = _configuration["Mailtrap:Port"];
            var userName = _configuration["Mailtrap:UserName"];
            var password = _configuration["Mailtrap:Password"];
            var from = _configuration["Mailtrap:From"];

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(portText) ||
                string.IsNullOrWhiteSpace(userName) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(from))
            {
                _logger.LogError("Mailtrap settings are missing.");
                throw new InvalidOperationException("Mailtrap settings are missing.");
            }

            if (!int.TryParse(portText, out var port))
            {
                _logger.LogError("Mailtrap:Port is invalid. Value: {Port}", portText);
                throw new InvalidOperationException("Mailtrap:Port is invalid.");
            }

            var subject = "【Studio Book】メールアドレス認証のお願い";
            var body = $"""
                Studio Book の会員登録ありがとうございます。

                以下のURLをクリックしてメール認証を完了してください。
                {verifyUrl}

                ※このURLの有効期限は24時間です。
                ※このメールに心当たりがない場合は、破棄してください。
                """;

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(userName, password),
                EnableSsl = true
            };

            using var message = new MailMessage(from, toEmail, subject, body)
            {
                IsBodyHtml = false
            };

            await client.SendMailAsync(message);

            _logger.LogInformation("Verification email sent to {Email}", toEmail);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetUrl)
        {
            var host = _configuration["Mailtrap:Host"];
            var portText = _configuration["Mailtrap:Port"];
            var userName = _configuration["Mailtrap:UserName"];
            var password = _configuration["Mailtrap:Password"];
            var from = _configuration["Mailtrap:From"];

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(portText) ||
                string.IsNullOrWhiteSpace(userName) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(from))
            {
                _logger.LogError("Mailtrap settings are missing.");
                throw new InvalidOperationException("Mailtrap settings are missing.");
            }

            if (!int.TryParse(portText, out var port))
            {
                _logger.LogError("Mailtrap:Port is invalid. Value: {Port}", portText);
                throw new InvalidOperationException("Mailtrap:Port is invalid.");
            }

            var subject = "【Studio Book】パスワード再設定のご案内";
            var body = $"""
                パスワード再設定のご依頼を受け付けました。

                以下のURLをクリックして新しいパスワードを設定してください。
                {resetUrl}

                ※このURLの有効期限は1時間です。
                ※このメールに心当たりがない場合は、破棄してください。
                """;

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(userName, password),
                EnableSsl = true
            };

            using var message = new MailMessage(from, toEmail, subject, body)
            {
                IsBodyHtml = false
            };

            await client.SendMailAsync(message);

            _logger.LogInformation("Password reset email sent to {Email}", toEmail);
        }
    }
}