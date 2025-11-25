using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Timetracker.Infrastructure.Context;

namespace Timetracker.Infrastructure.Caching;

public class CachedDbSets : ICachedDbSets, IDisposable
{
    private readonly IDbContextFactory<TimetrackerDbContext> _dbFactory;
    private readonly ConcurrentDictionary<Type, object> _sets = new();
    private bool _disposed;

    public CachedDbSets(IDbContextFactory<TimetrackerDbContext> dbFactory)
    {
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
    }

    public ICachedSet<T> For<T>() where T : class
    {
        var set = (ICachedSet<T>)_sets.GetOrAdd(typeof(T), _ => new CachedSet<T>(_dbFactory));
        return set;
    }

    public async Task ExecuteInTransactionAsync(Func<TimetrackerDbContext, Task> operation)
    {
        if (operation == null) throw new ArgumentNullException(nameof(operation));

        await using var db = _dbFactory.CreateDbContext();
        await using var tx = await db.Database.BeginTransactionAsync().ConfigureAwait(false);

        try
        {
            await operation(db).ConfigureAwait(false);
            await db.SaveChangesAsync().ConfigureAwait(false);
            await tx.CommitAsync().ConfigureAwait(false);
        }
        catch
        {
            try
            {
                await tx.RollbackAsync().ConfigureAwait(false);
            }
            catch
            {
            }

            throw;
        }

        // After a transaction commit, refresh all caches so snapshots reflect DB state.
        // This is simple and correct. If you need optimization, refresh specific sets instead.
        await RefreshAllAsync().ConfigureAwait(false);
    }

    public async Task ExecuteInTransactionAsync<T>(Func<TimetrackerDbContext, DbSet<T>, Task> operation) where T : class
    {
        if (operation == null) throw new ArgumentNullException(nameof(operation));

        await using var db = _dbFactory.CreateDbContext();
        await using var tx = await db.Database.BeginTransactionAsync().ConfigureAwait(false);

        try
        {
            // give caller the typed DbSet<T> from this DbContext
            await operation(db, db.Set<T>()).ConfigureAwait(false);

            await db.SaveChangesAsync().ConfigureAwait(false);
            await tx.CommitAsync().ConfigureAwait(false);
        }
        catch
        {
            try
            {
                await tx.RollbackAsync().ConfigureAwait(false);
            }
            catch
            {
            }

            throw;
        }

        await RefreshAllAsync().ConfigureAwait(false);
    }

    public async Task RefreshAllAsync()
    {
        var tasks = _sets.Values
            .Cast<dynamic>() // dynamic to call RefreshAsync on ICachedSet<T>
            .Select(s => (Task)s.RefreshAsync());

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_disposed) return;
        foreach (var kv in _sets.Values)
        {
            (kv as IDisposable)?.Dispose();
        }

        _disposed = true;
    }
}