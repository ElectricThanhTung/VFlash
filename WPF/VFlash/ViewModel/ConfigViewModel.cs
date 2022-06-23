using System;
using System.Text;

namespace VFlash.ViewModel {
    public class ConfigViewModel : ViewModelBase {
        private string device;
        private string channel;
        private string canType;
        private string bitrate;
        private string txId;
        private string rxId;
        private string functionId;
        private string stmin;
        private string p2;
        private string timeout;
        private int securityLevel;
        private string seedKeyDll;
        private string flashActionsPath; 

        public ConfigViewModel() {
            canType = "CAN";
            Bitrate = 500000;
            TxId = 0xFFF;
            RxId = 0xFFF;
            FunctionalId = 0x6FF;
            STmin = 0;
            P2Value = 100;
            Timeout = 5000;
            SeedKeyDll = "";
            SecurityLevel = 0x07;
            FlashActionsPath = "";
        }

        private bool IsHexNumber(string text) {
            if(text.Length >= 2 && text[0] == '0' && (text[1] == 'x' || text[1] == 'X'))
                return true;
            return false;
        }

        private string CorrectNumberString(string text) {
            if(text == null)
                return null;
            StringBuilder ret = new StringBuilder();
            bool isHex = false;
            if(IsHexNumber(text)) {
                isHex = true;
                ret.Append(text[0]);
                ret.Append(text[1]);
            }
            for(int i = isHex ? 2 : 0; i < text.Length; i++) {
                char c = text[i];
                if(Char.IsNumber(c))
                    ret.Append(c);
                else if(isHex && (('a' <= c && c <= 'f') || ('A' <= c && c <= 'F'))) {
                    if('a' <= c && c <= 'f')
                        c -= (char)('a' - 'A');
                    ret.Append(c);
                }
            }
            return ret.ToString();
        }

        private string CorrectFloatString(string text) {
            if(text == null)
                return null;
            StringBuilder ret = new StringBuilder();
            bool isHex = false;
            bool point = false;
            if(IsHexNumber(text)) {
                isHex = true;
                ret.Append(text[0]);
                ret.Append(text[1]);
            }
            for(int i = isHex ? 2 : 0; i < text.Length; i++) {
                char c = text[i];
                if(Char.IsNumber(c))
                    ret.Append(c);
                else if(isHex && (('a' <= c && c <= 'f') || ('A' <= c && c <= 'F')))
                    ret.Append(c);
                else if(!isHex && !point && i > 0 && (c == '.' || c == ',')) {
                    point = true;
                    ret.Append('.');
                }
            }
            return ret.ToString();
        }

        public string Device {
            get {
                return device;
            }
            set {
                device = value;
                OnPropertyChanged(nameof(Device));
            }
        }

        public string Channel {
            get {
                return channel;
            }
            set {
                channel = value;
                OnPropertyChanged(nameof(Channel));
            }
        }

        public string CanType {
            get {
                return canType;
            }
            set {
                canType = value;
                OnPropertyChanged(nameof(canType));
            }
        }

        public string BitrateString {
            get {
                return CorrectNumberString(bitrate);
            }
            set {
                bitrate = value;
                OnPropertyChanged(nameof(Bitrate));
                OnPropertyChanged(nameof(BitrateString));
            }
        }

        public int Bitrate {
            get {
                return Number.ForceToInt(BitrateString);
            }
            set {
                BitrateString = value.ToString();
            }
        }

        public string TxIdString {
            get {
                return CorrectNumberString(txId);
            }
            set {
                txId = value;
                OnPropertyChanged(nameof(TxId));
                OnPropertyChanged(nameof(TxIdString));
            }
        }

        public int TxId {
            get {
                return Number.ForceToInt(TxIdString);
            }
            set {
                TxIdString = "0x" + value.ToString("X3");
            }
        }

        public string RxIdString {
            get {
                return CorrectNumberString(rxId);
            }
            set {
                rxId = value;
                OnPropertyChanged(nameof(RxId));
                OnPropertyChanged(nameof(RxIdString));
            }
        }

        public int RxId {
            get {
                return Number.ForceToInt(RxIdString);
            }
            set {
                RxIdString = "0x" + value.ToString("X3");
            }
        }

        public string FunctionalIdString {
            get {
                return CorrectNumberString(functionId);
            }
            set {
                functionId = value;
                OnPropertyChanged(nameof(FunctionalId));
                OnPropertyChanged(nameof(FunctionalIdString));
            }
        }

        public int FunctionalId {
            get {
                return Number.ForceToInt(FunctionalIdString);
            }
            set {
                FunctionalIdString = "0x" + value.ToString("X3");
            }
        }

        public string STminString {
            get {
                return CorrectFloatString(stmin);
            }
            set {
                stmin = value;
                OnPropertyChanged(nameof(STmin));
                OnPropertyChanged(nameof(STminString));
            }
        }

        public double STmin {
            get {
                try {
                    return double.Parse(STminString);
                }
                catch {
                    return 0;
                }
            }
            set {
                STminString = value.ToString();
            }
        }

        public string P2String {
            get {
                return CorrectNumberString(p2);
            }
            set {
                p2 = value;
                OnPropertyChanged(nameof(P2Value));
                OnPropertyChanged(nameof(P2String));
            }
        }

        public int P2Value {
            get {
                return Number.ForceToInt(P2String);
            }
            set {
                P2String = value.ToString();
            }
        }

        public string TimeoutString {
            get {
                return CorrectNumberString(timeout);
            }
            set {
                timeout = value;
                OnPropertyChanged(nameof(Timeout));
                OnPropertyChanged(nameof(TimeoutString));
            }
        }

        public int Timeout {
            get {
                return Number.ForceToInt(TimeoutString);
            }
            set {
                TimeoutString = value.ToString();
            }
        }

        public int SecurityLevel {
            get {
                return securityLevel;
            }
            set {
                securityLevel = value;
                OnPropertyChanged(nameof(SecurityLevel));
            }
        }

        public string SeedKeyDll {
            get {
                return seedKeyDll;
            }
            set {
                seedKeyDll = value;
                OnPropertyChanged(nameof(SeedKeyDll));
            }
        }

        public string FlashActionsPath {
            get {
                return flashActionsPath;
            }
            set {
                flashActionsPath = value;
                OnPropertyChanged(nameof(FlashActionsPath));
            }
        }
    }
}
