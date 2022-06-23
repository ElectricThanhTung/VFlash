using CanSharp;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using VFlash.ViewModel;

namespace VFlash {
    public partial class ConfigTab : UserControl {
        private class DeviceViewModel {
            private string deviceName;
            private string deviceIcon;

            public DeviceViewModel(string name, string icon) {
                DeviceName = name;
                DeviceIcon = icon;
            }

            public string DeviceName {
                get {
                    return deviceName;
                }
                private set {
                    deviceName = value;
                }
            }

            public string DeviceIcon {
                get {
                    return deviceIcon;
                }
                private set {
                    deviceIcon = value;
                }
            }
        }

        private string deviceOld = "";
        private string channelOld = "";
        private DispatcherTimer timer;
        private ObservableCollection<DeviceViewModel> deviceList;

        public ConfigTab() {
            InitializeComponent();

            deviceList = new ObservableCollection<DeviceViewModel>();
            foreach(string device in DeviceManager.GetDeviceList()) {
                string icon = DeviceManager.GetDeviceIcon(device);
                if(icon == "")
                    icon = "/VFlash;component/Resources/Images/Icons/DeviceIcon.png";
                deviceList.Add(new DeviceViewModel(device, icon));
            }
            DeviceComboBox.ItemsSource = deviceList;

            this.Loaded += ConfigTab_Loaded;

            timer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 100) };
            timer.Tick += ScanChannels;
            timer.Start();

            ChannelComboBox.SelectionChanged += ChannelComboBox_SelectionChanged;
        }

        private void ConfigTab_Loaded(object sender, RoutedEventArgs e) {
            this.Loaded -= ConfigTab_Loaded;
                ConfigViewModel configViewModel = this.DataContext as ConfigViewModel;
            if(configViewModel != null && configViewModel.Device != null) {
                string deviceName = configViewModel.Device.ToLower();
                for(int i = 0; i < deviceList.Count; i++) {
                    if(deviceName == deviceList[i].DeviceName.ToLower()) {
                        DeviceComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void ChannelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if(ChannelComboBox.SelectedItem != null)
                channelOld = ChannelComboBox.SelectedItem.ToString();
        }

        private void OpenSeedKeyDllButton_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Seed key dll|*.dll";
            if(openFileDialog.ShowDialog() == true) {
                SeedKeyDLLTextBox.Text = openFileDialog.FileName;
                SeedKeyDLLTextBox.Focus();
                SeedKeyDLLTextBox.CaretIndex = SeedKeyDLLTextBox.Text.Length;
            }
        }

        private void OpenFlashActionsButton_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Flash Actions|*.xml";
            if(openFileDialog.ShowDialog() == true) {
                FlashActionsPAthTextBox.Text = openFileDialog.FileName;
                FlashActionsPAthTextBox.Focus();
                FlashActionsPAthTextBox.CaretIndex = SeedKeyDLLTextBox.Text.Length;
            }
        }

        private void UpdateChannelList(List<object> list) {
            try {
                ChannelComboBox.SelectionChanged -= ChannelComboBox_SelectionChanged;
                ChannelComboBox.Items.Clear();
                int selectedIndex = 0;
                if(list != null) {
                    for(int i = 0; i < list.Count; i++)
                        ChannelComboBox.Items.Add(list[i]);
                    if(channelOld != "") {
                        for(int i = 0; i < ChannelComboBox.Items.Count; i++) {
                            if(channelOld == ChannelComboBox.Items[i].ToString()) {
                                selectedIndex = i;
                                break;
                            }
                        }
                    }
                    ChannelComboBox.SelectedIndex = selectedIndex;
                }
                ChannelComboBox.SelectionChanged += ChannelComboBox_SelectionChanged;
            }
            catch {

            }
        }

        private void ScanChannels(object sender, EventArgs e) {
            if(deviceList.Count == 0) {
                UpdateChannelList(null);
                return;
            }

            string deviceName = deviceList[DeviceComboBox.SelectedIndex].DeviceName;

            ConfigViewModel configViewModel = (ConfigViewModel)this.DataContext;
            if(configViewModel != null && configViewModel.Device != deviceName)
                configViewModel.Device = deviceName;

            List<string> channels = DeviceManager.GetCanChannels(deviceName);
            if(channels == null)
                UpdateChannelList(null);
            else if(deviceOld != deviceName || channels.Count != ChannelComboBox.Items.Count) {
                deviceOld = deviceName;
                List<object> list = new List<object>();
                foreach(string channel in channels)
                    list.Add(channel);
                UpdateChannelList(list);
            }
        }
    }
}
