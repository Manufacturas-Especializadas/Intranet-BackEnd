using Intranet.Data;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Intranet.Services
{
    public class EmailService
    {
        private readonly EmailSettings _setting;

        public EmailService(IOptions<EmailSettings> options)
        {
            _setting = options.Value;
        }

        public async Task SendEmailForAPersonAsync(string toEmail, string subject, string body)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_setting.SenderEmail, _setting.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
                Priority = MailPriority.High,
            };

            mailMessage.To.Add(toEmail);

            using var client = new SmtpClient
            {
                Host = _setting.Host,
                Port = _setting.Port,
                EnableSsl = _setting.UseSSL,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_setting.Username, _setting.Password)
            };

            await client.SendMailAsync(mailMessage);
        }

        public async Task SendEmailAsync(List<string> recipients, string subject, string body)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_setting.SenderEmail, _setting.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
                Priority = MailPriority.High,
            };

            mailMessage.To.Add(_setting.SenderEmail);

            foreach (var email in recipients)
            {
                if (!string.IsNullOrWhiteSpace(email))
                {
                    mailMessage.Bcc.Add(email);
                }
            }

            using var client = new SmtpClient
            {
                Host = _setting.Host,
                Port = _setting.Port,
                EnableSsl = _setting.UseSSL,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_setting.Username, _setting.Password)
            };

            await client.SendMailAsync(mailMessage);
        }
    }
}