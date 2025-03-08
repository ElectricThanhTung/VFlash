using System.Collections.Generic;
using System.Text;
using System.Windows.Markup;
using System.Windows.Media;
using VFlash.Flashing;

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
                try {
                    switch(txData[0]) {
                        case 0x10:
                            return txData[1] switch {
                                0x01 => "Default Session",
                                0x02 => "Programming Session",
                                0x03 => "Extended Diagnostic Session",
                                0x04 => "Safety System Diagnostic Session",
                                _ => "Diagnostic Session Control"
                            };
                        case 0x11:
                            return txData[1] switch {
                                0x01 => "Hard Reset",
                                0x02 => "key Off-On Reset",
                                0x03 => "Soft Reset",
                                _ => "Reset"
                            };
                        case 0x14:
                            return "Clear Diagnostic Information";
                        case 0x27:
                            return (txData[1] % 2) switch {
                                0 => "Perform Security Access Level " + (txData[1] - 1),
                                1 => "Request Seed Level " + txData[1],
                                _ => "Security Access"
                            };
                        case 0x28: {
                            if((txData[1] & 0x01) != 0x00)
                                return "Disable Normal Communication";
                            else
                                return "Enable Normal Communication";
                        }
                        case 0x29:
                            return "Authentication";
                        case 0x3E:
                            return "Tester Present";
                        case 0x83:
                            return "Access Timing Parameters";
                        case 0x84:
                            return "Secured Data Transmission";
                        case 0x85:
                            return txData[1] switch {
                                0x01 => "Disable DTC Setting",
                                0x02 => "Enable DTC Setting",
                                _ => "Control DTC Settings"
                            };
                        case 0x86:
                            return "Response On Event";
                        case 0x87:
                            return "Link Control";
                        case 0x22:
                            return "Read Data By Identifier";
                        case 0x23:
                            return "Read Memory By Address";
                        case 0x24:
                            return "Read Scaling Data By Identifier";
                        case 0x2A:
                            return "Read Data By Identifier Periodic";
                        case 0x2C:
                            return "Dynamically Define Data Identifier";
                        case 0x2E:
                            return "Write Data By Identifier";
                        case 0x3D:
                            return "Write Memory By Address";
                        case 0x19:
                            return "Read DTC Information";
                        case 0x2F:
                            return "Input Output Control By Identifier";
                        case 0x31: {
                            if(txData.Length >= 4 && txData[1] == 0x01 && txData[2] == 0x02 && txData[3] == 0x03)
                                return "Check Programming Preconditions";
                            else if(txData.Length >= 13 && txData[1] == 0x01 && txData[2] == 0xFF && txData[3] == 0x00 && txData[4] == 0x44)
                                return "Request Erase";
                            else if(txData.Length >= 4 && txData[1] == 0x01 && txData[2] == 0xFF && txData[3] == 0x01)
                                return "Check Dependencies";
                            else if(txData.Length >= 4 && txData[1] == 0x01 && txData[2] == 0x02 && txData[3] == 0x02)
                                return "Check Signature/CRC";
                            return "Routine Control";
                        }
                        case 0x34: return "Request Download";
                        case 0x35: return "Request Upload";
                        case 0x36: return "Transfer Data";
                        case 0x37: return "Request Transfer Exit";
                        case 0x38: return "Request File Transfer";
                        default: return "Unknow";
                    }
                }
                catch {
                    return "Unknow";
                }
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
