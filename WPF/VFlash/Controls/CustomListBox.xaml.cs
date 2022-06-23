using System;
using System.Collections;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace VFlash {
    public partial class CustomListBox : UserControl {
        private static readonly DependencyProperty ItemsSourceProperty;
        private static readonly Color SelectedColor = Color.FromArgb(0x30, 0, 128, 255);
        private static readonly Color HighlightColor = Color.FromArgb(0x14, 0, 128, 255);

        private object highlightObject = null;
        private object selectedObject = null;
        private int selectedIndex = - 1;

        static CustomListBox() {
            ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(CustomListBox), new PropertyMetadata(null));
        }

        public CustomListBox() {
            InitializeComponent();

            MainItemsControl.DataContext = this;

            VScrollBar.LargeChangeChanged += VScrollBar_LargeChangeAndMaximumChanged;
            VScrollBar.MaximumChanged += VScrollBar_LargeChangeAndMaximumChanged;
            MainScrollViewer.ScrollChanged += MainScrollViewer_ScrollChanged;
        }

        private void VScrollBar_LargeChangeAndMaximumChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if(VScrollBar.LargeChange >= (VScrollBar.Maximum - 2))
                VScrollBar.Visibility = Visibility.Collapsed;
            else
                VScrollBar.Visibility = Visibility.Visible;
        }

        private void MainItemsControl_SizeChanged(object sender, SizeChangedEventArgs e) {
            VScrollBar.ValueChanged -= VScrollBar_ValueChanged;
            if((VScrollBar.Value + VScrollBar.LargeChange) >= (VScrollBar.Maximum - 2)) {
                VScrollBar.Maximum = MainItemsControl.ActualHeight;
                VScrollBar.Value = VScrollBar.Maximum - VScrollBar.LargeChange;
                MainScrollViewer.ScrollToVerticalOffset(VScrollBar.Value);
            }
            else
                VScrollBar.Maximum = MainItemsControl.ActualHeight;
            VScrollBar.ValueChanged += VScrollBar_ValueChanged;
        }

        private void MainScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e) {
            VScrollBar.ValueChanged -= VScrollBar_ValueChanged;
            VScrollBar.LargeChange = MainScrollViewer.ActualHeight;
            VScrollBar.Value = MainScrollViewer.ContentVerticalOffset;
            VScrollBar.ValueChanged += VScrollBar_ValueChanged;
        }

        private void VScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            MainScrollViewer.ScrollChanged -= MainScrollViewer_ScrollChanged;
            MainScrollViewer.ScrollToVerticalOffset(VScrollBar.Value);
            MainScrollViewer.UpdateLayout();
            MainScrollViewer.ScrollChanged += MainScrollViewer_ScrollChanged;
        }

        private object GetItemFormIndex(int index) {
            ContentPresenter container = (ContentPresenter)MainItemsControl.ItemContainerGenerator.ContainerFromIndex(index);
            container.ApplyTemplate();
            return (FrameworkElement)VisualTreeHelper.GetChild(container, 0);
        }

        private object GetItemAtPosition(Point point, out int index) {
            for(int i = 0; i < MainItemsControl.Items.Count; i++) {
                ContentPresenter container = (ContentPresenter)MainItemsControl.ItemContainerGenerator.ContainerFromIndex(i);
                container.ApplyTemplate();
                FrameworkElement control = (FrameworkElement)VisualTreeHelper.GetChild(container, 0);
                Point viewLocation = control.TransformToAncestor((Border)VisualTreeHelper.GetChild(MainItemsControl, 0)).Transform(new Point(0, 0));
                if((point.X >= viewLocation.X) && (point.X < (viewLocation.X + control.ActualWidth)) &&
                   (point.Y >= viewLocation.Y) && (point.Y < (viewLocation.Y + control.ActualHeight))) {
                    index = i;
                    return control;
                }
            }
            index = -1;
            return null;
        }

        private void ResetHightlightItem() {
            if(highlightObject != null) {
                Brush brush = (selectedObject != highlightObject) ? new SolidColorBrush(Colors.Transparent) : new SolidColorBrush(SelectedColor);
                SetViewBackground(highlightObject, brush);
            }
        }

        private void StackPanel_MouseMove(object sender, MouseEventArgs e) {
            ResetHightlightItem();
            object tempObject = GetItemAtPosition(e.GetPosition((StackPanel)sender), out _);
            if(tempObject != null) {
                highlightObject = tempObject;
                if(highlightObject == selectedObject) {
                    int temp = SelectedColor.A + HighlightColor.A;
                    byte a = (byte)(temp > 255 ? 255 : temp);
                    byte r = SelectedColor.R > HighlightColor.R ? SelectedColor.R : HighlightColor.R;
                    byte g = SelectedColor.G > HighlightColor.G ? SelectedColor.G : HighlightColor.G;
                    byte b = SelectedColor.B > HighlightColor.B ? SelectedColor.B : HighlightColor.B;
                    SetViewBackground(highlightObject, new SolidColorBrush(Color.FromArgb(a, r, g, b)));
                }
                else
                    SetViewBackground(highlightObject, new SolidColorBrush(HighlightColor));
            }
        }

        private void StackPanel_MouseDown(object sender, MouseButtonEventArgs e) {
            object tempObject = GetItemAtPosition(e.GetPosition((StackPanel)sender), out int index);
            if(index >= 0) {
                if(selectedObject != tempObject) {
                    if(selectedObject != null)
                        SetViewBackground(selectedObject, new SolidColorBrush(Colors.Transparent));
                    selectedIndex = index;
                    selectedObject = tempObject;
                    int temp = SelectedColor.A + HighlightColor.A;
                    byte a = (byte)(temp > 255 ? 255 : temp);
                    byte r = SelectedColor.R > HighlightColor.R ? SelectedColor.R : HighlightColor.R;
                    byte g = SelectedColor.G > HighlightColor.G ? SelectedColor.G : HighlightColor.G;
                    byte b = SelectedColor.B > HighlightColor.B ? SelectedColor.B : HighlightColor.B;
                    SetViewBackground(selectedObject, new SolidColorBrush(Color.FromArgb(a, r, g, b)));
                }
            }
        }

        private void StackPanel_MouseLeave(object sender, MouseEventArgs e) {
            ResetHightlightItem();
        }

        private void SetViewBackground(object view, Brush color) {
            Type objectType = view.GetType();
            PropertyInfo backgroundProperty = objectType.GetProperty("Background");
            if(backgroundProperty != null)
                backgroundProperty.SetValue(view, color, null);
        }

        public IEnumerable ItemsSource {
            get {
                return (IEnumerable)GetValue(ItemsSourceProperty);
            }
            set {
                SetValue(ItemsSourceProperty, value);
            }
        }

        public DataTemplate ItemTemplate {
            get {
                return MainItemsControl.ItemTemplate;
            }
            set {
                MainItemsControl.ItemTemplate = value;
            }
        }

        public int SelectedIndex {
            get {
                return selectedIndex;
            }
            set {
                if(selectedIndex != value) {
                    if(selectedObject != null)
                        SetViewBackground(selectedObject, new SolidColorBrush(Colors.Transparent));
                    selectedIndex = value;
                    if(selectedIndex >= 0) {
                        int temp = SelectedColor.A + HighlightColor.A;
                        byte a = (byte)(temp > 255 ? 255 : temp);
                        byte r = SelectedColor.R > HighlightColor.R ? SelectedColor.R : HighlightColor.R;
                        byte g = SelectedColor.G > HighlightColor.G ? SelectedColor.G : HighlightColor.G;
                        byte b = SelectedColor.B > HighlightColor.B ? SelectedColor.B : HighlightColor.B;
                        selectedObject = GetItemFormIndex(selectedIndex);
                        SetViewBackground(selectedObject, new SolidColorBrush(Color.FromArgb(a, r, g, b)));
                    }
                    else
                        selectedObject = null;
                }
            }
        }
    }
}
