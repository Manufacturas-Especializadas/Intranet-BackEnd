using Intranet.Data;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net.Mail;
using MailKit.Net.Smtp;
using System.Diagnostics;

namespace Intranet.Services
{
    public interface IEmailService
    {
        Task SendGlobalNotificationAsync(string subject, string messageBody, List<string> recipients);
    }

    public class EmailService : IEmailService
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
                email.Bcc.Add(MailboxAddress.Parse(address));
            }

            email.Subject = subject;
            var builder = new BodyBuilder { HtmlBody = messageBody };
            email.Body = builder.ToMessageBody();

            using var smtp = new MailKit.Net.Smtp.SmtpClient();
            smtp.Timeout = 10000;

            try
            {
                smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

                await smtp.ConnectAsync("mail.mexis.com.mx", 587, SecureSocketOptions.StartTls);

                await smtp.AuthenticateAsync(_settings.Username, _settings.Password);

                await smtp.SendAsync(email);

                Debug.WriteLine("[EMAIL]: Enviado exitosamente (Configuración Outlook clonada).");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR SMTP]: {ex.Message}");                
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }
    }
}