using Intranet.Data;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Diagnostics;

namespace Intranet.Services
{
    public class EmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendGlobalNotificationAsync(string subject, string messageBody, List<string> recipients)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));

            foreach (var address in recipients)
            {
                if (!string.IsNullOrWhiteSpace(address))
                {
                    email.Bcc.Add(MailboxAddress.Parse(address));
                }
            }

            email.Subject = subject;
            var builder = new BodyBuilder { HtmlBody = messageBody };
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            smtp.Timeout = 15000;

            try
            {
                smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

                await smtp.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.Auto);

                await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
                await smtp.SendAsync(email);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR EMAIL]: No se pudo enviar por puerto {_settings.Port}. Detalles: {ex.Message}");
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }
    }
}