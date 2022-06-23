using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VFlash.ViewModel;

namespace VFlash {
    public partial class FileInfoEditor : Window {
        private const int GWL_STYLE = -16;
        private const int WS_MAXIMIZEBOX = 0x10000;
        private const int WS_MINIMIZEBOX = 0x20000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public FileInfoEditor() {
            InitializeComponent();
            this.Loaded += ErrorWindow_Loaded;
        }

        private void ErrorWindow_Loaded(object sender, RoutedEventArgs e) {
            var hwnd = new WindowInteropHelper(this).Handle;
            HidenWindowIcon();
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_MAXIMIZEBOX & ~WS_MINIMIZEBOX);
        }

        private void HidenWindowIcon() {
            int width = 8;
            int height = width;
            int stride = width / 8;
            byte[] pixels = new byte[height * stride];

            List<Color> colors = new List<Color>();
            colors.Add(Colors.Transparent);
            BitmapPalette myPalette = new BitmapPalette(colors);

            BitmapSource image = BitmapSource.Create(width, height, 96, 96, PixelFormats.Indexed1, myPalette, pixels, stride);

            this.Icon = image;
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Program File|*.hex;*.s3;*.s37;*.s19";
            if(openFileDialog.ShowDialog() == true) {
                FilePathTextBox.Text = openFileDialog.FileName;
                FilePathTextBox.Focus();
                FilePathTextBox.CaretIndex = FilePathTextBox.Text.Length;
            }
        }

        private void OpenCrcButton_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Signature/CRC|*.sig;*crc|Self-calculation|*.dll";
            if(openFileDialog.ShowDialog() == true) {
                CrcPathTextBox.Text = openFileDialog.FileName;
                CrcPathTextBox.Focus();
                CrcPathTextBox.CaretIndex = CrcPathTextBox.Text.Length;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) {
            this.DialogResult = false;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e) {
            if(FileNameTextBox.Text.Trim() == "") {
                ErrorWindow.ShowDialog("File name must be not empty!");
                return;
            }
            FileItemViewModel itemViewModel = ((FileItemViewModel)this.DataContext);
            itemViewModel.FileName = FileNameTextBox.Text;
            itemViewModel.FilePath = FilePathTextBox.Text;
            itemViewModel.CrcPath = CrcPathTextBox.Text;
            itemViewModel.IsDriver = IsDriverCheckBox.IsChecked == true;

            this.DialogResult = true;
        }
    }
}
