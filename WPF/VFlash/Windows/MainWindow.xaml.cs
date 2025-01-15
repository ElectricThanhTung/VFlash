using CanSharp;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using VFlash.Flashing;
using VFlash.ViewModel;
using VFlashFiles;

namespace VFlash {
    public partial class MainWindow : Window {
        private MainWindowViewModel viewModel;
        private Thread flashThread;

        public MainWindow(string cfgPath) {
            InitializeComponent();
            viewModel = new MainWindowViewModel();
            this.DataContext = viewModel;

            LoadCongfig(cfgPath);
        }

        private void RemoveEcuTab(TabItem tab) {
            if(tab.Content.GetType() != typeof(EcuTab))
                return;
            EcuTab ecuTab = (EcuTab)tab.Content;
            ecuTab.UnlinkTabIndex();
            viewModel.EcuList.Remove((EcuViewModel)ecuTab.DataContext);
            MainTab.Items.Remove(tab);
        }

        private void RemoveAllEcuTab() {
            while(MainTab.Items.Count > 1) {
                TabItem tab = MainTab.Items[0] as TabItem;
                if(tab.Content.GetType() == typeof(EcuTab))
                    RemoveEcuTab(tab);
            }
        }

        private int GetTraceTabIndex() {
            for(int i = 0; i < MainTab.Items.Count; i++) {
                TabItem tabItem = (TabItem)MainTab.Items[i];
                if(tabItem.Content.GetType() == typeof(TraceTab))
                    return i;
            }
            return -1;
        }

