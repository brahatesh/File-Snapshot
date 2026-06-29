using FileSnapshotUI.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FileSnapshotUI.ViewModels;

/// <summary>
/// Manages the collection of application notifications and provides methods 
/// for filtering and manipulation via the UI.
/// </summary>
public class NotificationViewModel {
    /// <summary>
    /// Gets the collection of active notifications displayed in the application.
    /// </summary>
    public ObservableCollection<Notification> Notifications { get; } = new();

    /// <summary>
    /// Adds a new notification to the collection.
    /// </summary>
    /// <param name="fileItem">The <see cref="FileItem"/> the notification relates to.</param>
    /// <param name="message">The notification text to display.</param>
    public void AddNotification(FileItem fileItem, string message) {
        var notification = new Notification(fileItem, message);
        Notifications.Add(notification);
    }

    /// <summary>
    /// Removes a notification from the collection by its unique ID.
    /// </summary>
    /// <param name="notificationId">The <see cref="Guid"/> of the notification to remove.</param>
    public void RemoveNotification(Guid notificationId) {
        var notification = Notifications.FirstOrDefault(n => n.Id == notificationId);
        if (notification != null) {
            Notifications.Remove(notification);
        }
    }

    /// <summary>
    /// Clears all notifications from the collection.
    /// </summary>
    public void ClearAll() {
        Notifications.Clear();
    }

    /// <summary>
    /// Retrieves a specific notification by its unique ID.
    /// </summary>
    /// <param name="notificationId">The <see cref="Guid"/> of the notification to find.</param>
    /// <returns>The <see cref="Notification"/> if found; otherwise, <c>null</c>.</returns>
    public Notification? GetNotificationById(Guid notificationId) {
        return Notifications.FirstOrDefault(n => n.Id == notificationId);
    }

    /// <summary>
    /// Filters notifications associated with a specific <see cref="FileItem"/>.
    /// </summary>
    /// <param name="fileItemId">The <see cref="Guid"/> of the file to filter by.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> of matching notifications.</returns>
    public IEnumerable<Notification> GetNotificationsByFileId(Guid fileItemId) {
        return Notifications.Where(n => n.FileItemId == fileItemId);
    }
}
