using System.Windows;
using Timetracker.Core;
using Timetracker.UI.ViewModels;
using Timetracker.UI.Wpf.Views;

namespace Timetracker.UI.Wpf.Services;

public class HotkeyHandler
{
    private readonly bool _hotkeyShowsPrompt; // read from settings
    private readonly IServiceProvider _services; // to resolve dialog VM or main window
    private readonly ITimeEntryService _timeService;

    public HotkeyHandler(ITimeEntryService timeService, IServiceProvider services, bool hotkeyShowsPrompt = true)
    {
        _timeService = timeService;
        _services = services;
        _hotkeyShowsPrompt = hotkeyShowsPrompt;
    }

    // Call this from your global key hook or InputBindings
    public async Task HandleStopStartHotkeyAsync()
    {
        if (_hotkeyShowsPrompt)
        {
            // show prompt on UI thread
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                var vm = new StopPromptViewModel();
                var owner = Application.Current.MainWindow;
                var result = await StopPrompt.ShowDialogAsync(owner, vm).ConfigureAwait(false);

                // result is StopOptions?; call service on background thread
                if (result != null)
                    await _timeService.StopStartAsync(result).ConfigureAwait(false);
                else
                    await _timeService.StopStartAsync(null).ConfigureAwait(false); // or do nothing if you prefer
            }).Task.ConfigureAwait(false);
        }
        else
        {
            // fast path: call service directly (background thread)
            await _timeService.StopStartAsync().ConfigureAwait(false);
        }
    }

    public async Task HandleStopHotkeyAsync()
    {
        if (_hotkeyShowsPrompt)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                var vm = new StopPromptViewModel();
                var owner = Application.Current.MainWindow;
                var result = await StopPrompt.ShowDialogAsync(owner, vm).ConfigureAwait(false);

                if (result != null)
                    await _timeService.StopAsync(result).ConfigureAwait(false);
                else
                    await _timeService.StopAsync(null).ConfigureAwait(false);
            }).Task.ConfigureAwait(false);
        }
        else
        {
            await _timeService.StopAsync().ConfigureAwait(false);
        }
    }
}