using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace VFlash {
    public partial class InfoWindow : Window {
        private const int GWL_STYLE = -16;
        private const int WS_MAXIMIZEBOX = 0x10000;
        private const int WS_MINIMIZEBOX = 0x20000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public InfoWindow() {
            InitializeComponent();

            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            VersionTextBlock.Text = "VFlash Beta v" + fvi.FileVersion;

            this.Loaded += ErrorWindow_Loaded;
        }

        private void ErrorWindow_Loaded(object sender, RoutedEventArgs e) {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_MAXIMIZEBOX & ~WS_MINIMIZEBOX);
        }

        private void Close_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }
    }
}
