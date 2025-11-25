namespace Timetracker.Core
{
    public interface IWin32Interop
    {
        /// <summary>
        /// Registers a hotkey and returns a registration id (positive int) or throws on failure.
        /// </summary>
        int RegisterHotKey(IntPtr hWnd, HotkeyModifiers modifiers, ushort vk);

        /// <summary>
        /// Unregisters a previously registered hotkey id.
        /// </summary>
        bool UnregisterHotKey(IntPtr hWnd, int id);

        /// <summary>
        /// Event that the concrete Win32 implementation will raise when WM_HOTKEY is observed.
        /// Tests can raise this event in fakes.
        /// </summary>
        event Action<int, Hotkey>? MessageReceived;
    }
}