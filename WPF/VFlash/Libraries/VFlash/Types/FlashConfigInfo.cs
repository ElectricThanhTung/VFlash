using OfficeOpenXml.Packaging.Ionic.Zip;
using System.IO;

namespace VFlash {
    public class FlashConfigInfo {
        private string canType;

        public FlashConfigInfo() {
            CanType = "CAN";
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

        public string Device {
            get; set;
        }

        public string Channel {
            get; set;
        }

        public string CanType {
            get {
                return canType;
            }
            set {
                if(value == "CAN FD")
                    canType = "CAN FD";
                else
                    canType = "CAN";
            }
        }

        public int Bitrate {
            get; set;
        }

        public int TxId {
            get; set;
        }

        public int RxId {
            get; set;
        }

        public int FunctionalId {
            get; set;
        }

        public double STmin {
            get; set;
        }

        public int P2Value {
            get; set;
        }

        public int Timeout {
            get; set;
        }

        public int SecurityLevel {
            get; set;
        }

        public string SeedKeyDll {
            get; set;
        }

        public string FlashActionsPath {
            get; set;
        }

        public int UDSBufferSize {
            get; set;
        }
    }
}
