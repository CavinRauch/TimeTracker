using System.ComponentModel;
using System.Runtime.CompilerServices;
using Timetracker.Infrastructure.Entities;
using Timetracker.Infrastructure.Services;

namespace Timetracker.UI.Wpf.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ISettingsService _settingsService;
    private CancellationTokenSource? _cts;
    private bool _isSaving;
    private bool _promptOnStop;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _ = LoadAsync();
    }

    public bool PromptOnStop
    {
        get => _promptOnStop;
        set
        {
            if (_promptOnStop == value) return;
            _promptOnStop = value;
            OnPropertyChanged();
            DebouncedSave();
        }
    }

    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            _isSaving = value;
            OnPropertyChanged();
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private async Task LoadAsync()
    {
        try
        {
            var settings = await _settingsService.GetAsync().ConfigureAwait(false);
            PromptOnStop = settings.PromptOnStop;
        }
        catch
        {
            // swallow or log; UI can show an error if desired
        }
    }

    private void DebouncedSave()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, token).ConfigureAwait(false);
                IsSaving = true;
                var settings = new AppSettingsEntity { Id = 1, PromptOnStop = PromptOnStop };
                await _settingsService.UpdateAsync(settings).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
                // optionally surface error to UI
            }
            finally
            {
                IsSaving = false;
            }
        }, token);
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}