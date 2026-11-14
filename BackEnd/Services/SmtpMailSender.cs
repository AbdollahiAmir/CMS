using System.Net;
using System.Net.Mail;
using System.Text;
using BackEnd.IServices;
using BackEnd.Settings;
using Microsoft.Extensions.Options;

namespace BackEnd.Services
{
    public class SmtpMailSender : IMailSender
    {
        private readonly EmailOptions _options;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<SmtpMailSender> _logger;

        public SmtpMailSender(
            IOptions<EmailOptions> options,
            IHostEnvironment environment,
            ILogger<SmtpMailSender> logger)
        {
            _options = options.Value;
            _environment = environment;
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.FromAddress))
            {
                if (_environment.IsDevelopment())
                {
                    _logger.LogInformation("Email to {To} | {Subject}\n{Body}", to, subject, body);
                }
                else
                {
                    _logger.LogWarning("Email is not configured; message to {To} was not sent", to);
                }

                return;
            }

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl
            };

            if (!string.IsNullOrWhiteSpace(_options.UserName))
            {
                client.Credentials = new NetworkCredential(_options.UserName, _options.Password);
            }

            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName, Encoding.UTF8),
                Subject = subject,
                SubjectEncoding = Encoding.UTF8,
                Body = body,
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = false
            };

            message.To.Add(new MailAddress(to));

            await client.SendMailAsync(message, cancellationToken);
        }
    }
}
