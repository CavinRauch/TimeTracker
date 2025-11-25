using System.Globalization;
using Timetracker.Core;

namespace Timetracker.UI.Wpf.ViewModels
{
    public class HistoryRow
    {
        public HistoryRow(TimeEntry entry, IClock clock, IFormatProvider? culture = null)
        {
            culture ??= CultureInfo.CurrentCulture;

            var startLocal = entry.Start.ToLocalTime();
            var endLocal = entry.End?.ToLocalTime();

            Date = startLocal.ToString("d", culture);
            StartTime = startLocal.ToString("T", culture);
            EndTime = endLocal?.ToString("T", culture) ?? "-";

            var elapsed = entry.GetElapsed(clock);
            Elapsed = entry.End is null ? $"Running {FormatTimeSpan(elapsed)}" : FormatTimeSpan(elapsed);
            Id = entry.Id;
        }

        public Guid Id { get; }
        public string Date { get; }
        public string StartTime { get; }
        public string EndTime { get; }
        public string Elapsed { get; }

        private static string FormatTimeSpan(TimeSpan ts)
        {
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}:{ts.Minutes:D2}";
            return $"{ts.Minutes}:{ts.Seconds:D2}";
        }
    }
}