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
        private readonly EmailOptions _fallback;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<SmtpMailSender> _logger;

        public SmtpMailSender(
            IOptions<EmailOptions> fallback,
            IServiceScopeFactory scopeFactory,
            IHostEnvironment environment,
            ILogger<SmtpMailSender> logger)
        {
            _fallback = fallback.Value;
            _scopeFactory = scopeFactory;
            _environment = environment;
            _logger = logger;
        }

        private async Task<EmailIntegration?> ResolveAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var settings = scope.ServiceProvider.GetRequiredService<IIntegrationSettingsService>();
            var stored = await settings.GetAsync<EmailIntegration>(IntegrationKeys.Email);

            if (stored.Enabled
                && !string.IsNullOrWhiteSpace(stored.Host)
                && !string.IsNullOrWhiteSpace(stored.FromAddress))
            {
                return stored;
            }

            if (!string.IsNullOrWhiteSpace(_fallback.Host) && !string.IsNullOrWhiteSpace(_fallback.FromAddress))
            {
                return new EmailIntegration
                {
                    Enabled = true,
                    Host = _fallback.Host,
                    Port = _fallback.Port,
                    EnableSsl = _fallback.EnableSsl,
                    UserName = _fallback.UserName,
                    Password = _fallback.Password,
                    FromAddress = _fallback.FromAddress,
                    FromName = _fallback.FromName
                };
            }

            return null;
        }

        public async Task<bool> IsConfiguredAsync()
        {
            return await ResolveAsync() is not null;
        }

        public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
        {
            var options = await ResolveAsync();

            if (options is null)
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

            using var client = new SmtpClient(options.Host, options.Port)
            {
                EnableSsl = options.EnableSsl
            };

            if (!string.IsNullOrWhiteSpace(options.UserName))
            {
                client.Credentials = new NetworkCredential(options.UserName, options.Password);
            }

            using var message = new MailMessage
            {
                From = new MailAddress(options.FromAddress, options.FromName, Encoding.UTF8),
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
