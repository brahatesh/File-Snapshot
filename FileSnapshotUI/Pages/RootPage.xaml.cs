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
//using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FileSnapshotUI.Pages {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
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

            DetailsFrame.Navigate(typeof(EmptyDetailsPage));
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
                    //DeleteButton.IsEnabled = false;
                    //CreateSnapshotButton.IsEnabled = false;

                    var queue = App.Services.GetRequiredService<BackgroundTaskQueue>();
                    queue.EnqueueTask(async (token) => {
                        try {
                            IEnumerable<string> trackedFiles = selected.Snapshots.Last().TrackedFiles;
                            IEnumerable<string> trackedDirs = selected.Snapshots.Last().TrackedDirectories;
                            await FileOperationsHelper.DeleteTrackedFilesAsync(backupPath, trackedFiles, trackedDirs, token, true);
                        }
                        catch (Exception) { }

                        await default(ValueTask);
                    });
                }

            }
        }

        private void FilesListView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (RootViewModel.SelectedFile?.Snapshots != null && _snapshotChangeHandler != null) {
                RootViewModel.SelectedFile.Snapshots.CollectionChanged -= _snapshotChangeHandler;
                _snapshotChangeHandler = null;
            }

            RootViewModel.SelectedFile = FilesListView.SelectedItem as FileItem;
            //DeleteButton.IsEnabled = RootViewModel.SelectedFile != null;
            //CreateSnapshotButton.IsEnabled = RootViewModel.SelectedFile != null;
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
            if (selected != null && selected.Snapshots != null && selected.Snapshots.Count > 0) {
                // Pass the selected FileItem to DetailsPage as parameter
                DetailsFrame.Navigate(typeof(DetailsPage), selected, new SuppressNavigationTransitionInfo());
            }
            else {
                DetailsFrame.Navigate(typeof(EmptyDetailsPage), null, new SuppressNavigationTransitionInfo());
            }
        }

        private void CreateSnapshot_Click(object sender, RoutedEventArgs e) {
            var selected = RootViewModel.SelectedFile;
            if (selected != null) {
                var snapshotService = App.Services.GetRequiredService<SnapshotService>();
                var queue = App.Services.GetRequiredService<BackgroundTaskQueue>();
                queue.EnqueueTask(async (token) => {
                    await snapshotService.PerformSnapshotAsync(selected, token);
                    await default(ValueTask);
                });
                //selected.AddSnapshot();
                //NotificationService.Instance.AddNotification(selected, "Created snapshot");
            }
        }

        public void SelectFile(FileItem item) {
            FilesListView.SelectedItem = item;
        }
    }
}
