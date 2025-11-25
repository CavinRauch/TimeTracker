using Microsoft.EntityFrameworkCore;
using Timetracker.Infrastructure.Context;

namespace Timetracker.Infrastructure.Caching;

public class CachedSet<T> : ICachedSet<T>, IDisposable where T : class
{
    private readonly IDbContextFactory<TimetrackerDbContext> _dbFactory;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private bool _disposed;
    private volatile IReadOnlyList<T> _snapshot = Array.Empty<T>();

    public CachedSet(IDbContextFactory<TimetrackerDbContext> dbFactory)
    {
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
    }

    public IReadOnlyList<T> Entities
    {
        get
        {
            if (_snapshot.Count == 0)
            {
                _ = RefreshAsync();
            }

            return _snapshot;
        }
    }

    public event EventHandler<T>? ItemAdded;
    public event EventHandler<T>? ItemUpdated;
    public event EventHandler<object>? ItemRemoved;

    public async Task RefreshAsync()
    {
        await using var db = _dbFactory.CreateDbContext();
        var items = await db.Set<T>().AsNoTracking().ToListAsync().ConfigureAwait(false);
        // Ensure deterministic ordering for snapshot consumers if entity has Start or similar.
        // Leave ordering to caller (e.g., .OrderByDescending) if needed.
        _snapshot = items;
    }

    public async Task AddAsync(T entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        await _writeLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await using var db = _dbFactory.CreateDbContext();
            db.Set<T>().Add(entity);
            await db.SaveChangesAsync().ConfigureAwait(false);

            // Update snapshot
            var list = _snapshot.ToList();
            list.Add(entity);
            _snapshot = list;
        }
        finally
        {
            _writeLock.Release();
        }

        ItemAdded?.Invoke(this, entity);
    }

    public async Task UpdateAsync(T entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        await _writeLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await using var db = _dbFactory.CreateDbContext();
            db.Set<T>().Update(entity);
            await db.SaveChangesAsync().ConfigureAwait(false);

            var list = _snapshot.ToList();
            var idx = list.FindIndex(x => ReferenceEquals(x, entity) || KeysEqual(x, entity));
            if (idx >= 0)
            {
                list[idx] = entity;
            }
            else
            {
                list.Add(entity);
            }

            _snapshot = list;
        }
        finally
        {
            _writeLock.Release();
        }

        ItemUpdated?.Invoke(this, entity);
    }

    public async Task DeleteAsync(object key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        await _writeLock.WaitAsync().ConfigureAwait(false);
        T? removedEntity = default;
        try
        {
            await using var db = _dbFactory.CreateDbContext();

            // Attempt to find by primary key - assumes single key.
            var entity = await db.Set<T>().FindAsync(new object[] { key }).ConfigureAwait(false);
            if (entity != null)
            {
                db.Set<T>().Remove(entity);
                await db.SaveChangesAsync().ConfigureAwait(false);
                removedEntity = entity;
            }

            var list = _snapshot.ToList();
            var removed = list.RemoveAll(x => EntityMatchesKey(x, key)) > 0;
            if (removed)
            {
                _snapshot = list;
            }
        }
        finally
        {
            _writeLock.Release();
        }

        if (removedEntity != null)
        {
            ItemRemoved?.Invoke(this, key);
        }
        else
        {
            ItemRemoved?.Invoke(this, key); // still notify that key was removed (idempotent)
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _writeLock?.Dispose();
        _disposed = true;
    }

    // Helper: compare primary key values using EF metadata if possible, otherwise try runtime matching by "Id" or "TimeEntryId".
    private static bool EntityMatchesKey(T entity, object key)
    {
        // Common conventions
        var t = entity.GetType();
        // try property named "Id"
        var idProp = t.GetProperty("Id");
        if (idProp != null)
        {
            var val = idProp.GetValue(entity);
            if (val != null && val.Equals(key))
            {
                return true;
            }
        }

        // try common FK name "TimeEntryId"
        var altProp = t.GetProperty("TimeEntryId");
        if (altProp != null)
        {
            var val = altProp.GetValue(entity);
            if (val != null && val.Equals(key))
            {
                return true;
            }
        }

        // fallback: not matched
        return false;
    }

    // Try to detect equality between two entity instances by primary key
    private static bool KeysEqual(T a, T b)
    {
        if (a == null || b == null)
        {
            return false;
        }

        var t = typeof(T);
        var idProp = t.GetProperty("Id");
        if (idProp != null)
        {
            var va = idProp.GetValue(a);
            var vb = idProp.GetValue(b);
            if (va != null && vb != null)
            {
                return va.Equals(vb);
            }
        }

        var altProp = t.GetProperty("TimeEntryId");
        if (altProp != null)
        {
            var va = altProp.GetValue(a);
            var vb = altProp.GetValue(b);
            if (va != null && vb != null)
            {
                return va.Equals(vb);
            }
        }

        return false;
    }
}