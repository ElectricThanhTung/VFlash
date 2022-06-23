using System.Collections.Generic;

namespace VFlash {
    public class EcuFlashInfo {
        public string Name {
            get; set;
        }

        public List<FlashFileInfo> Files {
            get; set;
        }

        public FlashConfigInfo Config {
            get; set;
        }

        public List<FlashFileInfo> GetActiveFiles() {
            List<FlashFileInfo> ret = new List<FlashFileInfo>();
            foreach(FlashFileInfo file in Files) {
                if(file.IsUsed && file.FilePath != "" && file.CrcPath != "")
                    ret.Add(file);
            }
            return ret;
        }
    }
}
