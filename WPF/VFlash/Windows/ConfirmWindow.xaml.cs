using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace VFlash {
    public partial class ConfirmWindow : Window {
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private ConfirmWindow(string msg) {
            InitializeComponent();
            ConfirmTextBlock.Text = msg;
            this.Loaded += ErrorWindow_Loaded;
        }

        private void ErrorWindow_Loaded(object sender, RoutedEventArgs e) {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
            OkButton.Focus();
        }

        private void OK_Click(object sender, RoutedEventArgs e) {
            this.DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) {
            this.DialogResult= false;
        }

        public static bool? ShowDialog(string msg) {
            ConfirmWindow confirm = new ConfirmWindow(msg);
            confirm.Owner = App.Current.MainWindow;
            return confirm.ShowDialog();
        }
    }
}
