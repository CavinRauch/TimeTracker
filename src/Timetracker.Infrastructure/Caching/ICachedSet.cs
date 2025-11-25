namespace Timetracker.Infrastructure.Caching;

public interface ICachedSet<T> where T : class
{
    /// <summary>
    /// Read-only snapshot of the cached entities. Fast, thread-safe, and immutable to callers.
    /// </summary>
    IReadOnlyList<T> Entities { get; }

    /// <summary>
    /// Reload the snapshot from the DB (AsNoTracking).
    /// </summary>
    Task RefreshAsync();

    /// <summary>
    /// Persist and add the entity; updates cache and raises ItemAdded.
    /// </summary>
    Task AddAsync(T entity);

    /// <summary>
    /// Persist and update the entity; updates cache and raises ItemUpdated.
    /// </summary>
    Task UpdateAsync(T entity);

    /// <summary>
    /// Persist deletion; updates cache and raises ItemRemoved.
    /// The key object should match the primary key type for the entity (Guid, int, etc.).
    /// </summary>
    Task DeleteAsync(object key);

    event EventHandler<T>? ItemAdded;
    event EventHandler<T>? ItemUpdated;
    event EventHandler<object>? ItemRemoved;
}