using FileSnapshotUI.Models;
using FileSnapshotUI.Pages;
using FileSnapshotUI.ViewModels;
using FileSnapshotUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using FileSnapshotUI.Services;
using System.Security;
//using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FileSnapshotUI.Pages {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RootPage : Page {

        private RootViewModel RootViewModel = new();
        private MainWindow? _hostWindow;
        private NotifyCollectionChangedEventHandler? _snapshotChangeHandler;

        public RootPage() {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            if(e.Parameter is MainWindow window) {
                _hostWindow = window;
                RootViewModel = window.ViewModel;
            }

            DetailsFrame.Navigate(typeof(EmptyDetailsPage));
        }

        private async unsafe void AddButton_Click(object sender, RoutedEventArgs e) {
            if (_hostWindow == null) return;
            string? selectedFilePath = OpenFileDialogHelper.PickSingleFile(_hostWindow);

            if(!string.IsNullOrEmpty(selectedFilePath)) {
                RootViewModel.Files.Add(new FileItem(selectedFilePath));
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e) {
            if (FilesListView.SelectedItem is FileItem selected) {
                ContentDialog confirmDialog = new ContentDialog {
                    XamlRoot = this.XamlRoot,
                    Title = "Remove file",
                    Content = $"Are you sure that you want to remove {selected.FileName}?",
                    PrimaryButtonText = "Delete",
                    SecondaryButtonText = "Cancel"
                };
                ContentDialogResult result = await confirmDialog.ShowAsync();
                if(result == ContentDialogResult.Primary) {
                    RootViewModel.Files.Remove(selected);
                    DeleteButton.IsEnabled = false;
                    CreateSnapshotButton.IsEnabled = false;
                }
            }
        }

        private void FilesListView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if(RootViewModel.SelectedFile?.Snapshots != null && _snapshotChangeHandler != null) {
                RootViewModel.SelectedFile.Snapshots.CollectionChanged -= _snapshotChangeHandler;
                _snapshotChangeHandler = null;
            }

            RootViewModel.SelectedFile = FilesListView.SelectedItem as FileItem;
            DeleteButton.IsEnabled = RootViewModel.SelectedFile != null;
            CreateSnapshotButton.IsEnabled = RootViewModel.SelectedFile != null;
            LastBackupStringUI.Visibility = RootViewModel.SelectedFile != null ? Visibility.Visible : Visibility.Collapsed;

            if (RootViewModel.SelectedFile?.Snapshots != null) {
                _snapshotChangeHandler = (_, __) => UpdateDetailsFrame();
                RootViewModel.SelectedFile.Snapshots.CollectionChanged += _snapshotChangeHandler;
            }

            UpdateDetailsFrame();
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

        private void UpdateDetailsFrame() {
            var selected = RootViewModel.SelectedFile;
            if (selected != null && selected.Snapshots != null && selected.Snapshots.Count > 0) {
                // Pass the selected FileItem to DetailsPage as parameter
                DetailsFrame.Navigate(typeof(DetailsPage), selected, new SuppressNavigationTransitionInfo());
            }
            else {
                DetailsFrame.Navigate(typeof(EmptyDetailsPage), null, new SuppressNavigationTransitionInfo());
            }
        }

        private void CreateSnapshot_Click(object sender, RoutedEventArgs e) {
            var selected = RootViewModel.SelectedFile;
            if (selected != null) {
                selected.AddSnapshot();
                NotificationService.Instance.AddNotification(selected, "Created snapshot");
            }
        }
    }
}
