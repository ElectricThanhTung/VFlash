using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace VFlash.ViewModel {
    public class TraceViewModel : ViewModelBase {
        private bool sendSuccessfull = false;
        private long timeStamp = 0;
        private int txId = -1;
        private int rxId = -1;
        private byte[] txData;
        private byte[] rxData;
        private long deltaTime = -1;

        private static Dictionary<byte, string> NRCs = new Dictionary<byte, string>() {
            { 0x10, "General reject" },
            { 0x11, "Service not supported" },
            { 0x12, "Sub Function not supported" },
            { 0x13, "Invalid message length/format" },
            { 0x14, "Response too long" },
            { 0x21, "Busy-repeat request" },
            { 0x22, "Conditions not correct" },
            { 0x24, "Request sequence error" },
            { 0x25, "No response from subnet component" },
            { 0x26, "Failure prevents execution of requested action" },
            { 0x31, "Request out of range" },
            { 0x33, "Security access denied" },
            { 0x35, "Invalid Key" },
            { 0x36, "Exceeded number of attempts" },
            { 0x37, "Required time delay has not expired" },
            { 0x70, "Upload/download not accepted" },
            { 0x71, "Transfer data suspended" },
            { 0x72, "Programming failure" },
            { 0x73, "Wrong block sequence counter" },
            { 0x78, "Response pending" },
            { 0x7E, "Sub function not supported in active session" },
            { 0x7F, "Service not supported in active session" },
            { 0x81, "RPM too high" },
            { 0x82, "RPM too low" },
            { 0x83, "Engine is running" },
            { 0x84, "Engine is not running" },
            { 0x85, "Engine run time too low" },
            { 0x86, "Temperature too high" },
            { 0x87, "Temperature too low" },
            { 0x88, "Speed too high" },
            { 0x89, "Speed too low" },
            { 0x8A, "Throttle pedal too high" },
            { 0x8B, "Throttle pedal too low" },
            { 0x8C, "Transmission range not in neutral" },
            { 0x8D, "Transmission range not in dear" },
            { 0x8F, "Brake switches not closed" },
            { 0x90, "Shifter lever not in park" },
            { 0x91, "Torque converter clutch locked" },
            { 0x92, "Voltage too high" },
            { 0x93, "Voltage too low" },
        };

        private string TimeStampStringFormat(long ms) {
            long s = ms / 1000;
            StringBuilder timeStamp = new StringBuilder();
            timeStamp.Append((s / 60 % 60).ToString("00"));
            timeStamp.Append(":");
            timeStamp.Append((s % 60).ToString("00"));
            timeStamp.Append(".");
            timeStamp.Append((ms % 1000).ToString("000"));
            return timeStamp.ToString();
        }

        private string DeltaTimeStringFormat(long value) {
            if(value < 0)
                return "";
            string str;
            if(value < 10 * 1000)
                str = ((double)value / 1000).ToString("0.000") + "s";
            else if(value < 60 * 1000)
                str = ((double)value / 1000).ToString("0.00") + "s";
            else if(value < 10 * 60 * 1000)
                str = ((double)value / 1000 / 60).ToString("0.000") + "p";
            else if(value < 100 * 60 * 1000)
                str = ((double)value / 1000 / 60).ToString("0.00") + "p";
            else if(value < 1000 * 60 * 1000)
                str = ((double)value / 1000 / 60).ToString("0.0") + "p";
            else
                str = (value / 1000 / 60).ToString() + "p";
            return str;
        }

        public string TimeStampString {
            get {
                return TimeStampStringFormat(timeStamp);
            }
        }

        public long TimeStamp {
            get {
                return timeStamp;
            }
            set {
                if(timeStamp != value) {
                    timeStamp = value;
                    OnPropertyChanged(nameof(TimeStamp));
                    OnPropertyChanged(nameof(TimeStampString));
                }
            }
        }

        public string TxIdString {
            get {
                if(txId < 0)
                    return "";
                return txId.ToString("X3");
            }
        }

        public int TxId {
            get {
                return txId;
            }
            set {
                if(txId != value) {
                    txId = value;
                    OnPropertyChanged(nameof(TxId));
                    OnPropertyChanged(nameof(TxIdString));
                }
            }
        }

        public string RxIdString {
            get {
                if(rxId < 0)
                    return "";
                return rxId.ToString("X3");
            }
        }

        public int RxId {
            get {
                return rxId;
            }
            set {
                if(rxId != value) {
                    rxId = value;
                    OnPropertyChanged(nameof(RxId));
                    OnPropertyChanged(nameof(RxIdString));
                }
            }
        }

        public bool SendSuccessfull {
            get {
                return sendSuccessfull;
            }
            set {
                if(sendSuccessfull != value) {
                    sendSuccessfull = value;
                    OnPropertyChanged(nameof(SendSuccessfull));
                }
            }
        }

        public Brush RequestForeground {
            get {
                return new SolidColorBrush(sendSuccessfull ? Colors.Black : Color.FromArgb(255, 204, 0, 0));
            }
        }

        private string ArrayToString(byte[] data) {
            StringBuilder str = new StringBuilder();
            int length = data.Length > 64 ? 64 : data.Length;
            for(int i = 0; i < length; i++) {
                str.Append(data[i].ToString("X2"));
                str.Append(" ");
            }
            if(length < data.Length)
                str.Append("...");
            return str.ToString().Trim();
        }

        public string RequestDataString {
            get {
                if(txData == null)
                    return "";
                return ArrayToString(txData);
            }
        }

        public string RequestName {
            get {
                if(txData == null)
                    return "Null";
                return "Unknow";
            }
        }

        public byte[] RequestData {
            get {
                return txData;
            }
            set {
                if(txData != value) {
                    txData = value;
                    OnPropertyChanged(nameof(RequestData));
                    OnPropertyChanged(nameof(RequestDataString));
                    OnPropertyChanged(nameof(RequestName));
                }
            }
        }

        public string ResponseDataString {
            get {
                if(rxData == null)
                    return "";
                return ArrayToString(rxData);
            }
        }

        public string ResponseCodes {
            get {
                if(rxData == null)
                    return "Null";
                else if(rxData[0] != 0x7F)
                    return "Positive response";
                else {
                    if(NRCs.ContainsKey(rxData[rxData.Length - 1]))
                        return NRCs[rxData[rxData.Length - 1]];
                    else
                        return "Unknow";
                }
            }
        }

        public byte[] ResponseData {
            get {
                return rxData;
            }
            set {
                if(rxData != value) {
                    rxData = value;
                    OnPropertyChanged(nameof(ResponseData));
                    OnPropertyChanged(nameof(ResponseDataString));
                    OnPropertyChanged(nameof(ResponseCodes));
                    if(rxData != null)
                        OnPropertyChanged(nameof(ResponseForeground));
                }
            }
        }

        public Brush ResponseForeground {
            get {
                if(rxData == null)
                    return new SolidColorBrush(Colors.Black);
                if(rxData[0] == 0x7F) {
                    if(rxData.Length == 3 && rxData[2] == 0x78)
                        return new SolidColorBrush(Color.FromArgb(255, 255, 153, 51));
                    return new SolidColorBrush(Color.FromArgb(255, 204, 0, 0));
                }
                return new SolidColorBrush(Color.FromArgb(255, 64, 145, 40));
            }
        }

        public string DeltaTimeString {
            get {
                return DeltaTimeStringFormat(deltaTime);
            }
        }

        public long DeltaTime {
            get {
                return deltaTime;
            }
            set {
                if(deltaTime != value) {
                    deltaTime = value;
                    OnPropertyChanged(nameof(DeltaTime));
                    OnPropertyChanged(nameof(DeltaTimeString));
                }
            }
        }
    }
}
