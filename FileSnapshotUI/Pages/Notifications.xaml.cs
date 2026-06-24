using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using FileSnapshotUI.ViewModels;
using FileSnapshotUI.Models;
using FileSnapshotUI.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FileSnapshotUI.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class Notifications : Page
{
    private NotificationViewModel _viewModel;
    private MainWindow? _hostWindow;
    public Notifications()
    {
        InitializeComponent();
        _viewModel = NotificationService.Instance.ViewModel;
        this.DataContext = _viewModel;
    }

    //public NotificationViewModel ViewModel => _viewModel;

    private void OnNotificationClicked(object sender, RoutedEventArgs e) {
        if(sender is Button button && button.DataContext is Notification notification) {
            //OnNotificationSelected?.Invoke(notification.FileItem);
            _hostWindow?.SelectFile(notification.FileItem);
        }
    }

    private void OnRemoveNotification(object sender, RoutedEventArgs e) { 
        if(sender is Button button && button.DataContext is Notification notification) {
            NotificationService.Instance.RemoveNotification(notification.Id);
            //_viewModel.RemoveNotification(notification.Id);
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e) {
        base.OnNavigatedTo(e);
        if(e.Parameter is MainWindow window) {
            _hostWindow = window;
        }
    }

    //public event Action<FileItem>? OnNotificationSelected;
}
