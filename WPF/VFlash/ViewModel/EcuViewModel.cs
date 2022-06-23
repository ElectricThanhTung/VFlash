using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace VFlash.ViewModel {
    public class EcuViewModel : ViewModelBase {
        private bool isFLashing = false;
        private string name;
        private ObservableCollection<FlashStepViewModel> steps;
        private ObservableCollection<FileItemViewModel> files;
        private ConfigViewModel config;

        public EcuViewModel() {
            Config = new ConfigViewModel();
            Steps = new ObservableCollection<FlashStepViewModel>();
            Files = new ObservableCollection<FileItemViewModel>();
        }

        public EcuViewModel(EcuFlashInfo ecuFlashInfo) {
            Config = new ConfigViewModel();
            Steps = new ObservableCollection<FlashStepViewModel>();
            Files = new ObservableCollection<FileItemViewModel>();

            Name = ecuFlashInfo.Name;
            foreach(FlashFileInfo file in ecuFlashInfo.Files) {
                Files.Add(new FileItemViewModel() {
                    IsUsed = file.IsUsed,
                    IsDriver = file.IsDriver,
                    FileName = file.FileName,
                    FilePath = file.FilePath,
                    CrcPath = file.CrcPath,
                });
            }

            Config = new ConfigViewModel() {
                Device = ecuFlashInfo.Config.Device,
                Channel = ecuFlashInfo.Config.Channel,
                CanType = ecuFlashInfo.Config.CanType,
                Bitrate = ecuFlashInfo.Config.Bitrate,
                TxId = ecuFlashInfo.Config.TxId,
                RxId = ecuFlashInfo.Config.RxId,
                FunctionalId = ecuFlashInfo.Config.FunctionalId,
                STmin = ecuFlashInfo.Config.STmin,
                P2Value = ecuFlashInfo.Config.P2Value,
                Timeout = ecuFlashInfo.Config.Timeout,
                SecurityLevel = ecuFlashInfo.Config.SecurityLevel,
                SeedKeyDll = ecuFlashInfo.Config.SeedKeyDll,
                FlashActionsPath = ecuFlashInfo.Config.FlashActionsPath,
            };
        }

        public EcuFlashInfo ToEcuFlashInfo() {
            EcuFlashInfo ret = new EcuFlashInfo();
            ret.Files = new List<FlashFileInfo>();
            ret.Name = Name;
            foreach(FileItemViewModel file in Files) {
                ret.Files.Add(new FlashFileInfo() {
                    IsUsed = file.IsUsed,
                    IsDriver = file.IsDriver,
                    FileName = file.FileName,
                    FilePath = file.FilePath,
                    CrcPath = file.CrcPath,
                });
            }

            ret.Config = new FlashConfigInfo() {
                Device = Config.Device,
                Channel = Config.Channel,
                CanType = Config.CanType,
                Bitrate = Config.Bitrate,
                TxId = Config.TxId,
                RxId = Config.RxId,
                FunctionalId = Config.FunctionalId,
                STmin = Config.STmin,
                P2Value = Config.P2Value,
                Timeout = Config.Timeout,
                SecurityLevel = Config.SecurityLevel,
                SeedKeyDll = Config.SeedKeyDll,
                FlashActionsPath = Config.FlashActionsPath,
            };

            return ret;
        }

        public bool IsFlashing {
            get {
                return isFLashing;
            }
            set {
                if(isFLashing != value) {
                    isFLashing = value;
                    OnPropertyChanged(nameof(IsFlashing));
                }
            }
        }

        public string Name {
            get {
                return name;
            }
            set {
                if(name != value) {
                    name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public ObservableCollection<FlashStepViewModel> Steps {
            get {
                return steps;
            }
            set {
                if(steps != value) {
                    steps = value;
                    OnPropertyChanged(nameof(Steps));
                }
            }
        }

        private void UpdateFilesOnPropertyChangedEvent() {
            if(files != null) {
                for(int i = 0; i < files.Count; i++) {
                    files[i].PropertyChanged -= EcuViewModel_PropertyChanged;
                    files[i].PropertyChanged += EcuViewModel_PropertyChanged;
                }
            }
        }

        private void EcuViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            OnPropertyChanged(nameof(Segments));
        }

        private void EcuViewModel_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            UpdateFilesOnPropertyChangedEvent();
            OnPropertyChanged(nameof(Segments));
        }

        public ObservableCollection<FileItemViewModel> Files {
            get {
                return files;
            }
            set {
                if(files != value) {
                    files = value;
                    files.CollectionChanged -= EcuViewModel_CollectionChanged;
                    files.CollectionChanged += EcuViewModel_CollectionChanged;
                    UpdateFilesOnPropertyChangedEvent();
                    OnPropertyChanged(nameof(Files));
                }
            }
        }

        public ObservableCollection<FileItemViewModel> Segments {
            get {
                ObservableCollection<FileItemViewModel> ret = new ObservableCollection<FileItemViewModel>();
                foreach(FileItemViewModel item in Files) {
                    if(item.IsUsed && item.FilePath != "" && item.CrcPath != "")
                        ret.Add(item);
                }
                return ret;
            }
        }

        public ConfigViewModel Config {
            get {
                return config;
            }
            set {
                if(config != value) {
                    config = value;
                    OnPropertyChanged(nameof(Config));
                }
            }
        }
    }
}
