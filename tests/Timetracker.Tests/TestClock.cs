using Timetracker.Core;

namespace Timetracker.Tests
{
    internal class TestClock : IClock
    {
        private DateTimeOffset _now;
        public TestClock(DateTimeOffset initial) => _now = initial;
        public DateTimeOffset Now => _now;
        public void Advance(TimeSpan delta) => _now = _now.Add(delta);
        public void Set(DateTimeOffset value) => _now = value;
    }
}