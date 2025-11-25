using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Timetracker.Core;

namespace Timetracker.UI.Wpf.ViewModels
{
    public class DashboardViewModel : ObservableObject, IDisposable
    {
        private readonly IClock _clock;
        private readonly ITimeEntryService _timeEntryService;
        private readonly Timer _timer;

        private TimeEntry? _current;

        private string _elapsed = "00:00:00";

        public DashboardViewModel(ITimeEntryService timeEntryService, IClock clock)
        {
            _timeEntryService = timeEntryService ?? throw new ArgumentNullException(nameof(timeEntryService));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));

            StartStopCommand = new AsyncRelayCommand(ToggleAsync);
            Recent = new ObservableCollection<HistoryRow>();

            // subscribe to persisted entries so the UI updates immediately
            _timeEntryService.EntryStopped += OnStopped;

            // Timer to tick every second for elapsed updates (uses ThreadPool)
            _timer = new Timer(async _ => await RefreshCurrentElapsedAsync(), null, TimeSpan.Zero,
                TimeSpan.FromSeconds(1));
        }

        public IAsyncRelayCommand StartStopCommand { get; }

        public TimeEntry? Current
        {
            get => _current;
            private set => SetProperty(ref _current, value);
        }

        public string Elapsed
        {
            get => _elapsed;
            private set => SetProperty(ref _elapsed, value);
        }

        public ObservableCollection<HistoryRow> Recent { get; }

        public void Dispose()
        {
            _timer?.Dispose();
            _timeEntryService.EntryStopped -= OnStopped;
        }

        public async Task InitializeAsync()
        {
            await RefreshAsync();
        }

        public async Task RefreshAsync()
        {
            var entries = await _timeEntryService.GetRecentAsync(50);
            Recent.Clear();
            foreach (var e in entries.OrderByDescending(x => x.Start))
                Recent.Add(new HistoryRow(e, _clock));
            Current = _timeEntryService.Current;
            UpdateElapsedForCurrent();
        }

        private async Task RefreshCurrentElapsedAsync()
        {
            // Called from Timer thread; marshal property updates to the UI thread by using Dispatcher if needed.
            // In a simple approach, read values and update properties on UI thread via Application.Current.Dispatcher.
            var current = _timeEntryService.Current;
            var elapsed = current is null ? TimeSpan.Zero : current.GetElapsed(_clock);

            var formatted = FormatElapsed(elapsed);

            // marshal to UI thread
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Current = _timeEntryService.Current;
                Elapsed = formatted;
            });
            await Task.CompletedTask;
        }

        public async Task ToggleAsync() => await _timeEntryService.ToggleAsync();

        private void UpdateElapsedForCurrent()
        {
            if (Current == null)
            {
                Elapsed = "00:00";
                return;
            }

            var elapsed = Current.GetElapsed(_clock);
            Elapsed = FormatElapsed(elapsed);
        }

        private static string FormatElapsed(TimeSpan ts)
        {
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
            return $"{ts.Minutes:D2}:{ts.Seconds:D2}";
        }

        private void OnStopped(object? sender, TimeEntry entry)
        {
            // This may be raised from a non-UI thread — marshal to UI thread.
            Application.Current?.Dispatcher.BeginInvoke(new Action(async () => { await RefreshAsync(); }));
        }
    }
}