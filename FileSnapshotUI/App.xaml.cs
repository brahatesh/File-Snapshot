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

namespace FileSnapshotUI
{
    public partial class App : Application
    {
        public static Microsoft.UI.Dispatching.DispatcherQueue MainDispatcher { get; private set; }
        private static WindowHelper? windowHelper;

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

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            MainDispatcher = DispatcherQueue.GetForCurrentThread();

            await _host.StartAsync();

            var window = new MainWindow();
            window.Activate();

            windowHelper = new WindowHelper(window);
            windowHelper.SetWindowMinMaxSize(new WindowHelper.POINT() { x = 700, y = 500 });

            window.AppWindow.Hide();

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        }

        private async void CurrentDomain_ProcessExit(object? sender, EventArgs e) {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }
    }
}

