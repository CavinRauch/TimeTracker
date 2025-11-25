using System.Collections.Concurrent;

namespace Timetracker.Core
{
    public class HotkeyManager : IHotkeyManager
    {
        private readonly IWin32Interop _interop;
        private readonly ConcurrentDictionary<int, Hotkey> _registrations = new();
        private readonly IntPtr _windowHandle;

        public HotkeyManager(IWin32Interop interop, IntPtr? windowHandle = default)
        {
            _interop = interop ?? throw new ArgumentNullException(nameof(interop));
            _windowHandle = windowHandle ?? IntPtr.Zero;
            _interop.MessageReceived += OnMessageReceived;
        }

        public event EventHandler<Hotkey>? HotkeyPressed;

        public int Register(Hotkey hotkey)
        {
            var id = _interop.RegisterHotKey(_windowHandle, hotkey.Modifiers, hotkey.VirtualKey);
            if (id <= 0)
                throw new InvalidOperationException("RegisterHotKey failed; id <= 0");

            _registrations[id] = hotkey;
            return id;
        }

        public void Unregister(int registrationId)
        {
            if (_registrations.TryRemove(registrationId, out _))
            {
                _interop.UnregisterHotKey(_windowHandle, registrationId);
            }
        }

        public void Dispose()
        {
            // Best-effort unregister of all registered hotkeys
            foreach (var id in _registrations.Keys)
            {
                try
                {
                    _interop.UnregisterHotKey(_windowHandle, id);
                }
                catch
                {
                    /* swallow */
                }
            }

            _registrations.Clear();
            _interop.MessageReceived -= OnMessageReceived;
        }

        private void OnMessageReceived(int id, Hotkey hk)
        {
            if (_registrations.ContainsKey(id))
            {
                HotkeyPressed?.Invoke(this, hk);
            }
        }
    }
}