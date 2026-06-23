using FileSnapshotUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SystemTray.Core;

// -----------------------------------------------------------------------------
// SystemTray for WinUI 3
// A complete system tray (notification area) implementation for WinUI 3 apps.
//
// Repository: https://github.com/MEHDIMYADI
// Author: Mehdi Dimyadi
// License: MIT
// -----------------------------------------------------------------------------

namespace FileSnapshotUI.Pages
{
    public sealed partial class SettingsPage : Page
    {
        public RootViewModel? ViewModel;
        private SystemTrayManager? systemTrayManager;
        private MainWindow? _hostWindow;

        public SettingsPage()
        {
            this.InitializeComponent();
        }

        public void SetManager(SystemTrayManager manager)
        {
            systemTrayManager = manager;
        }

        private void TrayIconToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (systemTrayManager != null)
            {
                systemTrayManager.IsIconVisible = TrayIconToggle.IsOn;
                if (TrayIconToggle.IsOn == false)
                {
                    systemTrayManager.CloseButtonMinimizesToTray = false;
                    CloseButtonTrayToggle.IsOn = false;
                }
                MinimizeToTrayToggle.IsEnabled = TrayIconToggle.IsOn;
                CloseButtonTrayToggle.IsEnabled = TrayIconToggle.IsOn;
            }
        }

        private void MinimizeToTrayToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (systemTrayManager != null)
            {
                systemTrayManager.MinimizeToTray = MinimizeToTrayToggle.IsOn;
            }
        }

        private void CloseToMinimizesInTrayToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (systemTrayManager != null)
            {
                systemTrayManager.CloseButtonMinimizesToTray = CloseButtonTrayToggle.IsOn;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is MainWindow window) {
                _hostWindow = window;
                ViewModel = window.ViewModel;
                systemTrayManager = window.systemTrayManager;

                TrayIconToggle.IsOn = systemTrayManager.IsIconVisible;
                MinimizeToTrayToggle.IsOn = systemTrayManager.MinimizeToTray;
                CloseButtonTrayToggle.IsOn = systemTrayManager.CloseButtonMinimizesToTray;
            }
        }

        private void SettingsEditFullPath_Click(object sender, RoutedEventArgs e) {

        }

        private void SettingsEditBackupPath_Click(object sender, RoutedEventArgs e) {

        }
    }
}