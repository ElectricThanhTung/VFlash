using CanSharp;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using VFlash.ViewModel;
using VFlashFiles;

namespace VFlash {
    public partial class TraceTab : UserControl {
        public TraceTab() {
            InitializeComponent();
            UDS.AnyRequestSentFailed += RequestSentFailedHandler;
            UDS.AnyRequestStartSent += RequestStartSentHandler;
            UDS.AnyResponseReceived += ResponseReceivedHandler;
            this.Loaded += (o, e) => {
                if(EcuTargetComboBox.Items.Count > 0)
                    EcuTargetComboBox.SelectedIndex = 0;
            };
        }

        private int GetTxId(UDS uds) {
            IDevice device = uds.Device;
            if(device.GetType() == typeof(CanTp))
                return ((CanTp)device).TxId;
            throw new Exception("Can't get TxId form " + typeof(CanTp).Name);
        }

        private int GetRxId(UDS uds) {
            IDevice device = uds.Device;
            if(device.GetType() == typeof(CanTp))
                return ((CanTp)device).RxId;
            throw new Exception("Can't get TxId form " + typeof(CanTp).Name);
        }

        private void RequestStartSentHandler(UDS sender, byte[] data) {
            TraceViewModel msg = new TraceViewModel() {
                TimeStamp = MainWindowViewModel.TimeStamp,
                TxId = GetTxId(sender),
                RxId = GetRxId(sender),
                RequestData = (byte[])data.Clone(),
                ResponseData = null,
                SendSuccessfull = true,
            };
            Dispatcher.BeginInvoke(new Action(() => {
                MainWindowViewModel viewModel = (MainWindowViewModel)this.DataContext;
                viewModel.Trace.Add(msg);
                if(viewModel.TraceShow.Count >= 200)
                    viewModel.TraceShow.RemoveAt(0);
                viewModel.TraceShow.Add(msg);
            }));
        }

        private void RequestSentFailedHandler(UDS sender, byte[] data) {
            int txId = GetTxId(sender);
            int rxId = GetRxId(sender);
            Dispatcher.BeginInvoke(new Action(() => {
                MainWindowViewModel viewModel = (MainWindowViewModel)this.DataContext;
                ObservableCollection<TraceViewModel> trace = viewModel.Trace;
                for(int i = trace.Count - 1; i >= 0; i--) {
                    TraceViewModel traceItem = trace[i];
                    if(traceItem.TxId == txId && traceItem.RxId == rxId && traceItem.RequestData == data) {
                        traceItem.SendSuccessfull = false;
                        return;
                    }
                }
            }));
        }

        private void ResponseReceivedHandler(UDS sender, byte[] data) {
            int rxId = GetRxId(sender);
            Dispatcher.BeginInvoke(new Action(() => {
                MainWindowViewModel viewModel = (MainWindowViewModel)this.DataContext;
                ObservableCollection<TraceViewModel> trace = viewModel.Trace;
                for(int i = trace.Count - 1; i >= 0; i--) {
                    if(trace[i].RxId == rxId) {
                        if((data[0] == 0x7F && trace[i].RequestData[0] == data[1]) || (data[0] == (trace[i].RequestData[0] | 0x40))) {
                            trace[i].ResponseData = data;
                            trace[i].DeltaTime = MainWindowViewModel.TimeStamp - trace[i].TimeStamp;
                            return;
                        }
                    }
                }
            }));
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e) {
            MainWindowViewModel viewModel = (MainWindowViewModel)this.DataContext;
            if(viewModel.IsBusy) {
                ErrorWindow.ShowDialog("Can't export file while flashing!");
                return;
            }

            ObservableCollection<TraceViewModel> trace = viewModel.Trace;
            if(trace == null || trace.Count == 0) {
                ErrorWindow.ShowDialog("Nothing to export!");
                return;
            }
            SaveFileDialog saveFile = new SaveFileDialog();
            saveFile.DefaultExt = "xlsx";
            saveFile.FileName = "VFlash_Trace.xlsx";
            saveFile.Filter = "Xlsx File|*.xlsx|CSV File|*.csv";
            if(saveFile.ShowDialog() == true) {
                if(viewModel.Trace.Count <= 100) {
                    TraceWriter writer = new TraceWriter(saveFile.FileName);
                    writer.Save(viewModel.Trace);
                }
                else {
                    WaitingWindow.Start("Saving export file", () => {
                        TraceWriter writer = new TraceWriter(saveFile.FileName);
                        writer.Save(viewModel.Trace);
                    });
                }
            }
        }

