using System.Collections.Generic;
using System.IO;
using System.Xml;
using VFlash;
using VFlash.ViewModel;

namespace VFlashFiles {
    internal class ConfigWriter {
        private string configPath;
        private string configFolder;
        private XmlDocument vinflashDoc;

        public ConfigWriter(string configPath) {
            this.configPath = configPath;
            this.configFolder = Path.GetDirectoryName(configPath).Replace("/", "\\") + "\\";
            vinflashDoc = new XmlDocument();
            vinflashDoc.LoadXml("<VFlash Title=\"VFlash Config\"></VFlash>");
        }

        private XmlNode CreateConfigValueNode(string name, string value) {
            XmlNode node = vinflashDoc.CreateNode("element", name, "");
            node.InnerText = value;
            return node;
        }

        private XmlNode CreateFileNode(FlashFileInfo flashFile) {
            XmlNode fileElem = vinflashDoc.CreateNode("element", "File", "");

            XmlAttribute labelAttr = vinflashDoc.CreateAttribute("Label");
            labelAttr.Value = flashFile.FileName.Trim();
            fileElem.Attributes.Append(labelAttr);

            if(flashFile.IsDriver) {
                XmlAttribute attribute = vinflashDoc.CreateAttribute("IsDriver");
                attribute.Value = "True";
                fileElem.Attributes.Append(attribute);
            }

            string filePath = flashFile.FilePath.Trim().Replace("/", "\\");
            if(filePath != "") {
                XmlAttribute attribute = vinflashDoc.CreateAttribute("Path");
                if(filePath.IndexOf(configFolder) == 0)
                    filePath = filePath.Substring(configFolder.Length);
                attribute.Value = filePath;
                fileElem.Attributes.Append(attribute);
            }

            string crcPath = flashFile.CrcPath.Trim().Replace("/", "\\");
            if(crcPath != "") {
                XmlAttribute attribute = vinflashDoc.CreateAttribute("CRC");
                if(crcPath.IndexOf(configFolder) == 0)
                    crcPath = crcPath.Substring(configFolder.Length);
                attribute.Value = crcPath;
                fileElem.Attributes.Append(attribute);
            }

            return fileElem;
        }

        public void Save(List<EcuFlashInfo> flashInfos) {
            XmlElement root = vinflashDoc.DocumentElement;

            foreach(EcuFlashInfo flashInfo in flashInfos) {
                XmlNode flashElem = vinflashDoc.CreateElement("Flash");

                XmlAttribute titleAttr = vinflashDoc.CreateAttribute("Title");
                titleAttr.Value = flashInfo.Name;
                flashElem.Attributes.Append(titleAttr);

                if(flashInfo.Files.Count > 0) {
                    XmlNode dataElem = vinflashDoc.CreateElement("Data");
                    foreach(FlashFileInfo flashFile in flashInfo.Files)
                        dataElem.AppendChild(CreateFileNode(flashFile));
                    flashElem.AppendChild(dataElem);
                }

                XmlNode cfgNode = vinflashDoc.CreateElement("Config");

                FlashConfigInfo cfg = flashInfo.Config;
                cfgNode.AppendChild(CreateConfigValueNode("Device", cfg.Device));
                cfgNode.AppendChild(CreateConfigValueNode("CanType", cfg.CanType));
                cfgNode.AppendChild(CreateConfigValueNode("Bitrate", cfg.Bitrate.ToString()));
                cfgNode.AppendChild(CreateConfigValueNode("TxId", "0x" + cfg.TxId.ToString("X")));
                cfgNode.AppendChild(CreateConfigValueNode("RxId", "0x" + cfg.RxId.ToString("X")));
                cfgNode.AppendChild(CreateConfigValueNode("FunctionalId", "0x" + cfg.FunctionalId.ToString("X")));
                cfgNode.AppendChild(CreateConfigValueNode("STmin", cfg.STmin.ToString()));
                cfgNode.AppendChild(CreateConfigValueNode("P2", cfg.P2Value.ToString()));
                cfgNode.AppendChild(CreateConfigValueNode("Timeout", cfg.Timeout.ToString()));
                cfgNode.AppendChild(CreateConfigValueNode("SecurityLevel", cfg.SecurityLevel.ToString()));
                cfgNode.AppendChild(CreateConfigValueNode("UDSBufferSize", cfg.UDSBufferSize.ToString()));

                if(cfg.SeedKeyDll != null && cfg.SeedKeyDll.Trim() != "") {
                    string seedkeydll = cfg.SeedKeyDll.Trim().Replace("/", "\\");
                    if(seedkeydll.IndexOf(configFolder) == 0)
                        seedkeydll = seedkeydll.Substring(configFolder.Length);
                    cfgNode.AppendChild(CreateConfigValueNode("SeedKeyDll", seedkeydll));
                }

                if(cfg.FlashActionsPath != null && cfg.FlashActionsPath.Trim() != "") {
                    string actionsPath = cfg.FlashActionsPath.Trim().Replace("/", "\\");
                    if(actionsPath.IndexOf(configFolder) == 0)
                        actionsPath = actionsPath.Substring(configFolder.Length);
                    cfgNode.AppendChild(CreateConfigValueNode("FlashActionsPath", actionsPath));
                }

                flashElem.AppendChild(cfgNode);

                root.AppendChild(flashElem);
            }

            vinflashDoc.Save(configPath);
        }
    }
}
