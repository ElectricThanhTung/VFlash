using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace VFlash {
    public partial class CustomScrollBar : UserControl {
        public event RoutedPropertyChangedEventHandler<double> ValueChanged;
        public event RoutedPropertyChangedEventHandler<double> MaximumChanged;
        public event RoutedPropertyChangedEventHandler<double> LargeChangeChanged;

        Storyboard scrollBarStoryboard = new Storyboard();
        private Stopwatch animationStopwatch = new Stopwatch();
        private bool thumbIsClicked = false;
        private bool mouseIsEnter = false;
        private double value = 0;
        private double largeChange = 10;
        private double maximum = 100;
        private System.Timers.Timer timer;
        private double yOffset;

        public CustomScrollBar() {
            InitializeComponent();
            timer = new System.Timers.Timer();
            timer.Interval = 1;
            timer.Elapsed += Timer_Tick;
            this.Loaded += CustomScrollBar_Loaded;
        }

        private void ScrollBarLostFocusAnimation() {
            DoubleAnimation animation = new DoubleAnimation(3, TimeSpan.FromMilliseconds(300));
            Storyboard.SetTarget(animation, MainBorder);
            Storyboard.SetTargetProperty(animation, new PropertyPath("Width"));

            scrollBarStoryboard.Children.Clear();
            scrollBarStoryboard.Children.Add(animation);
            scrollBarStoryboard.Completed += (_o, _e) => { scrollBarStoryboard.Children.Clear(); };
            scrollBarStoryboard.Begin();
        }

        private void Timer_Tick(object sender, EventArgs e) {
            this.Dispatcher.BeginInvoke(new Action(() => {
                if(thumbIsClicked || mouseIsEnter)
                    animationStopwatch.Restart();
                if(thumbIsClicked) {
                    this.CaptureMouse();
                    Point mouseLocation = Mouse.GetPosition(this);
                    MouseButtonState mouseLeftButton = Mouse.LeftButton;
                    this.ReleaseMouseCapture();

                    if(Mouse.LeftButton == MouseButtonState.Pressed) {
                        double y = mouseLocation.Y - yOffset;
                        if(y < 0)
                            y = 0;
                        else if((y + ThumbBorder.ActualHeight) > MainBorder.ActualHeight)
                            y = MainBorder.ActualHeight - ThumbBorder.ActualHeight;
                        ThumbBorder.Margin = new Thickness(0, y, 0, 0);

                        double temp = y * maximum / MainBorder.ActualHeight;
                        if(value != temp) {
                            value = temp;
                            ValueChanged?.Invoke(this, null);
                        }
                    }
                    else
                        thumbIsClicked = false;
                }
                else {
                    if(animationStopwatch.ElapsedMilliseconds >= 1000) {
                        animationStopwatch.Stop();
                        timer.Stop();
                        ScrollBarLostFocusAnimation();
                    }
                }
            }));
        }

        private void ThumbBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            thumbIsClicked = true;
            yOffset = e.GetPosition(ThumbBorder).Y;
            if(!timer.Enabled)
                timer.Start();
        }

        private void MainBorder_MouseEnter(object sender, MouseEventArgs e) {
            mouseIsEnter = true;
        }

        private void MainBorder_MouseLeave(object sender, MouseEventArgs e) {
            mouseIsEnter = false;
            if(!timer.Enabled) {
                timer.Start();
                animationStopwatch.Restart();
            }
        }

        private void UpdateBorderSizeAndLocation() {
            if(maximum > 0) {
                double h = largeChange * MainBorder.ActualHeight / maximum;
                ThumbBorder.Height = h;

                double y = value * MainBorder.ActualHeight / maximum;
                if(y < 0)
                    y = 0;
                else if((y + ThumbBorder.ActualHeight) > MainBorder.ActualHeight)
                    y = MainBorder.ActualHeight - ThumbBorder.ActualHeight;
                ThumbBorder.Margin = new Thickness(0, y, 0, 0);
            }
            else {
                ThumbBorder.Margin = new Thickness(0, 0, 0, 0);
                ThumbBorder.Height = MainBorder.ActualHeight;
                value = 0;
            }
        }

        private void CustomScrollBar_Loaded(object sender, RoutedEventArgs e) {
            UpdateBorderSizeAndLocation();
        }

        private void MainBorder_SizeChanged(object sender, SizeChangedEventArgs e) {
            UpdateBorderSizeAndLocation();
        }

        public double LargeChange {
            get {
                return largeChange;
            }
            set {
                if(largeChange != value) {
                    largeChange = value;
                    UpdateBorderSizeAndLocation();
                    LargeChangeChanged?.Invoke(this, null);
                }
            }
        }

        public double Value {
            get {
                return this.value;
            }
            set {
                double temp = value;
                if(temp < 0)
                    temp = 0;
                else if(temp + largeChange > maximum)
                    temp = maximum - largeChange;
                if(this.value != temp) {
                    ValueChanged?.Invoke(this, null);
                    this.value = temp;
                    UpdateBorderSizeAndLocation();
                }
            }
        }

        public double Maximum {
            get {
                return maximum;
            }
            set {
                if(maximum != value) {
                    maximum = value;
                    UpdateBorderSizeAndLocation();
                    MaximumChanged?.Invoke(this, null);
                }
            }
        }
    }
}
