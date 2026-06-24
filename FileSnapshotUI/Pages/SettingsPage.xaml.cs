using FileSnapshotUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SystemTray.Core;
using FileSnapshotUI.Helpers;
using System.IO;
using System.Threading.Tasks;
using System;

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

                FileSettingsPanel.Visibility = ViewModel?.SelectedFile != null ?
                    Visibility.Visible : Visibility.Collapsed;

                TrayIconToggle.IsOn = systemTrayManager.IsIconVisible;
                MinimizeToTrayToggle.IsOn = systemTrayManager.MinimizeToTray;
                CloseButtonTrayToggle.IsOn = systemTrayManager.CloseButtonMinimizesToTray;
                if (ViewModel!=null && ViewModel.SelectedFile!=null)
                    BackupDuration.Text = ViewModel.SelectedFile.SnapshotIntervalDurationString;
            }
        }

        private async void SettingsEditFullPath_Click(object sender, RoutedEventArgs e) {
            if (_hostWindow == null || ViewModel == null || ViewModel.SelectedFile == null) return;
            var newPath = OpenFileDialogHelper.PickSingleFile(_hostWindow);
            if(newPath != null) {
                if(Path.GetFileName(newPath) != ViewModel.SelectedFile.FileName) {
                    ContentDialog dialog = new() {
                        XamlRoot = this.XamlRoot,
                        Title = "File Name Mismatch",
                        Content = "File names of the new and old files do no match. Do want to continue?",
                        PrimaryButtonText = "Yes",
                        SecondaryButtonText = "No"
                    };

                    ContentDialogResult result = await dialog.ShowAsync();
                    if(result == ContentDialogResult.Primary) {
                        ViewModel.SelectedFile.FullPath = newPath;
                    }
                }
            }
        }

        private void SettingsEditBackupPath_Click(object sender, RoutedEventArgs e) {

        }

        private void SaveBackupDuration_Click(object sender, RoutedEventArgs e) {
            if (ViewModel == null || ViewModel.SelectedFile == null) return;
            string durationString = BackupDuration.Text;
            TimeSpan duration = TimeSpan.Zero;
            try {
                duration = TimeSpanJiraStringConverter.JiraToTimeSpan(durationString);
                BackupDurationError.Visibility = Visibility.Collapsed;
                BackupDuration.ClearValue(TextBox.BorderBrushProperty);

                if (duration < TimeSpan.FromMinutes(1) || duration > TimeSpan.FromDays(365))
                    throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be between 1 minute and 365 days.");
            }
            catch (ArgumentException ex) {
                if (ex is ArgumentOutOfRangeException) BackupDurationError.Text = "Please enter a duration between 1m and 365d.";
                else BackupDurationError.Text = "Invalid format. Please use format like '1d 2h 30m'.";
                BackupDurationError.Visibility = Visibility.Visible;

                if (Application.Current.Resources.TryGetValue("SystemFillColorCriticalBrush", out object errorBrush)) {
                    BackupDuration.BorderBrush = (Microsoft.UI.Xaml.Media.Brush)errorBrush;
                }
                return;
            }
            ViewModel.SelectedFile.SnapshotIntervalDuration = duration;
        }
    }
}