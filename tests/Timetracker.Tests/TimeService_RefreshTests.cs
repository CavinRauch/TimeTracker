using FluentAssertions;
using Timetracker.Core;
using Timetracker.UI.Wpf.ViewModels;

namespace Timetracker.Tests
{
    public class DashboardViewModel_RefreshTests
    {
        [Fact]
        public async Task RefreshAsync_populates_Recent_after_entry_stopped()
        {
            // Arrange: start at fixed time, then stop after 30 minutes
            var start = new DateTimeOffset(2025, 11, 16, 9, 0, 0, TimeSpan.Zero);
            var stop = start.AddMinutes(30);
            var clock = new TestClock(start);
            var timeService = new MemoryTimeEntryService(clock);

            // act: start and then advance time and stop
            await timeService.StartAsync();
            clock.Set(stop);
            await timeService.StopAsync();

            // create view model and refresh from the service
            var vm = new DashboardViewModel(timeService, clock);

            // perform refresh
            await vm.RefreshAsync();

            // Assert: Recent has one entry and fields are sensible
            vm.Recent.Should().HaveCount(1);
            var row = vm.Recent.First();

            // Date should equal the start date in local time representation; we use the invariant format assumption
            row.Date.Should().NotBeNullOrEmpty();
            row.StartTime.Should().NotBeNullOrEmpty();
            row.EndTime.Should().NotBeNullOrEmpty();
            row.Elapsed.Should().NotBeNullOrEmpty();

            // Ensure elapsed formatting corresponds to 30 minutes (either "30:00" or "0:30:00"-like)
            var elapsed = row.Elapsed;
            elapsed.Should().Contain("30"); // basic sanity check that 30 minutes appear in the elapsed text
        }
    }
}