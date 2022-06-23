using System;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace VFlash {
    public partial class CustomProgressBar : UserControl {
        private double maximun;
        private double value;
        private long startTime;
        private DispatcherTimer timer;

        public CustomProgressBar() {
            InitializeComponent();
            maximun = 100;
            value = 0;
            startTime = 0;
            timer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 50) };
            timer.Tick += Timer_Tick;
        }

        private static long GetCurrentTime() {
            DateTime now = DateTime.Now;
            return (((long)now.Day * 24 + now.Hour) * 3600 + now.Minute * 60 + now.Second) * 1000 + now.Millisecond;
        }

        public void RestartTimer() {
            timer.Start();
            startTime = GetCurrentTime();
        }

        public void StopTimer() {
            timer.Stop();
            UpdateRuntime();
        }

        private string TimeStampStringFormat(long ms) {
            long s = ms / 1000;
            StringBuilder timeStamp = new StringBuilder();
            timeStamp.Append((s / 60 % 60).ToString("00"));
            timeStamp.Append(":");
            timeStamp.Append((s % 60).ToString("00"));
            timeStamp.Append(".");
            timeStamp.Append((ms / 10 % 100).ToString("00"));
            return timeStamp.ToString();
        }

        private void UpdateRuntime() {
            RuntimeTextBlock.Text = TimeStampStringFormat(GetCurrentTime() - startTime);
        }

        private void Timer_Tick(object sender, EventArgs e) {
            UpdateRuntime();
        }

        private void CalcPercent() {
            double percent = value * 100 / maximun;
            BorderProgressBar.Width = value *((Border)BorderProgressBar.Parent).ActualWidth / maximun;
            PercnetTextBlock.Text = percent.ToString("00.00") + "%";
        }

        private void ProgressBar_SizeChanged(object sender, SizeChangedEventArgs e) {
            CalcPercent();
        }

        public double Maximum {
            get {
                return maximun;
            }
            set {
                if(maximun != value) {
                    maximun = value;
                    CalcPercent();
                }
            }
        }

        public double Value {
            get {
                return value;
            }
            set {
                if(this.value != value) {
                    this.value = value;
                    if(this.value == maximun) {
                        timer.Stop();
                        UpdateRuntime();
                    }
                    CalcPercent();
                }
            }
        }
    }
}
