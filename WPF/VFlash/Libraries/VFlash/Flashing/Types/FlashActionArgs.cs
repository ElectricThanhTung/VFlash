using CanSharp;
using System.Collections.Generic;

namespace VFlash.Flashing {
    public class FlashActionArgs {
        public UDS EcuUDS {
            get; set;
        }

        public UDS FunUDS {
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

        public List<FlashFileInfo> Files {
            get; set;
        }
    }
}
