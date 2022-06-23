namespace VFlash.Flashing {
    public class ProgressChangedArgs {
        public ProgressChangedArgs(int fileIndex, int value, int total) {
            FileIndex = fileIndex;
            Value = value;
            Total = total;
        }

        public int FileIndex {
            get; private set;
        }

        public int Value {
            get; private set;
        }

        public int Total {
            get; private set;
        }
    }
}
