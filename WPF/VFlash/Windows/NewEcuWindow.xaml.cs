using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using VFlash.ViewModel;

namespace VFlash {
    public partial class NewEcuWindow : Window {
        private const int GWL_STYLE = -16;
        private const int WS_MAXIMIZEBOX = 0x10000;
        private const int WS_MINIMIZEBOX = 0x20000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private ObservableCollection<EcuViewModel> ecuRefs;

        public NewEcuWindow(ObservableCollection<EcuViewModel> ecuRefs) {
            InitializeComponent();
            this.ecuRefs = ecuRefs;
            EcuRefComboBox.Items.Add(new ComboBoxItem() { Content = "None", Height = 22, VerticalAlignment = VerticalAlignment.Center });
            foreach(EcuViewModel ecu in ecuRefs) {
                ComboBoxItem comboBoxItem = new ComboBoxItem() {
                    Content = ecu.Name,
                    Height = 22,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(Colors.DarkGreen)
                };
                EcuRefComboBox.Items.Add(comboBoxItem);
            }
            EcuRefComboBox.SelectedIndex = 0;
            this.Loaded += ErrorWindow_Loaded;
        }

        private void ErrorWindow_Loaded(object sender, RoutedEventArgs e) {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_MAXIMIZEBOX & ~WS_MINIMIZEBOX);
        }

        private ObservableCollection<FileItemViewModel> CopyFileList(ObservableCollection<FileItemViewModel> files) {
            ObservableCollection<FileItemViewModel> ret = new ObservableCollection<FileItemViewModel>();
            foreach(FileItemViewModel file in files) {
                FileItemViewModel newFile = new FileItemViewModel() {
                    IsUsed = file.IsUsed,
                    IsDriver = file.IsDriver,
                    FileName = file.FileName,
                    FilePath = file.FilePath,
                    CrcPath = file.CrcPath,
                };
                ret.Add(newFile);
            }
            return ret;
        }

        private ConfigViewModel CopyConfig(ConfigViewModel config) {
            return new ConfigViewModel() {
                Device = config.Device,
                Channel = config.Channel,
                CanType = config.CanType,
                Bitrate = config.Bitrate,
                TxId = config.TxId,
                RxId = config.RxId,
                FunctionalId = config.FunctionalId,
                STmin = config.STmin,
                P2Value = config.P2Value,
                Timeout = config.Timeout,
                SecurityLevel = config.SecurityLevel,
                SeedKeyDll = config.SeedKeyDll,
            };
        }

        public EcuViewModel Value {
            get {
                if(EcuRefComboBox.SelectedIndex < 1)
                    return new EcuViewModel() { Name = NameTextBox.Text.Trim() };

                EcuViewModel ecuRef = ecuRefs[EcuRefComboBox.SelectedIndex - 1];
                return new EcuViewModel() {
                    Name = NameTextBox.Text.Trim(),
                    Files = CopyFileList(ecuRef.Files),
                    Config = CopyConfig(ecuRef.Config)
                };
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
            this.DialogResult = true;
        }
    }
}
