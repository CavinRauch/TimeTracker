using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Timetracker.Core;
using Timetracker.Infrastructure;
using Timetracker.Infrastructure.Caching;
using Timetracker.Infrastructure.Context;
using Timetracker.UI.Wpf.Services;
using Timetracker.UI.Wpf.ViewModels;

namespace Timetracker.UI.Wpf
{
    public partial class App : Application
    {
        public App()
        {
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddDbContextFactory<TimetrackerDbContext>(options =>
                        options.UseSqlite(InfrastructureConstants.ConnectionString));
                    services.AddSingleton<ICachedDbSets, CachedDbSets>();

                    services.AddSingleton<IClock, SystemClock>();
                    services.AddSingleton<ITimeEntryService, DbTimeService>();

                    services.AddSingleton<AppHotkeyHandler>();
                    services.AddSingleton<HotkeyHandler>();
                    services.AddSingleton<DashboardViewModel>();

                    services.AddSingleton<MainWindow>();

                    services.BuildServiceProvider();
                })
                .Build();
        }

        public IHost AppHost { get; }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // start host if not already started
            await AppHost!.StartAsync().ConfigureAwait(false);

            // apply EF migrations here (so DB schema exists before UI uses it)
            try
            {
                using var scope = AppHost.Services.CreateScope();
                var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TimetrackerDbContext>>();
                await using var db = dbFactory.CreateDbContext();
                await db.Database.MigrateAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // log or show a friendly error; avoid swallowing critical startup failures
                // for debug you can rethrow or show a messagebox
                MessageBox.Show($"Failed to migrate DB: {ex.Message}", "Startup error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                throw;
            }

            // Prime cache
            var cached = AppHost.Services.GetRequiredService<ICachedDbSets>();
            await cached.RefreshAllAsync().ConfigureAwait(false);

            // Resolve and show the single MainWindow instance
            var main = AppHost!.Services.GetRequiredService<MainWindow>();
            main.Show();

            // Now the window is created and has a handle / HwndSource
            var hwndSource = (HwndSource)HwndSource.FromVisual(main)!;
            var hwnd = new WindowInteropHelper(main).Handle;

            // Create Win32Interop and HotkeyManager now that we have the real window
            var win32Interop = new Win32Interop(hwndSource);
            var hotkeyManager = new HotkeyManager(win32Interop, hwnd);

            // Register the hotkey (use KeyInterop to convert WPF Key to virtual-key)
            var hkStopStart = new Hotkey(HotkeyModifiers.Control, (ushort)KeyInterop.VirtualKeyFromKey(Key.F1));
            var hkPause = new Hotkey(HotkeyModifiers.Control, (ushort)KeyInterop.VirtualKeyFromKey(Key.F2));

            hotkeyManager.Register(hkStopStart);
            hotkeyManager.Register(hkPause);

            //Wire the AppHotkeyHandler (manually or replace DI registration)
            var timeSvc = AppHost.Services.GetRequiredService<HotkeyHandler>();
            var appHotkeyHandler = new AppHotkeyHandler(hotkeyManager, timeSvc);

            //Keep references so they can be disposed on exit
            Current.Properties["AppHotkeyHandler"] = appHotkeyHandler;
            Current.Properties["HotkeyManager"] = hotkeyManager;
            Current.Properties["Win32Interop"] = win32Interop;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (Current.Properties["AppHotkeyHandler"] is IDisposable d) d.Dispose();
            if (Current.Properties["HotkeyManager"] is IDisposable d2) d2.Dispose();
            if (Current.Properties["Win32Interop"] is IDisposable d3) d3.Dispose();

            AppHost?.Dispose();
            base.OnExit(e);
        }
    }
}