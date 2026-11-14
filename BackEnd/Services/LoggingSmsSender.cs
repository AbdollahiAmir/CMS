using BackEnd.IServices;

namespace BackEnd.Services
{
    public class LoggingSmsSender : ISmsSender
    {
        private readonly IHostEnvironment _environment;
        private readonly ILogger<LoggingSmsSender> _logger;

        public LoggingSmsSender(IHostEnvironment environment, ILogger<LoggingSmsSender> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
        {
            if (_environment.IsDevelopment())
            {
                _logger.LogInformation("SMS to {Phone}: {Message}", phoneNumber, message);
            }
            else
            {
                _logger.LogWarning("No SMS provider is configured; message to {Phone} was not sent", phoneNumber);
            }

            return Task.CompletedTask;
        }
    }
}
