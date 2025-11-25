using FluentAssertions;
using Timetracker.Core;

namespace Timetracker.Tests
{
    public class HotkeyManagerTests
    {
        [Fact]
        public void Register_calls_underlying_api_and_returns_id()
        {
            var fake = new FakeWin32Interop();
            using var sut = new HotkeyManager(fake);

            var hk = new Hotkey(HotkeyModifiers.Control | HotkeyModifiers.Shift, (ushort)0x70); // Ctrl+Shift+F1
            var id = sut.Register(hk);

            id.Should().BeGreaterThan(0);
            fake.RegisterCallCount.Should().Be(1);
            fake.LastRegisteredHotkey.Should().Be(hk);
        }

        [Fact]
        public void Unregister_calls_underlying_api()
        {
            var fake = new FakeWin32Interop();
            using var sut = new HotkeyManager(fake);

            var hk = new Hotkey(HotkeyModifiers.Control, (ushort)0x70);
            var id = sut.Register(hk);

            sut.Unregister(id);

            fake.UnregisterCallCount.Should().Be(1);
            fake.LastUnregisteredId.Should().Be(id);
        }

        [Fact]
        public void HotkeyPressed_is_raised_when_interop_simulates_message()
        {
            var fake = new FakeWin32Interop();
            using var sut = new HotkeyManager(fake);

            var hk = new Hotkey(HotkeyModifiers.Control, (ushort)0x70);
            var id = sut.Register(hk);

            Hotkey? fired = null;
            sut.HotkeyPressed += (_, h) => fired = h;

            // simulate native message
            fake.SimulateHotkeyPress(id);

            fired.Should().NotBeNull();
            fired.Should().Be(hk);
        }

        [Fact]
        public void Dispose_unregisters_all_registered_hotkeys()
        {
            var fake = new FakeWin32Interop();
            var sut = new HotkeyManager(fake);

            var hk1 = new Hotkey(HotkeyModifiers.Control, (ushort)0x70);
            var hk2 = new Hotkey(HotkeyModifiers.Alt, (ushort)0x41); // A

            var id1 = sut.Register(hk1);
            var id2 = sut.Register(hk2);

            sut.Dispose();

            fake.UnregisterCallCount.Should().BeGreaterThanOrEqualTo(2);
            fake.UnregisteredIds.Should().Contain(id1);
            fake.UnregisteredIds.Should().Contain(id2);
        }
    }
}