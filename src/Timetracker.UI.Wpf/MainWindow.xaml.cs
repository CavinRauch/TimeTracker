using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Timetracker.UI.Wpf.ViewModels;

namespace Timetracker.UI.Wpf
{
    public partial class MainWindow : Window
    {
        private readonly DashboardViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();

            // Resolve the VM from the App's DI container
            _vm = ((App)Application.Current).AppHost.Services.GetRequiredService<DashboardViewModel>();
            this.DataContext = _vm;

            // Initialize async without blocking UI
            _ = _vm.InitializeAsync();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await _vm.RefreshAsync();
        }
    }
}