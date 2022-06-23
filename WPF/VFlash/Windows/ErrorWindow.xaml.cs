using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace VFlash {
    public partial class ErrorWindow : Window {
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private ErrorWindow(string msg) {
            InitializeComponent();
            MsgTextBlock.Text = msg;
            this.Loaded += ErrorWindow_Loaded;
        }

        private void ErrorWindow_Loaded(object sender, RoutedEventArgs e) {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            this.Close();
        }

        public static void Show(string msg) {
            ErrorWindow error = new ErrorWindow(msg);
            error.Owner = App.Current.MainWindow;
            error.Show();
        }

        public static void ShowDialog(string msg) {
            ErrorWindow error = new ErrorWindow(msg);
            try {
                error.Owner = App.Current.MainWindow;
            }
            catch {
                error.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            error.ShowDialog();
        }
    }
}
