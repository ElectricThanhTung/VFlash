using System;

namespace VFlash.Flashing {
    public abstract class FlashAction {
        public event InfomationEventHandler NewActionEvent;
        public event InfomationEventHandler ActionFinishEvent;
        public event InfomationEventHandler ActionErrorEvent;
        public event InfomationEventHandler UnknowErrorEvent;
        public event ProgressChangedHandler ProgressChanged;

        protected FlashAction() {
            AlwaysExecuted = false;
            IsSuccessful = null;
        }

        public bool AlwaysExecuted {
            get; set;
        }

        public bool? IsSuccessful {
            get; private set;
        }

        protected bool StartNewAction(string desc, Action action) {
            OnNewAction(desc);

            try {
                action();
            }
            catch(FlashException ex) {
                OnActionError(ex.Message);
                return false;
            }
            catch(Exception ex) {
                OnUnknowError(ex.Message);
                return false;
            }

            OnActionFinish(desc);
            return true;
        }

        protected void OnNewAction(string message) {
            NewActionEvent?.Invoke(this, new InfomationEventArgs(message));
        }

        protected void OnActionFinish(string message) {
            if(IsSuccessful == null)
                IsSuccessful = true;
            ActionFinishEvent?.Invoke(this, new InfomationEventArgs(message));
        }

        protected void OnActionError(string message) {
            IsSuccessful = false;
            ActionErrorEvent?.Invoke(this, new InfomationEventArgs(message));
        }

        protected void OnUnknowError(string message) {
            IsSuccessful = false;
            UnknowErrorEvent?.Invoke(this, new InfomationEventArgs(message));
        }

        protected void OnProgressChanged(int fileIndex, int value, int total) {
            ProgressChanged?.Invoke(this, new ProgressChangedArgs(fileIndex, value, total));
        }

        public abstract void Execute(FlashActionArgs actionArgs);
    }
}
