using BackEnd.IServices;

namespace BackEnd.Services
{
    public class AuditLogCleanupService : BackgroundService
    {
        private const int DefaultRetentionDays = 180;
        private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuditLogCleanupService> _logger;

        public AuditLogCleanupService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<AuditLogCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(Interval);

            do
            {
                try
                {
                    var days = _configuration.GetValue("Audit:RetentionDays", DefaultRetentionDays);

                    if (days > 0)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var service = scope.ServiceProvider.GetRequiredService<IAuditLogService>();
                        var removed = await service.DeleteOlderThanAsync(DateTime.UtcNow.AddDays(-days));

                        if (removed > 0)
                        {
                            _logger.LogInformation("Audit log cleanup removed {Count} entries", removed);
                        }
                    }
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    _logger.LogError(exception, "Audit log cleanup failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}
