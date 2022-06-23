using System.Collections.Generic;
using System.IO;

namespace VFlashFiles {
    internal class FwReader {
        public FwReader(string filePath) {
            string extName = Path.GetExtension(filePath).ToLower();
            if(extName == ".hex") {
                HexFileReader reader = new HexFileReader(filePath);
                DataBlocks = reader.DataBlocks;
                MergeData = reader.MergeData;
            }
            else {
                SrecReader reader = new SrecReader(filePath);
                DataBlocks = reader.DataBlocks;
                MergeData = reader.MergeData;
            }
            FirstStartAddress = DataBlocks[0].StartAddress;
            int lastIndex = DataBlocks.Count - 1;
            LastEndAddress = (uint)(DataBlocks[lastIndex].StartAddress + DataBlocks[lastIndex].Data.Count - 1);
        }

        public List<FwData> DataBlocks {
            get; private set;
        }

        public FwData MergeData {
            get; private set;
        }

        public uint FirstStartAddress {
            get; private set;
        }

        public uint LastEndAddress {
            get; private set;
        }
    }
}