        private TabItem CreateEcuTab(EcuViewModel ecuView) {
            StackPanel stackPanel = new StackPanel() { Height = 20, Orientation = Orientation.Horizontal, Background = new SolidColorBrush(Color.FromArgb(0x01, 0xFF, 0xFF, 0xFF)) };
            Border iconBorder = new Border() { Width = 16, Height = 16 };
            iconBorder.Child = new Image() { Source = new BitmapImage(new Uri("/VFlash;component/Resources/Images/Icons/Ecu.png", UriKind.RelativeOrAbsolute)) };
            TextBlock text = new TextBlock() { Margin = new Thickness(5, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            text.SetBinding(TextBlock.TextProperty, new Binding("Name"));

            stackPanel.Children.Add(iconBorder);
            stackPanel.Children.Add(text);

            ecuView.PropertyChanged += (o, e) => {
                if(e.PropertyName == "IsFlashing") {
                    if(ecuView.IsFlashing == false) {
                        if(iconBorder.Child.GetType() == typeof(LoadingIcon))
                            ((LoadingIcon)iconBorder.Child).Dispose();
                        iconBorder.Child = new Image() { Source = new BitmapImage(new Uri("/VFlash;component/Resources/Images/Icons/Ecu.png", UriKind.RelativeOrAbsolute)) };
                    }
                    else
                        iconBorder.Child = new LoadingIcon();
                }
            };

            TabItem tabItem = new TabItem() { Header = stackPanel, Content = new EcuTab(), DataContext = ecuView };
            viewModel.EcuList.Add(ecuView);

            ContextMenu menu = new ContextMenu();

            Image editIcon = new Image();
            editIcon.Source = new BitmapImage(new Uri("/VFlash;component/Resources/Images/Icons/Rename.png", UriKind.RelativeOrAbsolute));
            MenuItem rename = new MenuItem() { Header = "Rename", Icon = editIcon };
            rename.Click += (o, e) => {
                RenameWindow nameInput = new RenameWindow();
                nameInput.Owner = this;
                nameInput.NameString = ecuView.Name;
                if(nameInput.ShowDialog() == true)
                    ecuView.Name = nameInput.NameString;
            };
            menu.Items.Add(rename);

            Image removeIcon = new Image();
            removeIcon.Source = new BitmapImage(new Uri("/VFlash;component/Resources/Images/Icons/Remove.png", UriKind.RelativeOrAbsolute));
            MenuItem remove = new MenuItem() { Header = "Remove", Icon = removeIcon };
            remove.Click += (o, e) => {
                if(ConfirmWindow.ShowDialog("Are you sure want to remove " + ecuView.Name + "?") == true)
                    RemoveEcuTab(tabItem);
            };
            menu.Items.Add(remove);

            int traceTabIndex = GetTraceTabIndex();
            if(traceTabIndex < 0)
                MainTab.Items.Add(tabItem);
            else {
                object traceTab = MainTab.Items[traceTabIndex];
                MainTab.Items[traceTabIndex] = tabItem;
                MainTab.Items.Add(traceTab);
            }

            stackPanel.ContextMenu = menu;

            return tabItem;
        }

        private void LoadCongfig(string cfgPath) {
            if(cfgPath == null || cfgPath == "") {
                string configPathSaved = Properties.Settings.Default.ConfigPath;
                if(!(configPathSaved == null || configPathSaved == "" || !File.Exists(configPathSaved)))
                    cfgPath = configPathSaved;
                else {
                    RemoveAllEcuTab();
                    CreateEcuTab(new EcuViewModel() { Name = "ECU Flash" });
                    MainTab.SelectedIndex = 0;
                    return;
                }
            }

            try {
                Directory.SetCurrentDirectory(Path.GetDirectoryName(cfgPath));
                ConfigReader config = new ConfigReader(cfgPath);
                this.Title = "VFlash - ";
                this.Title += (config.Title == "") ? Path.GetFileName(cfgPath) : config.Title;

                RemoveAllEcuTab();
                foreach(EcuFlashInfo ecu in config.GetEcuFlashInfo())
                    CreateEcuTab(new EcuViewModel(ecu));
                MainTab.SelectedIndex = 0;

                viewModel.ConfigPath = cfgPath;
                Properties.Settings.Default.ConfigPath = cfgPath;
                Properties.Settings.Default.Save();
            }
            catch(Exception ex) {
                ErrorWindow.ShowDialog("Can't open config file. " + ex.Message);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e) {
            if(ConfirmWindow.ShowDialog("Are you sure want to exit this application?") == true)
                this.Close();
        }

        private void NewEcu_Click(object sender, RoutedEventArgs e) {
            NewEcuWindow newEcuWindow = new NewEcuWindow(viewModel.EcuList);
            newEcuWindow.Owner = this;
            if(newEcuWindow.ShowDialog() == true)
                CreateEcuTab(newEcuWindow.Value);
        }

        private void OpenConfig_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Filter = "VFlash Config|*.vcfg";
            if(openFile.ShowDialog() == true)
                LoadCongfig(openFile.FileName);
        }

        private void Save_Click(object sender, RoutedEventArgs e) {
            string filePath = viewModel.ConfigPath;
            bool isShouldSaveAs = false;
            string defaultCfg = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "config/default.vcfg");
            if(filePath == null)
                isShouldSaveAs = true;
            else if(filePath == defaultCfg)
                isShouldSaveAs = true;
            else if(File.Exists(filePath) && new FileInfo(filePath).IsReadOnly)
                isShouldSaveAs = true;

            if(!isShouldSaveAs) {
                if(ConfirmWindow.ShowDialog("Are you sure want to override " + Path.GetFileName(viewModel.ConfigPath) + " file?") == true) {
                    ConfigWriter configWriter = new ConfigWriter(viewModel.ConfigPath);
                    configWriter.Save(viewModel.GetEcuFlashInfo());
                }
            }
            else
                SaveAs_Click(sender, e);
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e) {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "VFlash Config|*.vcfg";
            if(saveFileDialog.ShowDialog() == true) {
                ConfigWriter configWriter = new ConfigWriter(saveFileDialog.FileName);
                configWriter.Save(viewModel.GetEcuFlashInfo());
                viewModel.ConfigPath = saveFileDialog.FileName;
                Properties.Settings.Default.ConfigPath = viewModel.ConfigPath;
                Properties.Settings.Default.Save();
            }
        }

        private long GetTotalFileSize() {
            long size = 0;
            foreach(EcuViewModel ecu in viewModel.EcuList) {
                foreach(FileItemViewModel file in ecu.Segments)
                    size += file.Size;
            }
            return size;
        }

        private void ClearAllLogAndStatus() {
            viewModel.Trace.Clear();
            viewModel.TraceShow.Clear();
            foreach(EcuViewModel ecu in viewModel.EcuList) {
                ecu.Steps.Clear();
                foreach(FileItemViewModel file in ecu.Files)
                    file.Status = -1;
            }
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;
            FlashProgressBar.Value = 0;
            FlashProgressBar.RestartTimer();
            MainWindowViewModel.ResetTimeStamp();
        }

        private void Flash_Click(object sender, RoutedEventArgs e) {
            if(viewModel.IsBusy || (flashThread != null && flashThread.IsAlive))
                return;
            viewModel.IsBusy = true;
            flashThread = new Thread(() => {
                try {
                    StartFlash();
                }
                catch(Exception ex) {
                    Dispatcher.BeginInvoke(new Action(() => throw ex));
                }
                viewModel.IsBusy = false;
            }) {
                IsBackground = true
            };
            ClearAllLogAndStatus();
            flashThread.Start();
        }

        private void StartFlash() {
            int flashCount = 0;
            long totalSize = GetTotalFileSize();
            long bytesSent = 0;

            Dispatcher.Invoke(() => FlashProgressBar.Maximum = (totalSize > 0) ? totalSize : 100);

            foreach(EcuViewModel ecu in viewModel.EcuList) {
                EcuFlashInfo ecuFlashInfo = ecu.ToEcuFlashInfo();
                FlashConfigInfo cfg = ecuFlashInfo.Config;
                List<FlashAction> actions;
                bool alwaysExecuted = false;
                if(cfg.FlashActionsPath == null || cfg.FlashActionsPath == "")
                    actions = FlashSequence.DefaultActions;
                else {
                    ActionsReader actionsReader = new ActionsReader(cfg.FlashActionsPath);
                    actions = actionsReader.Actions;
                    alwaysExecuted = actionsReader.AlwaysExecuted;
                }

                if(alwaysExecuted != true && ecu.Segments.Count == 0)
                    continue;
                else if(ecu.Config.Channel == null || ecu.Config.Channel == "")
                    continue;

                Dispatcher.Invoke(() => ecu.IsFlashing = true);

                FlashSequence flashing = new FlashSequence(actions);

                FlashStepViewModel currentStep = null;
                flashing.NewActionEvent += (o, e) => Dispatcher.Invoke(() => {
                    currentStep = new FlashStepViewModel() { TimeStamp = MainWindowViewModel.TimeStamp, Description = e.Message };
                    ecu.Steps.Add(currentStep);
                });
                flashing.ActionFinishEvent += (o, e) => currentStep.IsRunning = false;
                flashing.ActionErrorEvent += (o, e) => currentStep.IsFailed = true;
                flashing.UnknowErrorEvent += (o, e) => {
                    Dispatcher.Invoke(() => {
                        currentStep.IsFailed = true;
                        ErrorWindow.ShowDialog(e.Message);
                    });
                };

                flashing.ProgressChanged += (o, e) => {
                    Dispatcher.Invoke(() => {
                        ecu.Files[e.FileIndex].Status = (double)e.Value * 100 / e.Total;
                        FlashProgressBar.Value = bytesSent + e.Value;
                        TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                        double value = FlashProgressBar.Value / FlashProgressBar.Maximum;
                        TaskbarItemInfo.ProgressValue = (value < 0.001D) ? 0.001D : value;
                        if(e.Value == e.Total)
                            bytesSent += e.Value;
                    });
                };

                CanDevice canDevice = DeviceManager.GetCanInstance(cfg.Device, cfg.Channel, cfg.CanType, cfg.Bitrate);
                FlashActionArgs actionArgs = new FlashActionArgs();
                actionArgs.EcuUDS = new UDS(new CanTp(canDevice, cfg.TxId, cfg.RxId) { STmin = cfg.STmin });
                actionArgs.FunUDS = new UDS(new CanTp(canDevice, cfg.FunctionalId, cfg.RxId) { STmin = cfg.STmin });
                actionArgs.Timeout = cfg.Timeout;
                actionArgs.SecurityLevel = cfg.SecurityLevel;
                actionArgs.SeedKeyDll = cfg.SeedKeyDll;
                actionArgs.Files = ecuFlashInfo.GetActiveFiles();
                actionArgs.UDSDefaultBufferSize = cfg.UDSBufferSize;

                flashing.Execute(actionArgs);

                Dispatcher.Invoke(() => ecu.IsFlashing = false);

                flashCount++;
            }

            Dispatcher.Invoke(() => {
                FlashProgressBar.StopTimer();
                if(flashCount > 0)
                    TaskbarItemInfo.ProgressState = (FlashProgressBar.Value >= FlashProgressBar.Maximum) ? TaskbarItemProgressState.None : TaskbarItemProgressState.Error;
                else
                    ErrorWindow.ShowDialog("Nothing to flash!");
            });
        }

        private void Help_Click(object sender, RoutedEventArgs e) {
            HelpWindow help = new HelpWindow();
            help.Owner = this;
            help.Show();
        }

        private void Info_Click(object sender, RoutedEventArgs e) {
            InfoWindow info = new InfoWindow();
            info.Owner = this;
            info.ShowDialog();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e) {
            if((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) {
                if(e.Key == Key.N)
                    NewEcu_Click(sender, e);
                else if(e.Key == Key.O)
                    OpenConfig_Click(sender, e);
                else if(e.Key == Key.S)
                    Save_Click(sender, e);
            }
        }
    }
}
