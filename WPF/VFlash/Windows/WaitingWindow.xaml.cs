using System;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace VFlash {
    public partial class WaitingWindow : Window {
        private ThreadStart action;
        private bool shown;
        private int step = 2;

        private WaitingWindow(string msg, ThreadStart action) {
            InitializeComponent();
            this.action = action;
            MsgTextBlock.Text = msg;

            System.Timers.Timer timer = new System.Timers.Timer() {
                Interval = 30
            };
            timer.Elapsed += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e) {
            if(++step > 29)
                step = 1;
            Uri imgUri = new Uri(@"/VFlash;component/Resources/Images/Icons/Loading/64x64/Loading_" + step.ToString("00") + ".png", UriKind.RelativeOrAbsolute);
            this.Dispatcher.BeginInvoke(new Action(() => LoadingImage.Source = new BitmapImage(imgUri)));
        }

        protected override void OnContentRendered(EventArgs e) {
            base.OnContentRendered(e);

            if(shown)
                return;

            shown = true;

            Thread thread = new Thread(() => {
                try {
                    action();
                }
                catch(Exception ex) {
                    Dispatcher.BeginInvoke(new Action(() => throw ex));
                }

                Dispatcher.Invoke(() => DialogResult = true);
            }) {
                IsBackground = true
            };
            thread.Start();
        }

        public static bool? Start(string msg, ThreadStart action) {
            WaitingWindow wating = new WaitingWindow(msg, action);
            wating.Owner = App.Current.MainWindow;
            return wating.ShowDialog();
        }
    }
}
