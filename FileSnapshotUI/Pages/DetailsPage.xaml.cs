using DocumentFormat.OpenXml.Office2013.WebExtension;
using FileSnapshotUI.Helpers;
using FileSnapshotUI.Models;
using FileSnapshotUI.Services;
using FileSnapshotUI.ViewModels;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FileSnapshotUI.Pages {
    /// <summary>
    /// Page showing snapshot details of the selected file
    /// </summary>
    public sealed partial class DetailsPage : Page {
        private readonly NotificationService _notificationService;

        public DetailsPage() {
            InitializeComponent();

            // Get the notification service instance
            _notificationService = App.Services.GetRequiredService<NotificationService>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            // Expect the navigation parameter to be the selected FileItem passed from RootPage.xaml.cs
            if (e.Parameter is FileItem fileItem) {
                DataContext = fileItem;
            }
        }

        private async void RestoreSnapshot_Click(object sender, RoutedEventArgs e) {
            // Verify if snapshot is attached and file is selected
            if (sender is Button btn && btn.CommandParameter is SnapshotDetails snapshot) {
                if (DataContext is FileItem file) {
                    var backupPath = file.BackupPath;

                    // Get required service instances
                    var queue = App.Services.GetRequiredService<BackgroundTaskQueue>();
                    var snapshotService = App.Services.GetRequiredService<SnapshotService>();

                    // Dialog asking if user wants to replace file or create a separate file
                    //ContentDialog confirmDialog = new ContentDialog {
                    //    XamlRoot = this.XamlRoot,
                    //    Title = "Replace current file",
                    //    Content = $"Do you want to replace {file.FileName} with the snapshot taken at time {snapshot.SnapshotTimeString}?",
                    //    PrimaryButtonText = "Yes",
                    //    SecondaryButtonText = "No",
                    //    CloseButtonText = "Cancel"
                    //};
                    //ContentDialogResult result = await confirmDialog.ShowAsync();
                    //if (result == ContentDialogResult.None) return; // Cancel clicked

                    // Create working dir
                    var tempDir = AppEnvironment.GetTempFolder();
                    Directory.CreateDirectory(tempDir);

                    // Lock the UI
                    file.IsProcessing = true;

                    // Add task to queue for background processing
                    queue.EnqueueTask(async (token) => {
                        try {
                            // If user wants to replace file, take snapshot first to save current state. 
                            // Take snapshot in silent mode, no notification.
                            //if (result == ContentDialogResult.Primary) {
                            //    await snapshotService.PerformSnapshotAsync(file, SnapshotMode.Silent, token);
                            //}

                            // Rollback repo and copy old file version to tempDir. Also, restores back to current commit.
                            await RollbackService.RollbackRepo(file, tempDir, snapshot, token);

                            // Set file names and rename to {filename_time}
                            var newFileName = $"{Path.GetFileNameWithoutExtension(file.FullPath)}_{snapshot.SnapshotTime.ToString("yyyy-MM-dd_HH-mm-ss")}{Path.GetExtension(file.FullPath)}";
                            var oldPath = Path.Combine(tempDir, file.FileName);
                            var newPath = Path.Combine(tempDir, newFileName);
                            File.Move(oldPath, newPath);

                            var rootPath = Path.GetDirectoryName(file.FullPath);

                            // Copy old file to root dir of original file
                            await FileOperationsHelper.CopyFileAsync(newPath, rootPath, token);

                            // If user wants to replace, delete and rename file
                            //if(result==ContentDialogResult.Primary) {
                            //    File.Delete(file.FullPath);
                            //    File.Move(Path.Combine(rootPath, newFileName), file.FullPath);
                            //}

                            App.MainDispatcher.TryEnqueue(()=>_notificationService.AddNotification(file, $"File restored successfully to {newFileName}"));
                        }
                        catch(Exception ex) {
                            App.MainDispatcher.TryEnqueue(()=>_notificationService.AddNotification(file, $"Failed to restore backup: {ex.Message}"));
                        }
                        finally {
                            Directory.Delete(tempDir, true);
                        }
                        await default(ValueTask);
                    });

                    file.IsProcessing = false;
                }
            }
        }

        private async void DeleteSnapshot_Click(object sender, RoutedEventArgs e) {
            // Verify if snapshot is attached and file is selected
            if (sender is Button btn && btn.CommandParameter is SnapshotDetails snapshot) {
                if (DataContext is FileItem parentFile) {
                    // Confirm if user wants to delete
                    ContentDialog confirmDialog = new ContentDialog {
                        XamlRoot = this.XamlRoot,
                        Title = "Delete snapshot",
                        Content = $"Are you sure that you want to delete snapshot for file {parentFile.FileName} at time {snapshot.SnapshotTimeString}?",
                        PrimaryButtonText = "Delete",
                        SecondaryButtonText = "Cancel"
                    };
                    ContentDialogResult result = await confirmDialog.ShowAsync();

                    if (result != ContentDialogResult.Primary) return;

                    var stateService = App.Services.GetRequiredService<IStateService>();
                    await stateService.RemoveSnapshotAsync(snapshot);

                    parentFile.Snapshots.Remove(snapshot);
                }
            }
        }
    }
}
