using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace VFlash {
    public partial class RenameWindow : Window {
        private const int GWL_STYLE = -16;
        private const int WS_MAXIMIZEBOX = 0x10000;
        private const int WS_MINIMIZEBOX = 0x20000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private string nameStr;

        public RenameWindow() {
            InitializeComponent();
            this.Loaded += ErrorWindow_Loaded;
        }

        private void ErrorWindow_Loaded(object sender, RoutedEventArgs e) {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_MAXIMIZEBOX & ~WS_MINIMIZEBOX);
        }

        public string NameString {
            get {
                return nameStr;
            }
            set {
                nameStr = value;
                NameTextBox.Text = value;
            }
        }

        private void NameInput_KeyDown(object sender, KeyEventArgs e) {
            if(e.Key == Key.Enter)
                OkButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        private void OK_Click(object sender, RoutedEventArgs e) {
            if(NameTextBox.Text.Trim() == "") {
                ErrorWindow.ShowDialog("Name must be not empty!");
                return;
            }
            nameStr = NameTextBox.Text.Trim();
            this.DialogResult = true;
        }
    }
}
