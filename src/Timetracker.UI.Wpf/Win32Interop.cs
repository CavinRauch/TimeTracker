using System.Runtime.InteropServices;
using System.Windows.Interop;
using Timetracker.Core;

namespace Timetracker.UI.Wpf
{
    public class Win32Interop : IWin32Interop, IDisposable
    {
        private readonly HwndSource? _hwndSource;

        private int _idGenerator = 1;

        public Win32Interop(HwndSource hwndSource)
        {
            _hwndSource = hwndSource ?? throw new ArgumentNullException(nameof(hwndSource));
            _hwndSource.AddHook(WndProc);
        }

        public void Dispose()
        {
            if (_hwndSource != null)
                _hwndSource.RemoveHook(WndProc);
        }

        public event Action<int, Hotkey>? MessageReceived;

        public int RegisterHotKey(IntPtr hWnd, HotkeyModifiers modifiers, ushort vk)
        {
            // use an incrementing id per registration; native RegisterHotKey uses an id int
            var id = _idGenerator++;
            if (!NativeMethods.RegisterHotKey(hWnd, id, (uint)modifiers, vk))
                throw new InvalidOperationException("RegisterHotKey failed: " + Marshal.GetLastWin32Error());
            return id;
        }

        public bool UnregisterHotKey(IntPtr hWnd, int id)
        {
            return NativeMethods.UnregisterHotKey(hWnd, id);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY)
            {
                var id = wParam.ToInt32();
                var vk = (ushort)((lParam.ToInt64() >> 16) & 0xFFFF);
                var modifiers = (HotkeyModifiers)((int)lParam & 0xFFFF);
                MessageReceived?.Invoke(id, new Hotkey(modifiers, vk));
            }

            return IntPtr.Zero;
        }

        private static class NativeMethods
        {
            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, ushort vk);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        }
    }
}