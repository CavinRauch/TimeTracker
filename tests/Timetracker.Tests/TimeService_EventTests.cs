using FluentAssertions;
using Timetracker.Core;

namespace Timetracker.Tests
{
    public class MemoryTimeService_EventTests
    {
        [Fact]
        public async Task EntryPersisted_is_raised_when_StopAsync_called()
        {
            var start = new DateTimeOffset(2025, 01, 01, 09, 00, 00, TimeSpan.Zero);
            var end = start.AddHours(1);
            var clock = new TestClock(start);
            var sut = new MemoryTimeEntryService(clock);

            TimeEntry? persisted = null;
            sut.EntryStopped += (_, e) => persisted = e;

            await sut.StartAsync();

            // advance clock to stop time
            clock.Set(end);
            await sut.StopAsync();

            persisted.Should().NotBeNull();
            persisted!.Start.Should().Be(start);
            persisted.End.Should().Be(end);

            // ensure history contains the entry
            var recent = await sut.GetRecentAsync();
            recent.Should().HaveCount(1);
            recent[0].Start.Should().Be(start);
            recent[0].End.Should().Be(end);
        }

        [Fact]
        public async Task StopStartAsync_stops_then_starts_atomic()
        {
            var start = new DateTimeOffset(2025, 1, 1, 9, 0, 0, TimeSpan.Zero);
            var clock = new TestClock(start);
            var svc = new MemoryTimeEntryService(clock);

            await svc.StartAsync();
            clock.Advance(TimeSpan.FromMinutes(30));

            await svc.StopStartAsync();

            // after StopStartAsync: history has one entry, Current is non-null
            var recent = await svc.GetRecentAsync();
            recent.Should().HaveCount(1);
            svc.Current.Should().NotBeNull();
            recent[0].End.Should().Be(clock.Now); // End equals the time StopStart recorded
        }

        [Fact]
        public async Task StopStartAsync_is_atomic_under_concurrent_calls()
        {
            // Arrange
            var start = new DateTimeOffset(2025, 01, 01, 09, 00, 00, TimeSpan.Zero);
            var clock = new TestClock(start);
            var svc = new MemoryTimeEntryService(clock);

            // Start an entry
            await svc.StartAsync();

            // Advance time so stop will record a non-zero duration
            clock.Advance(TimeSpan.FromMinutes(30));

            // Act
            // Kick off multiple concurrent StopStartAsync calls to simulate races
            var tasks = Enumerable.Range(0, 10)
                .Select(_ => Task.Run(() => svc.StopStartAsync()))
                .ToArray();

            await Task.WhenAll(tasks);

            // Assert
            var recent = await svc.GetRecentAsync(10);
            // Only one persisted entry should exist (the stop portion executed once)
            recent.Should().HaveCount(1);

            // Current should be started (non-null) and have a Start >= previous stop time
            svc.Current.Should().NotBeNull();
            var persisted = recent.Single();
            persisted.End.Should().Be(clock.Now);

            // The started Current must have Start equal to the time StopStartAsync used for the new start
            svc.Current!.Start.Should().Be(clock.Now);
        }
    }
}