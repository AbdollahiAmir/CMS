using Microsoft.EntityFrameworkCore;

namespace BackEnd.Helpers
{
    public static class DbContextExtensions
    {
        public static async Task ExecuteInTransactionAsync(this DbContext context, Func<Task> action)
        {
            var strategy = context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync();

                await action();

                await transaction.CommitAsync();
            });
        }
    }
}