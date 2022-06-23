namespace VFlash.Flashing {
    public class InfomationEventArgs {
        public InfomationEventArgs(string msg) {
            Message = msg;
        }

        public string Message {
            get; private set;
        }
    }
}