        private void TraceToggleButton_Checked(object sender, RoutedEventArgs e) {
            ((ToggleButton)sender).BorderThickness = new Thickness(1);
        }

        private void TraceToggleButton_Unchecked(object sender, RoutedEventArgs e) {
            ((ToggleButton)sender).BorderThickness = new Thickness(0);
        }

        private bool IsHex(char c) {
            if(('0' <= c && c <= '9') || ('a' <= c && c <= 'f') || ('A' <= c && c <= 'F'))
                return true;
            return false;
        }

        private void MsgTextBox_KeyDown(object sender, KeyEventArgs e) {
            if(e.Key == Key.Enter)
                SendButton_Click(SendButton, null);
        }

        private void MsgTextBox_TextChanged(object sender, TextChangedEventArgs e) {
            TextBox textBox = (TextBox)sender;
            int caretIndex = textBox.CaretIndex;
            string text = textBox.Text.TrimStart();
            if(text == null || text == "")
                return;

            textBox.TextChanged -= MsgTextBox_TextChanged;

            text = text.Replace("0x", "");
            text = text.Replace(',', ' ');
            StringBuilder strBuilder = new StringBuilder();
            bool space = false;
            int hexCount = 0;
            for(int i = 0; i < text.Length; i++) {
                if(text[i] == ' ') {
                    if(!space) {
                        strBuilder.Append(text[i]);
                        space = true;
                    }
                    else if(caretIndex > i)
                        caretIndex--;
                    hexCount = 0;
                }
                else {
                    space = false;
                    if(!IsHex(text[i])) {
                        if(caretIndex > i)
                            caretIndex--;
                    }
                    else {
                        strBuilder.Append(text[i]);
                        if(++hexCount == 2) {
                            if((i + 1) < text.Length) {
                                strBuilder.Append(' ');
                                space = true;
                                if(caretIndex > i)
                                    caretIndex++;
                            }
                        }
                    }
                }
            }
            textBox.Text = strBuilder.ToString();
            textBox.CaretIndex = caretIndex;
            textBox.TextChanged += MsgTextBox_TextChanged;
        }

        private void ClearMsgButton_Click(object sender, RoutedEventArgs e) {
            MsgTextBox.Text = "";
            MsgTextBox.Focus();
        }

        private byte[] GetArrayFormString(string s) {
            List<byte> ret = new List<byte>();
            string[] hexValues = s.Split(' ');
            foreach(string hex in hexValues)
                ret.Add((byte)Number.HexToInt(hex));
            return ret.ToArray();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e) {
            string text = MsgTextBox.Text.Trim();
            if(text == "" || EcuTargetComboBox.SelectedIndex < 0)
                return;
            MainWindowViewModel viewModel = (MainWindowViewModel)this.DataContext;
            EcuViewModel ecu = (EcuViewModel)EcuTargetComboBox.SelectedItem;

            Thread thread = new Thread(() => {
                try {
                    ConfigViewModel cfg = ecu.Config;
                    if(cfg.Channel != null && cfg.Channel.Trim() != "") {
                        viewModel.IsBusy = true;
                        object content = null;
                        this.Dispatcher.Invoke(() => {
                            content = SendButton.Content;
                            SendButton.Content = new LoadingIcon();
                        });
                        byte[] data = GetArrayFormString(text);
                        CanDevice canDevice = DeviceManager.GetCanInstance(cfg.Device, cfg.Channel, cfg.CanType, cfg.Bitrate);
                        UDS ecuUDS = new UDS(new CanTp(canDevice, cfg.TxId, cfg.RxId) { STmin = cfg.STmin });
                        ecuUDS.Connect();
                        ecuUDS.SendRequest(data, cfg.Timeout);
                        ecuUDS.Disconnect();
                        this.Dispatcher.Invoke(() => {
                            ((LoadingIcon)SendButton.Content).Dispose();
                            SendButton.Content = content;
                        });
                        viewModel.IsBusy = false;
                    }
                }
                catch(Exception ex) {
                    Dispatcher.BeginInvoke(new Action(() => throw ex));
                }
            }) {
                IsBackground = true
            };
            thread.Start();
        }

        private void CopyRequest_Click(object sender, RoutedEventArgs e) {
            TraceViewModel traceItem = (TraceViewModel)((MenuItem)sender).DataContext;
            Clipboard.SetText(traceItem.RequestDataString);
        }

        private void CopyResponse_Click(object sender, RoutedEventArgs e) {
            TraceViewModel traceItem = (TraceViewModel)((MenuItem)sender).DataContext;
            Clipboard.SetText(traceItem.ResponseDataString);
        }
    }
}
