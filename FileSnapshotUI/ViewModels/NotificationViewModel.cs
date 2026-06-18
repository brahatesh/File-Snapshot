using FileSnapshotUI.Models;
using System.Collections.ObjectModel;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace FileSnapshotUI.ViewModels;

public class NotificationViewModel {
    public ObservableCollection<Notification> Notifications { get; } = new();
    public void AddNotification(FileItem fileItem, string message) {
        var notification = new Notification(fileItem, message);
        Notifications.Add(notification);
    }

    public void RemoveNotification(Guid notificationId) {
        var notification = Notifications.FirstOrDefault(n => n.Id == notificationId);
        if (notification != null) {
            Notifications.Remove(notification);
        }
    }

    public void ClearAll() {
        Notifications.Clear();
    }

    public Notification? GetNotificationById(Guid notificationId) { 
        return Notifications.FirstOrDefault(n=> n.Id == notificationId);
    }

    public IEnumerable<Notification> GetNotificationsByFileId(Guid fileItemId) { 
        return Notifications.Where(n => n.FileItemId == fileItemId);
    }
}
