using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using VFlash.ViewModel;
using System.Windows.Input;

namespace VFlash {
    public partial class FilesTab : UserControl {
        public FilesTab() {
            InitializeComponent();
        }

        private void AddMenu_Click(object sender, RoutedEventArgs e) {
            FileInfoEditor editor = new FileInfoEditor();
            editor.Owner = App.Current.MainWindow;
            editor.DataContext = new FileItemViewModel() { IsUsed = true, IsDriver = false };
            if(editor.ShowDialog() == true)
                ((ObservableCollection<FileItemViewModel>)FileList.ItemsSource).Add((FileItemViewModel)editor.DataContext);
        }

        private void EditMenu_Click(object sender, RoutedEventArgs e) {
            if(FileList.SelectedIndex < 0)
                return;
            FileItemViewModel selectedItem = ((ObservableCollection<FileItemViewModel>)FileList.ItemsSource)[FileList.SelectedIndex];
            FileInfoEditor editor = new FileInfoEditor();
            editor.Owner = App.Current.MainWindow;
            editor.DataContext = selectedItem;
            editor.ShowDialog();
        }

        private void RemoveMenu_Click(object sender, RoutedEventArgs e) {
            if(FileList.SelectedIndex >= 0) {
                ObservableCollection<FileItemViewModel> list = (ObservableCollection<FileItemViewModel>)FileList.ItemsSource;
                string fileName = list[FileList.SelectedIndex].FileName;
                if(ConfirmWindow.ShowDialog("Are you sure want to remove " + fileName) == true) {
                    list.RemoveAt(FileList.SelectedIndex);
                    FileList.SelectedIndex = -1;
                }
            }
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e) {
            if(FileList.SelectedIndex > 0) {
                ObservableCollection<FileItemViewModel> list = (ObservableCollection<FileItemViewModel>)FileList.ItemsSource;
                FileItemViewModel temp = list[FileList.SelectedIndex - 1];
                FileItemViewModel selected = list[FileList.SelectedIndex];
                list[FileList.SelectedIndex - 1] = selected;
                list[FileList.SelectedIndex] = temp;
                FileList.SelectedIndex--;
            }
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e) {
            ObservableCollection<FileItemViewModel> list = (ObservableCollection<FileItemViewModel>)FileList.ItemsSource;
            if(FileList.SelectedIndex >= 0 && FileList.SelectedIndex < (list.Count - 1)) {
                FileItemViewModel temp = list[FileList.SelectedIndex + 1];
                FileItemViewModel selected = list[FileList.SelectedIndex];
                list[FileList.SelectedIndex + 1] = selected;
                list[FileList.SelectedIndex] = temp;
                FileList.SelectedIndex++;
            }
        }

        private void FileItem_Click(object sender, MouseButtonEventArgs e) {
            if(e.ClickCount == 2)
                EditMenu_Click(sender, null);
        }
    }
}
