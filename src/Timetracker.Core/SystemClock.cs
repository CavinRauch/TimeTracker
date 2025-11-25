namespace Timetracker.Core
{
    public class SystemClock : IClock
    {
        public DateTimeOffset Now => DateTimeOffset.Now;
    }
}