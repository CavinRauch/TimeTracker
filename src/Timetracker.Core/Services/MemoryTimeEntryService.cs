namespace Timetracker.Core.Services;

// Minimal, synchronous internal implementation but exposing async methods for future extensibility
public class MemoryTimeEntryService : ITimeEntryService, IDisposable
{
    private readonly IClock _clock;

    private readonly List<TimeEntry> _history = [];

    // Simple in-memory description store (Id -> StopOptions)
    private readonly Dictionary<Guid, StopOptions?> _inMemoryDescriptions = new(); //TODO: Remove
    private readonly SemaphoreSlim _slim = new(1, 1);
    private TimeEntry? _current;
    private int _stopStartInProgress; // 0 == none, 1 == running

    public MemoryTimeEntryService(IClock clock)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public void Dispose()
    {
        _slim?.Dispose();
    }

    public TimeEntry? Current => _current;

    public event EventHandler<TimeEntry>? EntryStopped;

    public Task StartAsync()
    {
        if (Current != null)
            return Task.CompletedTask; // idempotent: already running

        _current = new TimeEntry(Guid.NewGuid(), _clock.Now, null);
        return Task.CompletedTask;
    }

    public Task ToggleAsync()
    {
        return Current == null ? StartAsync() : StopAsync();
    }

    public async Task StopAsync(StopOptions? options = null)
    {
        await _slim.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_current == null) return;

            var now = _clock.Now;
            var finished = _current with { End = now };

            // persist to history with optional description metadata stored in a simple wrapper
            _history.Add(finished);

            // Optionally attach description/metadata to a parallel dictionary or a richer domain model.
            // For simplicity in memory, we could store a map from Id -> StopOptions if needed.
            if (options != null)
            {
                // In-memory store for descriptions (simple)
                _inMemoryDescriptions[finished.Id] = options;
            }

            _current = null;

            EntryStopped?.Invoke(this, finished);
        }
        finally
        {
            _slim.Release();
        }
    }

    public async Task StopStartAsync(StopOptions? options = null)
    {
        // fast guard: only one caller performs the stop+start
        if (Interlocked.Exchange(ref _stopStartInProgress, 1) == 1)
            return;

        await _slim.WaitAsync().ConfigureAwait(false);
        try
        {
            var now = _clock.Now;

            if (_current != null)
            {
                var finished = _current with { End = now };
                _history.Add(finished);

                if (options != null)
                    _inMemoryDescriptions[finished.Id] = options;

                _current = null;
                EntryStopped?.Invoke(this, finished);
            }

            // Start new entry
            _current = new TimeEntry(Guid.NewGuid(), now, null);
        }
        finally
        {
            _slim.Release();
            Interlocked.Exchange(ref _stopStartInProgress, 0);
        }
    }

    public Task<TimeEntry[]> GetRecentAsync(int max = 50)
    {
        var items = _history
            .OrderByDescending(e => e.Start)
            .Take(max)
            .ToList();

        return Task.FromResult(items.ToArray());
    }
}