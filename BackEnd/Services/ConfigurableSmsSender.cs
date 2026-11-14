using System.Text;
using System.Text.Json;
using BackEnd.IServices;
using BackEnd.Settings;

namespace BackEnd.Services
{
    public class ConfigurableSmsSender : ISmsSender
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<ConfigurableSmsSender> _logger;

        public ConfigurableSmsSender(
            IServiceScopeFactory scopeFactory,
            IHttpClientFactory httpClientFactory,
            IHostEnvironment environment,
            ILogger<ConfigurableSmsSender> logger)
        {
            _scopeFactory = scopeFactory;
            _httpClientFactory = httpClientFactory;
            _environment = environment;
            _logger = logger;
        }

        private async Task<SmsIntegration?> ResolveAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var settings = scope.ServiceProvider.GetRequiredService<IIntegrationSettingsService>();
            var stored = await settings.GetAsync<SmsIntegration>(IntegrationKeys.Sms);

            return stored.Enabled && !string.IsNullOrWhiteSpace(stored.ApiKey) ? stored : null;
        }

        public async Task<bool> IsConfiguredAsync()
        {
            return await ResolveAsync() is not null;
        }

        public async Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
        {
            var options = await ResolveAsync();

            if (options is null)
            {
                if (_environment.IsDevelopment())
                {
                    _logger.LogInformation("SMS to {Phone}: {Message}", phoneNumber, message);
                }
                else
                {
                    _logger.LogWarning("No SMS provider is configured; message to {Phone} was not sent", phoneNumber);
                }

                return;
            }

            switch (options.Provider)
            {
                case SmsProviders.Kavenegar:
                    await SendKavenegarAsync(options, phoneNumber, message, cancellationToken);
                    break;
                case SmsProviders.SmsIr:
                    await SendSmsIrAsync(options, phoneNumber, message, cancellationToken);
                    break;
                default:
                    throw new InvalidOperationException("ارائه‌دهنده‌ی پیامک نامعتبر است");
            }
        }

        private async Task SendKavenegarAsync(SmsIntegration options, string phone, string message, CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient("sms");
            var url = $"https://api.kavenegar.com/v1/{Uri.EscapeDataString(options.ApiKey)}/sms/send.json";

            var fields = new Dictionary<string, string>
            {
                ["receptor"] = phone,
                ["message"] = message
            };

            if (!string.IsNullOrWhiteSpace(options.Sender))
            {
                fields["sender"] = options.Sender;
            }

            using var response = await client.PostAsync(url, new FormUrlEncodedContent(fields), cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            var status = ReadStatus(body, "return", "status");

            if (!response.IsSuccessStatusCode || status != 200)
            {
                _logger.LogError("Kavenegar rejected the message: HTTP {Http}, status {Status}", (int)response.StatusCode, status);

                throw new InvalidOperationException("ارسال پیامک ناموفق بود");
            }
        }

        private async Task SendSmsIrAsync(SmsIntegration options, string phone, string message, CancellationToken cancellationToken)
        {
            if (!long.TryParse(options.Sender, out var lineNumber))
            {
                throw new InvalidOperationException("شماره‌ی خط ارسال‌کننده نامعتبر است");
            }

            var client = _httpClientFactory.CreateClient("sms");

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sms.ir/v1/send/bulk");
            request.Headers.Add("X-API-KEY", options.ApiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    lineNumber,
                    messageText = message,
                    mobiles = new[] { phone }
                }),
                Encoding.UTF8,
                "application/json");

            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            var status = ReadStatus(body, "status");

            if (!response.IsSuccessStatusCode || status != 1)
            {
                _logger.LogError("SMS.ir rejected the message: HTTP {Http}, status {Status}", (int)response.StatusCode, status);

                throw new InvalidOperationException("ارسال پیامک ناموفق بود");
            }
        }

        private static int? ReadStatus(string json, params string[] path)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                var element = document.RootElement;

                foreach (var segment in path)
                {
                    if (!element.TryGetProperty(segment, out element))
                    {
                        return null;
                    }
                }

                return element.ValueKind == JsonValueKind.Number ? element.GetInt32() : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
