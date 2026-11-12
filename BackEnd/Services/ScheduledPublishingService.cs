using BackEnd.Data;
using BackEnd.Filters;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Services
{
    public class ScheduledPublishingService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ScheduledPublishingService> _logger;

        public ScheduledPublishingService(IServiceScopeFactory scopeFactory, ILogger<ScheduledPublishingService> logger)
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
                    await PublishDueAsync(stoppingToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    _logger.LogError(exception, "Scheduled publishing failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        private async Task PublishDueAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var cache = scope.ServiceProvider.GetRequiredService<IOutputCacheStore>();

            var now = DateTime.UtcNow;

            var posts = await context.Posts
                .Where(p => !p.IsPublished && p.ScheduledAt != null && p.ScheduledAt <= now && p.Content != "")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.IsPublished, true)
                    .SetProperty(p => p.PublishedAt, p => p.ScheduledAt)
                    .SetProperty(p => p.ScheduledAt, (DateTime?)null)
                    .SetProperty(p => p.UpdatedAt, (DateTime?)now),
                    cancellationToken);

            var pages = await context.ContentPages
                .Where(p => !p.IsPublished && p.ScheduledAt != null && p.ScheduledAt <= now && p.Content != "")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.IsPublished, true)
                    .SetProperty(p => p.PublishedAt, p => p.ScheduledAt)
                    .SetProperty(p => p.ScheduledAt, (DateTime?)null)
                    .SetProperty(p => p.UpdatedAt, (DateTime?)now),
                    cancellationToken);

            if (posts + pages > 0)
            {
                await cache.EvictByTagAsync(PublicCacheEvictionFilter.Tag, cancellationToken);

                _logger.LogInformation("Scheduled publishing published {Posts} posts and {Pages} pages", posts, pages);
            }
        }
    }
}
