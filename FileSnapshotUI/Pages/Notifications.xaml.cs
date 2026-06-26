using FileSnapshotUI.Models;
using FileSnapshotUI.Services;
using FileSnapshotUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FileSnapshotUI.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
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

    //public NotificationViewModel ViewModel => _viewModel;

    private void OnNotificationClicked(object sender, RoutedEventArgs e) {
        if (sender is Button button && button.DataContext is Notification notification) {
            //OnNotificationSelected?.Invoke(notification.FileItem);
            _hostWindow?.SelectFile(notification.FileItem);
        }
    }

    private void OnRemoveNotification(object sender, RoutedEventArgs e) {
        if (sender is Button button && button.DataContext is Notification notification) {
            _viewModel.RemoveNotification(notification.Id);
            //_viewModel.RemoveNotification(notification.Id);
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e) {
        base.OnNavigatedTo(e);
        if (e.Parameter is MainWindow window) {
            _hostWindow = window;
        }
    }

    //public event Action<FileItem>? OnNotificationSelected;
}
