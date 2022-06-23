using System.Collections.Generic;

namespace VFlashFiles {
    public class FwData {
        public uint StartAddress {
            get; set;
        }

        public List<byte> Data {
            get; set;
        }
    }
}
