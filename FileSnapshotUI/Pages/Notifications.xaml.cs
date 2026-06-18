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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FileSnapshotUI.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class Notifications : Page
{
    private NotificationViewModel _viewModel;
    public Notifications()
    {
        InitializeComponent();
        _viewModel = new NotificationViewModel();
        this.DataContext = _viewModel;
        _viewModel.AddNotification(new FileItem { FullPath = "C:\\dummy.xlsx" }, "test");
    }

    public NotificationViewModel ViewModel => _viewModel;

    private void OnNotificationClicked(object sender, RoutedEventArgs e) {
        if(sender is Button button && button.DataContext is Notification notification) {
            OnNotificationSelected?.Invoke(notification.FileItem);
        }
    }

    private void OnRemoveNotification(object sender, RoutedEventArgs e) { 
        if(sender is Button button && button.DataContext is Notification notification) {
            _viewModel.RemoveNotification(notification.Id);
        }
    }

    public event Action<FileItem>? OnNotificationSelected;
}
