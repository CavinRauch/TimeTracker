using Microsoft.EntityFrameworkCore;
using Timetracker.Infrastructure.Context;

namespace Timetracker.Infrastructure.Caching;

public interface ICachedDbSets
{
    ICachedSet<T> For<T>() where T : class;

    /// <summary>
    /// Execute multiple operations inside a single DbContext transaction.
    /// After commit the caches will be refreshed.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<TimetrackerDbContext, Task> operation);

    Task ExecuteInTransactionAsync<T>(Func<TimetrackerDbContext, DbSet<T>, Task> operation) where T : class;

    /// <summary>
    /// Refresh all cached sets from the database.
    /// </summary>
    Task RefreshAllAsync();
}