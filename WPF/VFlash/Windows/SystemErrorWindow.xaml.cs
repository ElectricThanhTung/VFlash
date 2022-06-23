using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace VFlash {
    public partial class SystemErrorWindow : Window {
        private const int GWL_STYLE = -16;
        private const int WS_MAXIMIZEBOX = 0x10000;
        private const int WS_MINIMIZEBOX = 0x20000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private SystemErrorWindow(Exception ex) {
            InitializeComponent();
            this.Loaded += ErrorWindow_Loaded;
            MsgTextBlock.Text = ex.Message;
            Exception innerEx = ex.InnerException;
            if(innerEx != null) {
                while(innerEx.InnerException != null)
                    innerEx = innerEx.InnerException;
                MsgTextBlock.Text += "\r\n" + "Inner Exception: " + innerEx.Message;
            }
            MethodInfoTextBlock.Text = GetExecutingMethodName(ex);
        }

        private void ErrorWindow_Loaded(object sender, RoutedEventArgs e) {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_MAXIMIZEBOX & ~WS_MINIMIZEBOX);
        }

        private string GetExecutingMethodName(Exception exception) {
            var trace = new StackTrace(exception);
            var frame = trace.GetFrame(0);
            var method = frame.GetMethod();
            return string.Concat(method.DeclaringType.FullName, ".", method.Name);
        }

        public static void ShowDialog(Exception ex) {
            SystemErrorWindow systemErrorWindow = new SystemErrorWindow(ex);
            systemErrorWindow.ShowDialog();
        }

        private void Close_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }
    }
}
