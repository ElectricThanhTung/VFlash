using System;
using System.Windows.Input;

namespace VFlash.ViewModel {
    public class RelayCommand<T> : ICommand {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        public RelayCommand(Predicate<T> canExecute, Action<T> execute) {
            if(execute == null)
                throw new ArgumentNullException("execute");
            this._execute = execute;
            this._canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => this._canExecute == null ? true : this._canExecute((T)parameter);

        public void Execute(object parameter) => this._execute((T)parameter);

        public void RaiseCanExecuteChanged() => this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
