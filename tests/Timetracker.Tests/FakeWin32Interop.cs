using Timetracker.Core;

namespace Timetracker.Tests
{
    // Minimal fake interop: records calls and can simulate WM_HOTKEY messages.
    internal class FakeWin32Interop : IWin32Interop
    {
        private readonly Dictionary<int, Hotkey> _registered = new();
        private int _nextId = 1;

        public int RegisterCallCount { get; private set; }
        public int UnregisterCallCount { get; private set; }
        public Hotkey? LastRegisteredHotkey { get; private set; }
        public int LastUnregisteredId { get; private set; }
        public List<int> UnregisteredIds { get; } = new();

        public int RegisterHotKey(IntPtr hWnd, HotkeyModifiers modifiers, ushort vk)
        {
            RegisterCallCount++;
            var id = _nextId++;
            var hk = new Hotkey(modifiers, vk);
            _registered[id] = hk;
            LastRegisteredHotkey = hk;
            return id;
        }

        public bool UnregisterHotKey(IntPtr hWnd, int id)
        {
            UnregisterCallCount++;
            LastUnregisteredId = id;
            UnregisteredIds.Add(id);
            return _registered.Remove(id);
        }

        public event Action<int, Hotkey>? MessageReceived;

        // Test helper to simulate WM_HOTKEY delivered by OS.  
        public void SimulateHotkeyPress(int id)
        {
            if (_registered.TryGetValue(id, out var hk))
            {
                RaiseMessageReceived(id, hk);
            }
        }

        private void RaiseMessageReceived(int id, Hotkey hk) => MessageReceived?.Invoke(id, hk);
    }
}