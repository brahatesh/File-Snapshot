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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FileSnapshotUI.Pages {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 
    public sealed partial class DetailsPage : Page {
        private readonly NotificationService _notificationService;

        public DetailsPage() {
            InitializeComponent();
            _notificationService = App.Services.GetRequiredService<NotificationService>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            // Expect the navigation parameter to be the selected FileItem
            if (e.Parameter is FileItem fileItem) {
                DataContext = fileItem;
            }
        }

        private async void RestoreSnapshot_Click(object sender, RoutedEventArgs e) {
            if (sender is Button btn && btn.CommandParameter is SnapshotDetails snapshot) {
                if (DataContext is FileItem file) {
                    var backupPath = file.BackupPath;

                    var queue = App.Services.GetRequiredService<BackgroundTaskQueue>();
                    var snapshotService = App.Services.GetRequiredService<SnapshotService>();

                    ContentDialog confirmDialog = new ContentDialog {
                        XamlRoot = this.XamlRoot,
                        Title = "Replace current file",
                        Content = $"Do you want to replace {file.FileName} with the snapshot taken at time {snapshot.SnapshotTimeString}?",
                        PrimaryButtonText = "Yes",
                        SecondaryButtonText = "No",
                        CloseButtonText = "Cancel"
                    };
                    ContentDialogResult result = await confirmDialog.ShowAsync();
                    if (result == ContentDialogResult.None) return;

                    var tempDir = AppEnvironment.GetTempFolder();
                    Directory.CreateDirectory(tempDir);

                    file.IsProcessing = true;

                    queue.EnqueueTask(async (token) => {
                        try {
                            if (result == ContentDialogResult.Primary) {
                                await snapshotService.PerformSnapshotAsync(file, SnapshotMode.Silent, token);
                            }

                            await RollbackService.RollbackRepo(file, tempDir, snapshot, token);

                            var newFileName = $"{Path.GetFileNameWithoutExtension(file.FullPath)}_{snapshot.SnapshotTime.ToString("yyyy-MM-dd_HH-mm-ss")}{Path.GetExtension(file.FullPath)}";
                            var oldPath = Path.Combine(tempDir, file.FileName);
                            var newPath = Path.Combine(tempDir, newFileName);
                            File.Move(oldPath, newPath);

                            var rootPath = Path.GetDirectoryName(file.FullPath);

                            await FileOperationsHelper.CopyFileAsync(newPath, rootPath, token);

                            if(result==ContentDialogResult.Primary) {
                                File.Delete(file.FullPath);
                                File.Move(Path.Combine(rootPath, newFileName), file.FullPath);
                            }

                            App.MainDispatcher.TryEnqueue(()=>_notificationService.AddNotification(file, $"File restored successfully to {(result==ContentDialogResult.Primary?file.FileName:newFileName)}"));
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
            if (sender is Button btn && btn.CommandParameter is SnapshotDetails snapshot) {
                if (DataContext is FileItem parentFile) {
                    ContentDialog confirmDialog = new ContentDialog {
                        XamlRoot = this.XamlRoot,
                        Title = "Delete snapshot",
                        Content = $"Are you sure that you want to delete snapshot for file {parentFile.FileName} at time {snapshot.SnapshotTimeString}?",
                        PrimaryButtonText = "Delete",
                        SecondaryButtonText = "Cancel"
                    };
                    ContentDialogResult result = await confirmDialog.ShowAsync();

                    if (result != ContentDialogResult.Primary) return;
                    parentFile.Snapshots.Remove(snapshot);
                }
            }
        }
    }
}
