using FileSnapshotUI.Models;
using FileSnapshotUI.Services;
using FileSnapshotUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace FileSnapshotUI.Pages;

/// <summary>
/// Page showing notifications related to App
/// </summary>
public sealed partial class Notifications : Page {
    private readonly NotificationViewModel _viewModel;
    private MainWindow? _hostWindow;

    public Notifications() {
        InitializeComponent();
        var notificationService = App.Services.GetRequiredService<NotificationService>();
        _viewModel = notificationService.ViewModel;
        this.DataContext = _viewModel;
    }

    private void OnNotificationClicked(object sender, RoutedEventArgs e) {
        if (sender is Button button && button.DataContext is Notification notification) {
            _hostWindow?.SelectFile(notification.FileItem);
        }
    }

    private void OnRemoveNotification(object sender, RoutedEventArgs e) {
        if (sender is Button button && button.DataContext is Notification notification) {
            _viewModel.RemoveNotification(notification.Id);
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e) {
        base.OnNavigatedTo(e);
        if (e.Parameter is MainWindow window) {
            _hostWindow = window;
        }
    }
}
