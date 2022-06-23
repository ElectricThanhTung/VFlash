using System.ComponentModel;

namespace VFlash.ViewModel {
    public class ViewModelBase : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
