using FileSnapshotUI.Models;
using FileSnapshotUI.ViewModels;
using System;

namespace FileSnapshotUI.Services;

public class NotificationService {
    //private static NotificationService? _instance;
    public NotificationViewModel ViewModel { get; } = new();

    //public static NotificationService Instance => _instance ??= new NotificationService();

    public void AddNotification(FileItem fileItem, string message) {
        ViewModel.AddNotification(fileItem, message);
    }

    public void RemoveNotification(Guid notificationId) {
        ViewModel.RemoveNotification(notificationId);
    }
}