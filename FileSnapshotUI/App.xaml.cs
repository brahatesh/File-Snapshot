using FileSnapshotUI.Helpers;
using FileSnapshotUI.Services;
using FileSnapshotUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace FileSnapshotUI {
    public partial class App : Application {
        public static Microsoft.UI.Dispatching.DispatcherQueue MainDispatcher { get; private set; }
        private static WindowHelper? windowHelper;
        public static MainWindow? m_window;

        private readonly IHost _host;
        public static IServiceProvider Services { get; private set; }

        // --- NATIVE WIN32 APIs ---
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int SW_RESTORE = 9;
        // -------------------------

        [STAThread]
        public static void Main(string[] args) {
            WinRT.ComWrappersSupport.InitializeComWrappers();

            var mainInstance = AppInstance.GetCurrent();
            var activationArgs = mainInstance.GetActivatedEventArgs();

            var keyInstance = AppInstance.FindOrRegisterForKey("FileSnapshotUI-SingleInstance-Key");

            if(!keyInstance.IsCurrent) {
                keyInstance.RedirectActivationToAsync(activationArgs).GetAwaiter().GetResult();
                System.Environment.Exit(0);
                return;
            }

            keyInstance.Activated += KeyInstanceActivated;

            Application.Start((p) => {
                var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                var app = new App();
            });
        }

        private static void KeyInstanceActivated(object? sender, AppActivationArguments e) {
            BringWindowToFront();
        }

        private void NotificationManager_NotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args) {
            BringWindowToFront();
        }

        private static void BringWindowToFront() {
            if (MainDispatcher != null && m_window != null) {
                MainDispatcher.TryEnqueue(() => {
                    IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(m_window);
                    m_window.AppWindow.Show();
                    ShowWindow(hwnd, SW_RESTORE);
                    SetForegroundWindow(hwnd);
                });
            }
        }

        public App() {
            this.InitializeComponent();

            AppNotificationManager.Default.NotificationInvoked += NotificationManager_NotificationInvoked;
            AppNotificationManager.Default.Register();

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

        protected override async void OnLaunched(LaunchActivatedEventArgs args) {
            MainDispatcher = DispatcherQueue.GetForCurrentThread();

            await _host.StartAsync();

            m_window = new MainWindow();
            m_window.Activate();

            windowHelper = new WindowHelper(m_window);
            windowHelper.SetWindowMinMaxSize(new WindowHelper.POINT() { x = 700, y = 500 });

            m_window.AppWindow.Hide();

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        }

        private async void CurrentDomain_ProcessExit(object? sender, EventArgs e) {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }
    }
}

