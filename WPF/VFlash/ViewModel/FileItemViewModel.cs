using System.IO;
using System.Threading;
using VFlashFiles;

namespace VFlash.ViewModel {
    public class FileItemViewModel : ViewModelBase {
        private bool isUsed = true;
        private bool isDriver;
        private double status = -1;
        private string fileName = "";
        private string filePath = "";
        private string crcPath = "";
        private long address = -1;
        private long size = -1;

        private Thread readerThread;

        public bool IsUsed {
            get {
                return isUsed;
            }
            set {
                if(isDriver == false) {
                    isUsed = value;
                    OnPropertyChanged(nameof(IsUsed));
                }
            }
        }

        public bool IsDriver {
            get {
                return isDriver;
            }
            set {
                if(isDriver != value) {
                    isDriver = value;
                    OnPropertyChanged(nameof(FileType));
                }
            }
        }

        public string StatusPercent {
            get {
                if(status == -1)
                    return "";
                if(status >= 100)
                    return "100%";
                else
                    return status.ToString("00.0") + "%";
            }
        }

        public double Status {
            get {
                return status;
            }
            set {
                if(status != value) {
                    status = value;
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(StatusPercent));
                }
            }
        }

        public string FileType {
            get {
                return isDriver ? "Driver" : "Data";
            }
        }

        public string FileName {
            get {
                return fileName;
            }
            set {
                if(fileName != value) {
                    fileName = value;
                    OnPropertyChanged(nameof(FileName));
                }
            }
        }

        public string FilePath {
            get {
                return filePath;
            }
            set {
                if(filePath != value) {
                    filePath = value;
                    OnPropertyChanged(nameof(FilePath));

                    if(filePath.Trim() == "" || !File.Exists(filePath)) {
                        AddressString = "";
                        SizeString = "";
                        return;
                    }
                    if(new FileInfo(filePath).Length <= 2 * 1024 * 1024) {
                        ReadFileInfo(FilePath);
                        return;
                    }
                    if(readerThread != null && readerThread.IsAlive)
                        readerThread.Abort();
                    readerThread = new Thread(() => {
                        AddressString = "Loading...";
                        SizeString = "Loading...";
                        OnPropertyChanged(nameof(AddressString));
                        OnPropertyChanged(nameof(SizeString));
                        ReadFileInfo(filePath);
                    }) {
                        IsBackground = true
                    };
                    readerThread.Start();
                }
            }
        }

        private void ReadFileInfo(string file) {
            FwReader reader = new FwReader(filePath);
            FwData data = reader.MergeData;
            Address = data.StartAddress;
            Size = data.Data.Count;
        }

        public string CrcPath {
            get {
                return crcPath;
            }
            set {
                crcPath = value;
                OnPropertyChanged(nameof(CrcPath));
            }
        }

        public string AddressString {
            get; private set;
        }

        public long Address {
            get {
                return address;
            }
            private set {
                address = value;
                AddressString = Number.ToHex(address);
                OnPropertyChanged(nameof(Address));
                OnPropertyChanged(nameof(AddressString));
            }
        }

        public string SizeString {
            get; private set;
        }

        public long Size {
            get {
                return size;
            }
            private set {
                size = value;

                if(size < 1024)
                    SizeString = size + "B";
                else if(size < 1048576)
                    SizeString = ((double)size / 1024).ToString("0.0") + "KB";
                else
                    SizeString = ((double)size / 1048576).ToString("0.0") + "MB";

                OnPropertyChanged(nameof(Size));
                OnPropertyChanged(nameof(SizeString));
            }
        }
    }
}
