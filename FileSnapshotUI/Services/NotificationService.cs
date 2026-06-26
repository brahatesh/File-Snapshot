using FileSnapshotUI.Models;
using FileSnapshotUI.ViewModels;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;

namespace FileSnapshotUI.Services;

public class NotificationService {
    //private static NotificationService? _instance;
    public NotificationViewModel ViewModel { get; } = new();

    //public static NotificationService Instance => _instance ??= new NotificationService();

    public void AddNotification(FileItem fileItem, string message) {
        ViewModel.AddNotification(fileItem, message);
        SendWindowsToast(fileItem?.FileName, message);
    }

    public void RemoveNotification(Guid notificationId) {
        ViewModel.RemoveNotification(notificationId);
    }

    private static void SendWindowsToast(string? fileName, string message) {
        try {
            string title = string.IsNullOrEmpty(fileName) ? "File Snapshot" : $"Snapshot: {fileName}";

            var toast = new AppNotificationBuilder()
                .AddText(title)
                .AddText(message)
                .BuildNotification();

            AppNotificationManager.Default.Show(toast);
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"Failed to send Windows notification: {ex.Message}");
        }
    }
}