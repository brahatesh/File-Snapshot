using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using FileSnapshotUI.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using FileSnapshotUI.Services;

namespace FileSnapshotUI
{
    public partial class App : Application
    {
        private static WindowHelper? windowHelper;

        [STAThread]
        public static void Main(string[] args)
        {
            Application.Start((p) =>
            {
                var app = new App();
            });
        }

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            var window = new MainWindow();
            window.Activate();

            windowHelper = new WindowHelper(window);
            windowHelper.SetWindowMinMaxSize(new WindowHelper.POINT() { x = 700, y = 500 });
        }
    }
}

