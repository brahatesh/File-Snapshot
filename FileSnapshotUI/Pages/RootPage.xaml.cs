using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using FileSnapshotUI.Models;
using FileSnapshotUI.ViewModels;
using FileSnapshotUI.Pages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FileSnapshotUI.Pages {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RootPage : Page {

        public RootViewModel ViewModel { get; } = new();
        private MainWindow? _hostWindow;

        public RootPage() {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            if(e.Parameter is MainWindow window) {
                _hostWindow = window;
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e) {
            ViewModel.Files.Add(new FileItem {
                FullPath = "C:\\Users\\braha\\OneDrive\\Desktop\\Flat Payment details.xlsx"
            });
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e) {
            if (FilesListView.SelectedItem is FileItem selected) {
                ViewModel.Files.Remove(selected);
                DeleteButton.IsEnabled = false;
            }
        }

        private void FilesListView_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e) {
            ViewModel.SelectedFile =
                FilesListView.SelectedItem as FileItem;
            DeleteButton.IsEnabled = true;

            // Future:
            // Load selected file details into DetailsPresenter
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e) {
            if(_hostWindow != null) {
                _hostWindow.OpenDrawer(DrawerContent.Settings);
            }
        }

        private void NotificationButton_Click(object sender, RoutedEventArgs e) {
            if(_hostWindow != null) {
                _hostWindow.OpenDrawer(DrawerContent.Notifications);
            }
        }
    }
}
