namespace Timetracker.Core
{
    public interface IHotkeyManager : IDisposable
    {
        /// <summary>
        /// Raised when a registered hotkey is pressed.
        /// </summary>
        event EventHandler<Hotkey>? HotkeyPressed;

        /// <summary>
        /// Registers the hotkey and returns an opaque id (used to unregister).
        /// Throws if registration fails.
        /// </summary>
        int Register(Hotkey hotkey);

        /// <summary>
        /// Unregisters the previously registered hotkey id.
        /// </summary>
        void Unregister(int registrationId);
    }
}