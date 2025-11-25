namespace Timetracker.Core
{
    public record TimeEntry(Guid Id, DateTimeOffset Start, DateTimeOffset? End)
    {
        public TimeSpan GetElapsed(IClock clock) => (End ?? clock.Now) - Start;
    }

    public enum SessionState
    {
        Idle,
        Running,
        Paused
    }

    public interface ITimeEntryService
    {
        /// <summary>Gets the current running TimeEntry or null if none is running.</summary>
        TimeEntry? Current { get; }

        /// <summary>Raised when a TimeEntry is stopped. Provides the entry.</summary>
        event EventHandler<TimeEntry>? EntryStopped;

        /// <summary>Start a new session. If a session is already running, behavior depends on implementation (idempotent expected).</summary>
        Task StartAsync();

        /// <summary>Stop the running session (if any) and persist it. optional StopOptions for description/metadata.</summary>
        Task StopAsync(StopOptions? options = null);

        /// <summary>Stop current, then start a new entry atomically Optional StopOptions apply to the stopped entry.</summary>
        Task StopStartAsync(StopOptions? options = null);

        /// <summary>Toggle start/stop: start if idle, stop if running.</summary>
        Task ToggleAsync();

        /// <summary>List recent persisted entries (most recent first).</summary>
        Task<TimeEntry[]> GetRecentAsync(int max = 50);
    }
}