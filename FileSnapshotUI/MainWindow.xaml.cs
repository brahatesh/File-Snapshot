using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SystemTray.Core;
using SystemTray.UI;
using FileSnapshotUI.Pages;
using Microsoft.UI.Windowing;
using System.ComponentModel.Design;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;

// -----------------------------------------------------------------------------
// SystemTray for WinUI 3
// A complete system tray (notification area) implementation for WinUI 3 apps.
//
// Repository: https://github.com/MEHDIMYADI
// Author: Mehdi Dimyadi
// License: MIT
// -----------------------------------------------------------------------------

namespace FileSnapshotUI
{
    public enum DrawerContent {
        Settings,
        Notifications
    }
    public partial class MainWindow : Window
    {
        private SystemTrayManager systemTrayManager;
        private bool _isDrawerOpen;

        public MainWindow()
        {
            this.InitializeComponent();

            var helper = new WindowHelper(this);
            systemTrayManager = new SystemTrayManager(helper)
            {
                OpenSettingsAction = () => NavigateToSettings(),
                IsIconVisible = true,
                IconToolTip = "File Snapshot",
                MinimizeToTray = true,
                CloseButtonMinimizesToTray = true,
                LanguageCode = "en-US"
            };

            Closed += (_, _) => systemTrayManager?.Dispose();

            //RightPanelFrame.ContentTransitions = null;

            RootFrame.Navigate(typeof(Pages.RootPage), this);

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
        }

        public void OpenDrawer(DrawerContent content) {
            DrawerRoot.Visibility = Visibility.Visible;
            DrawerRoot.IsHitTestVisible = true;

            Scrim.Opacity = 0;

            switch (content) {
                case DrawerContent.Settings:
                    RightPanelFrame.Navigate(typeof(Pages.SettingsPage), systemTrayManager, new SuppressNavigationTransitionInfo());
                    break;
                case DrawerContent.Notifications:
                    RightPanelFrame.Navigate(typeof(Pages.Notifications), null, new SuppressNavigationTransitionInfo());
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

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                NavigateToSettings();
                return;
            }

            if (args.SelectedItem is NavigationViewItem selectedItem)
            {
                switch (selectedItem.Tag)
                {
                    case "home":
                        RootFrame.Navigate(typeof(HomePage));
                        break;
                }
            }
        }

        public void NavigateToSettings()
        {
            if (systemTrayManager != null)
            {
                RootFrame.Navigate(typeof(SettingsPage), systemTrayManager);
            }
        }

        private void Scrim_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e) => CloseDrawer();

        private void DrawerTabButton_Click(object sender, RoutedEventArgs e) => CloseDrawer();
    }
}
