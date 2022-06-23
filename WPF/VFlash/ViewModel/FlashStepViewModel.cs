using System;
using System.Text;
using System.Windows.Media;
using System.Windows.Threading;

namespace VFlash.ViewModel {
    public class FlashStepViewModel : ViewModelBase {
        private long timeStamp;
        private long runtime;
        private string description;
        private bool isFailed;
        DispatcherTimer timer;

        public FlashStepViewModel() {
            timeStamp = 0;
            runtime = 0;
            description = "";
            isFailed = false;
            timer = new DispatcherTimer();
            timer.Interval = new System.TimeSpan(0, 0, 0, 0, 10);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private string TimeStampStringFormat(long ms) {
            long s = ms / 1000;
            StringBuilder timeStamp = new StringBuilder();
            timeStamp.Append((s / 60 % 60).ToString("00"));
            timeStamp.Append(":");
            timeStamp.Append((s % 60).ToString("00"));
            timeStamp.Append(".");
            timeStamp.Append((ms % 1000).ToString("000"));
            return timeStamp.ToString();
        }

        private string RuntimeStringFormat(long value) {
            string str;
            if(value < 10 * 1000)
                str = ((double)value / 1000).ToString("0.000") + "s";
            else if(value < 60 * 1000)
                str = ((double)value / 1000).ToString("0.00") + "s";
            else if(value < 10 * 60 * 1000)
                str = ((double)value / 1000 / 60).ToString("0.000") + "p";
            else if(value < 100 * 60 * 1000)
                str = ((double)value / 1000 / 60).ToString("0.00") + "p";
            else if(value < 1000 * 60 * 1000)
                str = ((double)value / 1000 / 60).ToString("0.0") + "p";
            else
                str = (value / 1000 / 60).ToString() + "p";
            return str;
        }

        public string TimeStampString {
            get {
                return TimeStampStringFormat(timeStamp);
            }
        }

        public long TimeStamp {
            get {
                return timeStamp;
            }
            set {
                if(timeStamp != value) {
                    timeStamp = value;
                    OnPropertyChanged(nameof(TimeStamp));
                    OnPropertyChanged(nameof(TimeStampString));
                }
            }
        }

        public string Description {
            get {
                return description;
            }
            set {
                if(description != value) {
                    description = value;
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        public Brush Foreground {
            get {
                if(isFailed)
                    return new SolidColorBrush(Colors.Red);
                else
                    return new SolidColorBrush(Colors.Black);
            }
        }

        public bool IsFailed {
            get {
                return isFailed;
            }
            set {
                if(isFailed != value) {
                    isFailed = value;
                    IsRunning = false;
                    OnPropertyChanged(nameof(IsFailed));
                    OnPropertyChanged(nameof(Foreground));
                }
            }
        }

        public string RuntimeString {
            get {
                return RuntimeStringFormat(runtime);
            }
        }

        public long Runtime {
            get {
                return runtime;
            }
        }

        private void Timer_Tick(object sender, EventArgs e) {
            runtime = MainWindowViewModel.TimeStamp - timeStamp;
            OnPropertyChanged(nameof(Runtime));
            OnPropertyChanged(nameof(RuntimeString));
        }

        public bool IsRunning {
            get {
                return timer.IsEnabled;
            }
            set {
                if(timer.IsEnabled != value) {
                    timer.IsEnabled = value;
                    if(value == false) {
                        runtime = MainWindowViewModel.TimeStamp - timeStamp;
                        OnPropertyChanged(nameof(Runtime));
                        OnPropertyChanged(nameof(RuntimeString));
                    }
                }
            }
        }
    }
}
