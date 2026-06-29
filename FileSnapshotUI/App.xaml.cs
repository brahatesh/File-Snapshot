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

        // --- NATIVE WIN32 APIs for restoring app on windows notification click ---
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int SW_RESTORE = 9;
        // -------------------------

        [STAThread]
        public static void Main(string[] args) {
            WinRT.ComWrappersSupport.InitializeComWrappers();

            // Instance detection, kill if duplicate spawns, restore existing instance
            var mainInstance = AppInstance.GetCurrent();
            var activationArgs = mainInstance.GetActivatedEventArgs();

            var keyInstance = AppInstance.FindOrRegisterForKey("FileSnapshotUI-SingleInstance-Key");

            if(!keyInstance.IsCurrent) {
                keyInstance.RedirectActivationToAsync(activationArgs).GetAwaiter().GetResult();
                System.Environment.Exit(0);
                return;
            }

            keyInstance.Activated += KeyInstanceActivated;

            // Dispatcher for making changes in UI thread
            Application.Start((p) => {
                var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                var app = new App();
            });
        }

        // Restore instance
        private static void KeyInstanceActivated(object? sender, AppActivationArguments e) {
            BringWindowToFront();
        }

        // Restore instance when windows notification clicked
        private void NotificationManager_NotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args) {
            BringWindowToFront();
        }

        // Bring window to focus and restore window
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

            // Windows notification
            AppNotificationManager.Default.NotificationInvoked += NotificationManager_NotificationInvoked;
            string appIconPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Assets", "Icon.ico");
            AppNotificationManager.Default.Register("File Snapshot", new Uri(appIconPath));

            // Host for configuration sharing with background threads and UI
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
            // Dispatcher for UI thread
            MainDispatcher = DispatcherQueue.GetForCurrentThread();

            await _host.StartAsync();

            m_window = new MainWindow();
            m_window.Activate();

            // Set min window size
            windowHelper = new WindowHelper(m_window);
            windowHelper.SetWindowMinMaxSize(new WindowHelper.POINT() { x = 700, y = 500 });

            // Hide window, so only app only starts in system tray
            m_window.AppWindow.Hide();

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        }

        private async void CurrentDomain_ProcessExit(object? sender, EventArgs e) {
            // On kill, kill background tasks
            await _host.StopAsync(TimeSpan.FromSeconds(60));
            _host.Dispose();
        }
    }
}

