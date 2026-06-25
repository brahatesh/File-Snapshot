using FileSnapshotUI.Models;
using FileSnapshotUI.Pages;
using FileSnapshotUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Windows.AppNotifications;
using System;
using System.ComponentModel.Design;
using SystemTray.Core;
using SystemTray.UI;
using Windows.UI.ViewManagement;


namespace FileSnapshotUI
{
    public enum DrawerContent {
        Settings,
        Notifications
    }
    public partial class MainWindow : Window
    {
        public RootViewModel ViewModel { get; } = App.Services.GetRequiredService<RootViewModel>();
        public SystemTrayManager systemTrayManager;
        private bool _isDrawerOpen;
        private readonly UISettings _uiSettings;

        public MainWindow()
        {
            var helper = new WindowHelper(this) { IsForceHidden = true };
            
            this.InitializeComponent();
            
            systemTrayManager = new SystemTrayManager(helper)
            {
                IsIconVisible = true,
                IconToolTip = "File Snapshot",
                MinimizeToTray = true,
                CloseButtonMinimizesToTray = true,
                LanguageCode = "en-US"
            };
            Closed += (_, _) => systemTrayManager?.Dispose();
            Closed += (sender, args) => AppNotificationManager.Default.Unregister();

            _uiSettings = new UISettings();
            _uiSettings.ColorValuesChanged += UiSettings_ColorValuesChanged;    

            RootFrame.Navigate(typeof(Pages.RootPage), this);

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            var initialBgColor = _uiSettings.GetColorValue(UIColorType.Background);
            bool initialIsDarkMode = initialBgColor == Colors.Black;
            foreach (var file in ViewModel.Files) {
                file.UpdateTheme(initialIsDarkMode);
            }
        }

        public void OpenDrawer(DrawerContent content) {
            DrawerRoot.Visibility = Visibility.Visible;
            DrawerRoot.IsHitTestVisible = true;

            Scrim.Opacity = 0;

            switch (content) {
                case DrawerContent.Settings:
                    RightPanelFrame.Navigate(typeof(Pages.SettingsPage), this, new SuppressNavigationTransitionInfo());
                    break;
                case DrawerContent.Notifications:
                    RightPanelFrame.Navigate(typeof(Pages.Notifications), this, new SuppressNavigationTransitionInfo());
                    break;
            }

            RightPanelGrid.UpdateLayout();
            double panelWidth = RightPanelGrid.ActualWidth;

            var openPanelAnim = (DoubleAnimation)OpenDrawerStoryboard.Children[0];
            openPanelAnim.From = panelWidth;
            openPanelAnim.To = 0;

            var openTabAnim = (DoubleAnimation)OpenDrawerStoryboard.Children[1];
            openTabAnim.From = panelWidth;
            openTabAnim.To = 0;

            var scrimFadeIn = (DoubleAnimation)OpenDrawerStoryboard.Children[2];
            scrimFadeIn.From = 0;
            scrimFadeIn.To = 1;

            CloseDrawerStoryboard.Stop();
            OpenDrawerStoryboard.Begin();

            _isDrawerOpen = true;
        }

        public void CloseDrawer() {
            if (!_isDrawerOpen) return;

            double panelWidth = RightPanelGrid.ActualWidth;

            var closePanelAnim = (DoubleAnimation)CloseDrawerStoryboard.Children[0];
            closePanelAnim.From = 0;
            closePanelAnim.To = panelWidth;

            var closeTabAnim = (DoubleAnimation)CloseDrawerStoryboard.Children[1];
            closeTabAnim.From = 0;
            closeTabAnim.To = panelWidth;

            var scrimFadeOut = (DoubleAnimation)CloseDrawerStoryboard.Children[2];
            scrimFadeOut.From = 1;
            scrimFadeOut.To = 0;

            OpenDrawerStoryboard.Stop();
            CloseDrawerStoryboard.Completed += CloseDrawerStoryboard_Completed;
            CloseDrawerStoryboard.Begin();

            _isDrawerOpen = false;
        }

        private void CloseDrawerStoryboard_Completed(object? sender, object e) {
            CloseDrawerStoryboard.Completed -= CloseDrawerStoryboard_Completed;
            DrawerRoot.Visibility = Visibility.Collapsed;
            DrawerRoot.IsHitTestVisible = false;

            RightPanelTransform.X = 0;
            TabTransform.X = 0;
            Scrim.Opacity = 0;
        }

        private void Scrim_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e) => CloseDrawer();

        private void DrawerTabButton_Click(object sender, RoutedEventArgs e) => CloseDrawer();

        private void UiSettings_ColorValuesChanged(UISettings sender, object args) {
            DispatcherQueue.TryEnqueue(() => {
                var bgColor = sender.GetColorValue(UIColorType.Background);
                bool isDarkMode = bgColor == Colors.Black;

                foreach (var file in ViewModel.Files) {
                    file.UpdateTheme(isDarkMode);
                }
            });
        }

        public void SelectFile(FileItem item) {
            CloseDrawer();

            if(RootFrame.Content is RootPage rootPage) {
                rootPage.SelectFile(item);
            }
        }
    }
}
