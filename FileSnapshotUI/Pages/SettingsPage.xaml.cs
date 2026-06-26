using FileSnapshotUI.Helpers;
using FileSnapshotUI.Services;
using FileSnapshotUI.ViewModels;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SystemTray.Core;

namespace FileSnapshotUI.Pages {
    public sealed partial class SettingsPage : Page {
        public RootViewModel? ViewModel;
        private SystemTrayManager? systemTrayManager;
        private MainWindow? _hostWindow;
        private readonly NotificationService _notificationService;

        public SettingsPage() {
            this.InitializeComponent();
            _notificationService = App.Services.GetRequiredService<NotificationService>();
        }

        public void SetManager(SystemTrayManager manager) {
            systemTrayManager = manager;
        }

        private void TrayIconToggle_Toggled(object sender, RoutedEventArgs e) {
            if (systemTrayManager != null) {
                systemTrayManager.IsIconVisible = TrayIconToggle.IsOn;
                if (TrayIconToggle.IsOn == false) {
                    systemTrayManager.CloseButtonMinimizesToTray = false;
                    CloseButtonTrayToggle.IsOn = false;
                }
                MinimizeToTrayToggle.IsEnabled = TrayIconToggle.IsOn;
                CloseButtonTrayToggle.IsEnabled = TrayIconToggle.IsOn;
            }
        }

        private void MinimizeToTrayToggle_Toggled(object sender, RoutedEventArgs e) {
            if (systemTrayManager != null) {
                systemTrayManager.MinimizeToTray = MinimizeToTrayToggle.IsOn;
            }
        }

        private void CloseToMinimizesInTrayToggle_Toggled(object sender, RoutedEventArgs e) {
            if (systemTrayManager != null) {
                systemTrayManager.CloseButtonMinimizesToTray = CloseButtonTrayToggle.IsOn;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
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
                if (ViewModel != null && ViewModel.SelectedFile != null)
                    BackupDuration.Text = ViewModel.SelectedFile.SnapshotIntervalDurationString;
            }
        }

        private async void SettingsEditFullPath_Click(object sender, RoutedEventArgs e) {
            if (_hostWindow == null || ViewModel == null || ViewModel.SelectedFile == null) return;
            var newPath = OpenDialogHelper.PickSingleFile(_hostWindow);
            if (newPath != null) {
                if (Path.GetExtension(newPath) != Path.GetExtension(ViewModel.SelectedFile.FullPath)) {
                    ContentDialog dialog = new() {
                        XamlRoot = this.XamlRoot,
                        Title = "File type mismatch",
                        Content = $"The provided file type and the current file type are not the same.",
                        CloseButtonText = "Okay"
                    };

                    await dialog.ShowAsync();
                    return;
                }
                if (Path.GetFileName(newPath) != ViewModel.SelectedFile.FileName) {
                    ContentDialog dialog = new() {
                        XamlRoot = this.XamlRoot,
                        Title = "File Name Mismatch",
                        Content = "File names of the new and old files do no match. Do want to continue?",
                        PrimaryButtonText = "Yes",
                        SecondaryButtonText = "No"
                    };

                    ContentDialogResult result = await dialog.ShowAsync();
                    if (result == ContentDialogResult.Primary) {
                        ViewModel.SelectedFile.FullPath = newPath;
                    }
                }
            }
        }

        private async void SettingsEditBackupPath_Click(object sender, RoutedEventArgs e) {
            if (ViewModel == null || ViewModel.SelectedFile == null || _hostWindow == null) return;
            var newDir = OpenDialogHelper.PickSingleFolder(_hostWindow);
            var oldDir = ViewModel.SelectedFile.BackupPath;
            var file = ViewModel.SelectedFile;

            if (newDir != null && newDir != oldDir) {
                if (!FileOperationsHelper.CanReadFromDir(newDir) || !FileOperationsHelper.CanWriteToDir(newDir)) {
                    ContentDialog dialog = new() {
                        XamlRoot = this.XamlRoot,
                        Title = "Cannot access Directory",
                        Content = $"Cannot access {newDir}",
                        CloseButtonText = "Okay"
                    };

                    await dialog.ShowAsync();
                    return;
                }

                if (Repository.IsValid(newDir)) {
                    ContentDialog dialog = new() {
                        XamlRoot = this.XamlRoot,
                        Title = "Directory cannot be used",
                        Content = $"{newDir} is already a git repository. Provide another directory.",
                        CloseButtonText = "Okay"
                    };

                    await dialog.ShowAsync();
                    return;
                }

                file.IsProcessing = true;
                file.BackupPath = newDir;

                var queue = App.Services.GetRequiredService<BackgroundTaskQueue>();
                queue.EnqueueTask(async (token) => {
                    try {
                        IEnumerable<string> trackedFiles = file.Snapshots.Last().TrackedFiles;
                        IEnumerable<string> trackedDirs = file.Snapshots.Last().TrackedDirectories;
                        await FileOperationsHelper.CopyTrackedFilesAsync(oldDir, newDir, trackedFiles, token, true);

                        App.MainDispatcher.TryEnqueue(() => {
                            _notificationService?.AddNotification(file, "Backup moved successfully");
                        });

                        try {
                            await FileOperationsHelper.DeleteTrackedFilesAsync(oldDir, trackedFiles, trackedDirs, CancellationToken.None, true);
                        }
                        catch (Exception) { }
                    }
                    catch (OperationCanceledException) {
                        App.MainDispatcher.TryEnqueue(() => {
                            file.BackupPath = oldDir;
                        });
                    }
                    catch (Exception ex) {
                        App.MainDispatcher.TryEnqueue(() => {
                            file.BackupPath = oldDir;
                            _notificationService.AddNotification(file, $"Unable to change backup directory: {ex.Message}");
                        });
                    }
                    finally {
                        App.MainDispatcher.TryEnqueue(() => {
                            file.IsProcessing = false;
                        });
                    }

                    await default(ValueTask);
                });
            }
        }

        private void SaveBackupDuration_Click(object sender, RoutedEventArgs e) {
            if (ViewModel == null || ViewModel.SelectedFile == null) return;
            string durationString = BackupDuration.Text;
            TimeSpan duration;
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