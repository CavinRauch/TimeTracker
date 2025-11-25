using System.Windows.Input;
using Timetracker.Core;
using Timetracker.UI.Wpf.Services;

namespace Timetracker.UI.Wpf
{
    // Subscribes to hotkey events and routes them to the time service.
    public class AppHotkeyHandler : IDisposable
    {
        private readonly HotkeyHandler _hotkeyHandler;
        private readonly IHotkeyManager _hotkeyManager;

        public AppHotkeyHandler(IHotkeyManager hotkeyManager, HotkeyHandler hotkeyHandler)
        {
            _hotkeyManager = hotkeyManager ?? throw new ArgumentNullException(nameof(hotkeyManager));
            _hotkeyHandler = hotkeyHandler ?? throw new ArgumentNullException(nameof(hotkeyHandler));

            _hotkeyManager.HotkeyPressed += OnHotkeyPressed;
        }

        public void Dispose()
        {
            _hotkeyManager.HotkeyPressed -= OnHotkeyPressed;
        }

        private async void OnHotkeyPressed(object? sender, Hotkey h)
        {
            try
            {
                var vk = h.VirtualKey;

                var f1 = (ushort)KeyInterop.VirtualKeyFromKey(Key.F1);
                var f2 = (ushort)KeyInterop.VirtualKeyFromKey(Key.F2);

                if (vk == (ushort)KeyInterop.VirtualKeyFromKey(Key.F1))
                {
                    await _hotkeyHandler.HandleStopStartHotkeyAsync();
                    return;
                }

                if (vk == (ushort)KeyInterop.VirtualKeyFromKey(Key.F2))
                {
                    await _hotkeyHandler.HandleStopHotkeyAsync();
                    return;
                }
            }
            catch
            {
                // Consider logging the exception (Serilog/Microsoft.Extensions.Logging) in production.
            }
        }
    }
}