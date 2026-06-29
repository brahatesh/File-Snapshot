using FileSnapshotUI.Models;
using FileSnapshotUI.ViewModels;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;

namespace FileSnapshotUI.Services;

/// <summary>
/// Manages application notifications by updating the UI view model and 
/// triggering Windows system toast notifications.
/// </summary>
public class NotificationService {
    /// <summary>
    /// Gets the <see cref="NotificationViewModel"/> that tracks the collection 
    /// of active notifications within the application.
    /// </summary>
    public NotificationViewModel ViewModel { get; } = new();

    /// <summary>
    /// Adds a new notification to the application's history and displays a 
    /// Windows toast notification to the user.
    /// </summary>
    /// <param name="fileItem">The <see cref="FileItem"/> associated with the event.</param>
    /// <param name="message">The message content to display.</param>
    public void AddNotification(FileItem fileItem, string message) {
        ViewModel.AddNotification(fileItem, message);
        SendWindowsToast(fileItem?.FileName, message);
    }

    /// <summary>
    /// Removes a notification from the application's history by its unique identifier.
    /// </summary>
    /// <param name="notificationId">The <see cref="Guid"/> of the notification to remove.</param>
    public void RemoveNotification(Guid notificationId) {
        ViewModel.RemoveNotification(notificationId);
    }

    /// <summary>
    /// Builds and displays a native Windows App Notification (toast).
    /// </summary>
    private static void SendWindowsToast(string? fileName, string message) {
        try {
            string title = string.IsNullOrEmpty(fileName) ? "File Snapshot" : $"Snapshot: {fileName}";

            string logoPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Assets", "Square44x44Logo.scale-200.png");

            var builder = new AppNotificationBuilder()
                .AddText(title)
                .AddText(message);

            if (System.IO.File.Exists(logoPath)) {
                builder.SetAppLogoOverride(new Uri(logoPath), AppNotificationImageCrop.Default);
            }

            AppNotificationManager.Default.Show(builder.BuildNotification());
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"Failed to send Windows notification: {ex.Message}");
        }
    }
}