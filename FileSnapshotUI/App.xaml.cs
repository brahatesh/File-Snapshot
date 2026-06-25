using FileSnapshotUI.Helpers;
using FileSnapshotUI.Services;
using FileSnapshotUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppLifecycle;

namespace FileSnapshotUI
{
    public partial class App : Application
    {
        public static Microsoft.UI.Dispatching.DispatcherQueue MainDispatcher { get; private set; }
        private static WindowHelper? windowHelper;
        public static MainWindow? MainAppWindow { get; private set; }

        private readonly IHost _host;
        public static IServiceProvider Services { get; private set; }

        [STAThread]
        public static void Main(string[] args)
        {
            Application.Start((p) =>
            {
                var app = new App();
            });
        }

        public App()
        {
            this.InitializeComponent();

            var mainInstance = AppInstance.FindOrRegisterForKey("FileSnapshotMain");

            if (!mainInstance.IsCurrent) {
                var currentArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
                mainInstance.RedirectActivationToAsync(currentArgs).AsTask().Wait();

                System.Diagnostics.Process.GetCurrentProcess().Kill();
                return;
            }
            mainInstance.Activated += MainInstance_Activated;

            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) => {
                    services.AddSingleton<NotificationService>();
                    services.AddSingleton<BackgroundTaskQueue>();
                    services.AddSingleton<RootViewModel>();
                    services.AddSingleton<SnapshotService>();

                    // Register both background services
                    services.AddHostedService<TaskProcessingService>();
                    services.AddHostedService<SnapshotTimerService>();
                })
                .Build();

            Services = _host.Services;
        }

        private void MainInstance_Activated(object sender, AppActivationArguments e) {
            if (MainAppWindow != null) {
                MainAppWindow.DispatcherQueue.TryEnqueue(() => {
                    MainAppWindow.Activate();
                });
            }
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            AppNotificationManager.Default.Register();
            MainDispatcher = DispatcherQueue.GetForCurrentThread();

            await _host.StartAsync();

            MainAppWindow = new MainWindow();
            MainAppWindow.Activate();

            windowHelper = new WindowHelper(MainAppWindow);
            windowHelper.SetWindowMinMaxSize(new WindowHelper.POINT() { x = 700, y = 500 });

            MainAppWindow.AppWindow.Hide();

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        }

        private async void CurrentDomain_ProcessExit(object? sender, EventArgs e) {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }
    }
}

