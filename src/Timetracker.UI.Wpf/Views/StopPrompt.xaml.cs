using System.Windows;
using Timetracker.Core;
using Timetracker.UI.ViewModels;

namespace Timetracker.UI.Wpf.Views;

public partial class StopPrompt : Window
{
    public StopPrompt()
    {
        InitializeComponent();
    }

    public static Task<StopOptions?> ShowDialogAsync(Window owner, StopPromptViewModel vm)
    {
        var tcs = vm.Completion;
        var w = new StopPrompt { Owner = owner, DataContext = vm };
        w.Show();

        // When completion set, close window on UI thread
        tcs.Task.ContinueWith(_ => { w.Dispatcher.Invoke(() => w.Close()); }, TaskScheduler.Default);

        return tcs.Task;
    }
}