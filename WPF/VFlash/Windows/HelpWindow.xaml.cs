using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace VFlash {
    public partial class HelpWindow : Window {
        public HelpWindow() {
            InitializeComponent();
            this.Loaded += HelpWindow_Loaded;
        }

        private async void HelpWindow_Loaded(object sender, RoutedEventArgs e) {
            string LocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appName = Assembly.GetExecutingAssembly().GetName().Name;
            string webViewDataPath = Path.Combine(LocalAppData, appName, "HelpWebView2Data");
            CoreWebView2Environment cwv2Environment = await CoreWebView2Environment.CreateAsync(null, webViewDataPath, new CoreWebView2EnvironmentOptions());
            await WebBrowserPanel.EnsureCoreWebView2Async(cwv2Environment);

            string exePath = Assembly.GetEntryAssembly().Location;
            string exeFolder = Path.GetDirectoryName(exePath);
            string homePagePath = Path.Combine(exeFolder, "documents", "index.html");
            if(File.Exists(homePagePath))
                WebBrowserPanel.Source = new Uri(homePagePath);

            await WebBrowserPanel.EnsureCoreWebView2Async();
            WebBrowserPanel.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
        }

        private void CoreWebView2_NewWindowRequested(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NewWindowRequestedEventArgs e) {
            e.Handled = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            WebBrowserPanel.Dispose();
        }
    }
}
