using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace VFlash.ViewModel {
    internal class MainWindowViewModel : ViewModelBase {
        private ImageSource backgroundSource;
        private ObservableCollection<EcuViewModel> ecuList;
        private ObservableCollection<TraceViewModel> trace;
        private ObservableCollection<TraceViewModel> traceShow;
        private bool isBusy = false;
        private bool isShowTesterPresent = true;
        private static long startTime;

        private static long GetCurrentTime() {
            DateTime now = DateTime.Now;
            return (((long)now.Day * 24 + now.Hour) * 3600 + now.Minute * 60 + now.Second) * 1000 + now.Millisecond;
        }

        public static void ResetTimeStamp() {
            startTime = GetCurrentTime();
        }

        public bool IsBusy {
            get {
                return isBusy;
            }
            set {
                if(isBusy != value) {
                    isBusy = value;
                    OnPropertyChanged(nameof(IsBusy));
                    OnPropertyChanged(nameof(IsNotBusy));
                }
            }
        }

        public bool IsNotBusy {
            get {
                return !isBusy;
            }
        }

        public static long TimeStamp {
            get {
                return GetCurrentTime() - startTime;
            }
        }

        public MainWindowViewModel() {
            StartBackgroundAnimation();
            EcuList = new ObservableCollection<EcuViewModel>();
            Trace = new ObservableCollection<TraceViewModel>();
            TraceShow = new ObservableCollection<TraceViewModel>();
            ClearTrace = new RelayCommand<object>(p => true, p => {
                Trace.Clear();
                TraceShow.Clear();
            });
        }

        public string ConfigPath {
            get; set;
        }

        public ImageSource BackgroundSource {
            get {
                return backgroundSource;
            }
            set {
                backgroundSource = value;
                OnPropertyChanged(nameof(BackgroundSource));
            }
        }

        public ObservableCollection<EcuViewModel> EcuList {
            get {
                return ecuList;
            }
            set {
                ecuList = value;
                OnPropertyChanged(nameof(EcuList));
            }
        }

        public ObservableCollection<TraceViewModel> Trace {
            get {
                return trace;
            }
            set {
                if(trace != value) {
                    trace = value;
                    OnPropertyChanged(nameof(Trace));
                }
            }
        }

        public List<EcuFlashInfo> GetEcuFlashInfo() {
            List<EcuFlashInfo> ecuFlashInfos = new List<EcuFlashInfo>();
            foreach(EcuViewModel ecu in EcuList)
                ecuFlashInfos.Add(ecu.ToEcuFlashInfo());
            return ecuFlashInfos;
        }

        public ObservableCollection<TraceViewModel> TraceShow {
            get {
                return traceShow;
            }
            set {
                if(traceShow != value) {
                    traceShow = value;
                    TraceShowFilter.Filter = new Predicate<object>(o => Filter(o as TraceViewModel));
                    OnPropertyChanged("TraceShow");
                }
            }
        }

        public ICollectionView TraceShowFilter {
            get {
                return CollectionViewSource.GetDefaultView(traceShow);
            }
        }

        public bool IsShowTesterPresent {
            get {
                return isShowTesterPresent;
            }
            set {
                if(isShowTesterPresent != value) {
                    isShowTesterPresent = value;
                    TraceShowFilter.Refresh();
                    OnPropertyChanged(nameof(IsShowTesterPresent));
                    OnPropertyChanged(nameof(TraceShowFilter));
                }
            }
        }

        public ICommand ClearTrace {
            get;
        }

        private bool Filter(TraceViewModel item) {
            return isShowTesterPresent == true || !(item.RequestData.Length == 2 && item.RequestData[0] == 0x3E && item.RequestData[1] == 0x80);
        }

        private void StartBackgroundAnimation() {
            string backgoundFolder = "pack://application:,,,/Resources/Images/Backgrounds/AbstractWhite/";
            int bgCount = 1;
            bool d = false;

            BackgroundSource = new BitmapImage(new Uri(backgoundFolder + bgCount.ToString("00000") + ".jpg"));
            bgCount++;

            DispatcherTimer timer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 40) };
            timer.Tick += (o, e) => {
                BackgroundSource = new BitmapImage(new Uri(backgoundFolder + bgCount.ToString("00000") + ".jpg"));

                if(d) {
                    bgCount -= 1;
                    if(bgCount <= 1)
                        d = !d;
                }
                else {
                    bgCount += 1;
                    if(bgCount >= 999)
                        d = !d;
                }
            };
            timer.Start();
        }
    }
}
