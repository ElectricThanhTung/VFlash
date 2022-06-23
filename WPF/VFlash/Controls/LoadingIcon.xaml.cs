using System;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace VFlash {
    public partial class LoadingIcon : UserControl {
        private DispatcherTimer timer;
        private static BitmapImage[] imgs = {
            new BitmapImage(new Uri(@"/VFlash;component/Resources/Images/Icons/Loading/16x16/Image_0.png", UriKind.RelativeOrAbsolute)),
            new BitmapImage(new Uri(@"/VFlash;component/Resources/Images/Icons/Loading/16x16/Image_1.png", UriKind.RelativeOrAbsolute)),
            new BitmapImage(new Uri(@"/VFlash;component/Resources/Images/Icons/Loading/16x16/Image_2.png", UriKind.RelativeOrAbsolute)),
            new BitmapImage(new Uri(@"/VFlash;component/Resources/Images/Icons/Loading/16x16/Image_3.png", UriKind.RelativeOrAbsolute)),
            new BitmapImage(new Uri(@"/VFlash;component/Resources/Images/Icons/Loading/16x16/Image_4.png", UriKind.RelativeOrAbsolute)),
            new BitmapImage(new Uri(@"/VFlash;component/Resources/Images/Icons/Loading/16x16/Image_5.png", UriKind.RelativeOrAbsolute)),
            new BitmapImage(new Uri(@"/VFlash;component/Resources/Images/Icons/Loading/16x16/Image_6.png", UriKind.RelativeOrAbsolute)),
            new BitmapImage(new Uri(@"/VFlash;component/Resources/Images/Icons/Loading/16x16/Image_7.png", UriKind.RelativeOrAbsolute)),
            new BitmapImage(new Uri(@"/VFlash;component/Resources/Images/Icons/Loading/16x16/Image_8.png", UriKind.RelativeOrAbsolute)),
            new BitmapImage(new Uri(@"/VFlash;component/Resources/Images/Icons/Loading/16x16/Image_9.png", UriKind.RelativeOrAbsolute)),
            new BitmapImage(new Uri(@"/VFlash;component/Resources/Images/Icons/Loading/16x16/Image_10.png", UriKind.RelativeOrAbsolute)),
            new BitmapImage(new Uri(@"/VFlash;component/Resources/Images/Icons/Loading/16x16/Image_11.png", UriKind.RelativeOrAbsolute)),
        };
        private int step = 0;

        public LoadingIcon() {
            InitializeComponent();
            IconImage.Source = imgs[0];
            timer = new DispatcherTimer() {
                Interval = new TimeSpan(0, 0, 0, 0, 100),
            };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e) {
            if(++step >= imgs.Length)
                step = 0;
            IconImage.Source = imgs[step];
        }

        public TimeSpan Interval {
            get {
                return timer.Interval;
            }
            set {
                timer.Interval = value;
            }
        }

        public void Dispose() {
            timer.Stop();
        }
    }
}
