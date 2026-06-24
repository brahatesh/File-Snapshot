using FileSnapshotUI.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public sealed partial class DetailsPage : Page {
        public DetailsPage() {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            // Expect the navigation parameter to be the selected FileItem
            if (e.Parameter is FileItem fileItem) {
                DataContext = fileItem;
            }
        }

        private void RestoreSnapshot_Click(object sender, RoutedEventArgs e) {
            
        }

        private async void DeleteSnapshot_Click(object sender, RoutedEventArgs e) {
            if(sender is Button btn && btn.CommandParameter is SnapshotDetails snapshot) {
                if (DataContext is FileItem parentFile) {
                    ContentDialog confirmDialog = new ContentDialog {
                        XamlRoot = this.XamlRoot,
                        Title = "Delete snapshot",
                        Content = $"Are you sure that you want to delete snapshot for file {parentFile.FileName} at time {snapshot.SnapshotTimeString}?",
                        PrimaryButtonText = "Delete",
                        SecondaryButtonText = "Cancel"
                    };
                    ContentDialogResult result = await confirmDialog.ShowAsync();

                    if (result != ContentDialogResult.Primary) return;
                    parentFile.Snapshots.Remove(snapshot);
                }
            }
        }
    }
}
