using FileSnapshotUI.Models;
using FileSnapshotUI.ViewModels;
using System;

namespace FileSnapshotUI.Services;

public class NotificationService {
    private static NotificationService? _instance;
    private NotificationViewModel _viewModel;

    private NotificationService() {
        _viewModel = new NotificationViewModel();
    }

    public static NotificationService Instance => _instance ??= new NotificationService();

    public void AddNotification(FileItem fileItem, string message) {
        _viewModel.AddNotification(fileItem, message);
    }

    public void RemoveNotification(Guid notificationId) {
        _viewModel.RemoveNotification(notificationId);
    }

    public NotificationViewModel ViewModel => _viewModel;
}