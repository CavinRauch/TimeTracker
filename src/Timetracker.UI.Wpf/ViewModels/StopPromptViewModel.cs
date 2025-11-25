using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Timetracker.Core;

namespace Timetracker.UI.ViewModels
{
    public class StopPromptField
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class StopPromptViewModel : ObservableObject // assume base implements INotifyPropertyChanged
    {
        private string _description = string.Empty;

        public StopPromptViewModel()
        {
            Fields = new ObservableCollection<StopPromptField>();
            AddFieldCommand = new RelayCommand(() => Fields.Add(new StopPromptField()));
            RemoveFieldCommand = new RelayCommand<object?>(p =>
            {
                if (p is StopPromptField f) Fields.Remove(f);
            });
            OkCommand = new AsyncRelayCommand(OkAsync);
            CancelCommand = new RelayCommand(Cancel);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public ObservableCollection<StopPromptField> Fields { get; }

        public ICommand AddFieldCommand { get; }
        public ICommand RemoveFieldCommand { get; }
        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        // TaskCompletionSource used by the Window code-behind to await result
        internal TaskCompletionSource<StopOptions?> Completion { get; } = new();

        private void Cancel()
        {
            Completion.TrySetResult(null);
        }

        private Task OkAsync()
        {
            var dict = Fields
                .Where(f => !string.IsNullOrWhiteSpace(f.Key))
                .ToDictionary(f => f.Key!, f => f.Value ?? string.Empty);

            var options = new StopOptions(
                Description: string.IsNullOrWhiteSpace(Description) ? null : Description,
                Fields: dict.Count == 0 ? null : dict
            );

            Completion.TrySetResult(options);
            return Task.CompletedTask;
        }
    }
}