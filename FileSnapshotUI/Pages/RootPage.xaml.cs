using FileSnapshotUI.Helpers;
using FileSnapshotUI.Models;
using FileSnapshotUI.Services;
using FileSnapshotUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace FileSnapshotUI.Pages {
    /// <summary>
    /// Main root page of the application
    /// </summary>
    public sealed partial class RootPage : Page {

        private RootViewModel RootViewModel = new();
        private MainWindow? _hostWindow;
        private NotifyCollectionChangedEventHandler? _snapshotChangeHandler;

        public RootPage() {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            if (e.Parameter is MainWindow window) {
                _hostWindow = window;
                RootViewModel = window.ViewModel;
            }

            // Set details frame to DetailsPage
            DetailsFrame.Navigate(typeof(DetailsPage), null, new SuppressNavigationTransitionInfo());
        }

        private async unsafe void AddButton_Click(object sender, RoutedEventArgs e) {
            if (_hostWindow == null) return;
            string? selectedFilePath = OpenDialogHelper.PickSingleFile(_hostWindow);

            if (!string.IsNullOrEmpty(selectedFilePath)) {
                RootViewModel.Files.Add(new FileItem(selectedFilePath));
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e) {
            if (FilesListView.SelectedItem is FileItem selected) {
                // Ask for confirmation before deleting
                ContentDialog confirmDialog = new() {
                    XamlRoot = this.XamlRoot,
                    Title = "Remove file",
                    Content = $"Are you sure that you want to remove {selected.FileName}?",
                    PrimaryButtonText = "Delete",
                    SecondaryButtonText = "Cancel"
                };
                ContentDialogResult result = await confirmDialog.ShowAsync();

                if (result == ContentDialogResult.Primary) {
                    var backupPath = selected.BackupPath;
                    RootViewModel.Files.Remove(selected);

                    // Add delete task to queue
                    var queue = App.Services.GetRequiredService<BackgroundTaskQueue>();
                    queue.EnqueueTask(async (token) => {
                        try {
                            IEnumerable<string> trackedFiles = [];
                            IEnumerable<string> trackedDirs = [];
                            if(selected.Snapshots.Count > 0) {
                                trackedFiles = selected.Snapshots.Last().TrackedFiles;
                                trackedDirs = selected.Snapshots.Last().TrackedDirectories;
                            }
                            await FileOperationsHelper.DeleteTrackedFilesAsync(backupPath, trackedFiles, trackedDirs, token, true);
                        }
                        catch (Exception) { }

                        await default(ValueTask);
                    });
                }

            }
        }

        // Whenever new file is selected from list of files, update the UI and SelectedFile
        private void FilesListView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (RootViewModel.SelectedFile?.Snapshots != null && _snapshotChangeHandler != null) {
                RootViewModel.SelectedFile.Snapshots.CollectionChanged -= _snapshotChangeHandler;
                _snapshotChangeHandler = null;
            }

            RootViewModel.SelectedFile = FilesListView.SelectedItem as FileItem;
            LastBackupStringUI.Visibility = RootViewModel.SelectedFile != null ? Visibility.Visible : Visibility.Collapsed;

            if (RootViewModel.SelectedFile?.Snapshots != null) {
                _snapshotChangeHandler = (_, __) => UpdateDetailsFrame();
                RootViewModel.SelectedFile.Snapshots.CollectionChanged += _snapshotChangeHandler;
            }

            UpdateDetailsFrame();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e) {
            _hostWindow?.OpenDrawer(DrawerContent.Settings);
        }

        private void NotificationButton_Click(object sender, RoutedEventArgs e) {
            if (RootViewModel.UnreadCount > 0) {
                RootViewModel.UnreadCount = 0;
            }
            _hostWindow?.OpenDrawer(DrawerContent.Notifications);
        }

        private void UpdateDetailsFrame() {
            var selected = RootViewModel.SelectedFile;
            DetailsFrame.Navigate(typeof(DetailsPage), selected, new SuppressNavigationTransitionInfo());
        }

        // Manual snapshot creation
        private void CreateSnapshot_Click(object sender, RoutedEventArgs e) {
            var selected = RootViewModel.SelectedFile;
            if (selected != null) {
                var snapshotService = App.Services.GetRequiredService<SnapshotService>();
                var queue = App.Services.GetRequiredService<BackgroundTaskQueue>();
                queue.EnqueueTask(async (token) => {
                    await snapshotService.PerformSnapshotAsync(selected, SnapshotMode.Manual, token);
                    await default(ValueTask);
                });
            }
        }

        // Select and item, made for when open is clicked on notification page
        public void SelectFile(FileItem item) {
            FilesListView.SelectedItem = item;
        }
    }
}
