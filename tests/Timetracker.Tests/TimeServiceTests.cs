using FluentAssertions;
using NSubstitute;
using Timetracker.Core;

namespace Timetracker.Tests
{
    public class TimeServiceTests
    {
        private readonly IClock _clock = Substitute.For<IClock>();

        [Fact]
        public async Task StartAsync_when_no_session_creates_running_session()
        {
            var now = new DateTimeOffset(2025, 1, 1, 9, 0, 0, TimeSpan.Zero);
            _clock.Now.Returns(now);

            var sut = new MemoryTimeEntryService(_clock);

            await sut.StartAsync();

            sut.Current.Should().NotBeNull();
            sut.Current!.Start.Should().Be(now);
            (await sut.GetRecentAsync()).Should().BeEmpty();
        }

        [Fact]
        public async Task StartAsync_when_already_running_is_idempotent()
        {
            var now = new DateTimeOffset(2025, 1, 1, 9, 0, 0, TimeSpan.Zero);
            _clock.Now.Returns(now);

            var sut = new MemoryTimeEntryService(_clock);

            await sut.StartAsync();
            await sut.StartAsync(); // second call

            sut.Current.Should().NotBeNull();
            sut.Current!.Start.Should().Be(now);
            (await sut.GetRecentAsync()).Should().BeEmpty();
        }

        [Fact]
        public async Task StopAsync_when_running_persists_entry_and_clears_current()
        {
            var start = new DateTimeOffset(2025, 1, 1, 9, 0, 0, TimeSpan.Zero);
            var end = new DateTimeOffset(2025, 1, 1, 10, 0, 0, TimeSpan.Zero);

            _clock.Now.Returns(start, end); // first call start, second call end

            var sut = new MemoryTimeEntryService(_clock);

            await sut.StartAsync();
            await sut.StopAsync();

            sut.Current.Should().BeNull();
            var recent = await sut.GetRecentAsync();
            recent.Should().HaveCount(1);
            recent[0].Start.Should().Be(start);
            recent[0].End.Should().Be(end);
        }

        [Fact]
        public async Task ToggleAsync_starts_when_idle_and_stops_when_running()
        {
            var start = new DateTimeOffset(2025, 1, 1, 9, 0, 0, TimeSpan.Zero);
            var stop = new DateTimeOffset(2025, 1, 1, 9, 30, 0, TimeSpan.Zero);

            _clock.Now.Returns(start, stop);

            var sut = new MemoryTimeEntryService(_clock);

            await sut.ToggleAsync(); // should start
            sut.Current.Should().NotBeNull();

            await sut.ToggleAsync(); // should stop
            sut.Current.Should().BeNull();

            var recent = await sut.GetRecentAsync();
            recent.Should().HaveCount(1);
            recent[0].Start.Should().Be(start);
            recent[0].End.Should().Be(stop);
        }

        [Fact]
        public async Task StopStartAsync_is_atomic_under_concurrent_calls()
        {
            // Arrange: create MemoryTimeEntryService or a test DbTimeService with in-memory DB
            var clock = new TestClock(DateTimeOffset.Now); // implement IClock for tests
            var svc = new MemoryTimeEntryService(clock);

            // Start an entry
            await svc.StartAsync();

            // Act: call StopStartAsync concurrently from multiple tasks
            var tasks = Enumerable.Range(0, 8).Select(_ => Task.Run(() => svc.StopStartAsync())).ToArray();
            await Task.WhenAll(tasks);

            // Assert: only one persisted entry exists in recent history
            var recent = await svc.GetRecentAsync(10);
            Assert.Equal(1, recent.Length);
            Assert.NotNull(svc.Current);
        }
    }
}