using BackEnd.IServices;

namespace BackEnd.Services
{
    public class RefreshTokenCleanupService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RefreshTokenCleanupService> _logger;

        public RefreshTokenCleanupService(IServiceScopeFactory scopeFactory, ILogger<RefreshTokenCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(Interval);

            do
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var tokens = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>();

                    var removed = await tokens.CleanupAsync();

                    _logger.LogInformation("Refresh token cleanup removed {Count} rows", removed);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    _logger.LogError(exception, "Refresh token cleanup failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}