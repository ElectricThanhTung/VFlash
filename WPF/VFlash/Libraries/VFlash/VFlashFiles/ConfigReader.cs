using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using VFlash;

namespace VFlashFiles {
    internal class ConfigReader {
        private List<EcuFlashInfo> ecuFlashInfo;
        private string configPath;

        public ConfigReader(string configPath) {
            this.configPath = configPath;

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(configPath);
            Title = ReadAttribute(xmlDocument.FirstChild, "Title");

            if(xmlDocument.DocumentElement.Name != "VFlash")
                throw CreateException("Root node is not VFlash tag.");

            List<EcuFlashInfo> flashes = new List<EcuFlashInfo>();
            foreach(XmlNode node in xmlDocument.DocumentElement.ChildNodes) {
                if(node.Name == "Flash") {
                    List<FlashFileInfo> flashFiles = new List<FlashFileInfo>();
                    FlashConfigInfo flashCfg = null;
                    foreach(XmlNode valueNode in node) {
                        if(valueNode.Name == "Data") {
                            if(flashFiles.Count != 0)
                                throw CreateException("Invalid XML format. Duplicate \"Files\" in a \"Flash\" tag.");
                            flashFiles = ReadFlashFiles(valueNode);
                        }
                        else if(valueNode.Name == "Config") {
                            if(flashCfg != null)
                                throw CreateException("Invalid XML format. Duplicate \"Config\" in a \"Flash\" tag.");
                            flashCfg = ReadFlashCfg(valueNode);
                        }
                    }
                    string flashHeaderName = ReadAttribute(node, "Title");
                    EcuFlashInfo flashInfo = new EcuFlashInfo() {
                        Name = flashHeaderName,
                        Files = flashFiles,
                        Config = flashCfg,
                    };
                    flashes.Add(flashInfo);
                }
                else if(node.Name == "VFlashFile") {
                    string path = node.InnerText;
                    if(File.Exists(path)) {
                        List<EcuFlashInfo> ecus = new ConfigReader(path).GetEcuFlashInfo();
                        foreach(EcuFlashInfo ecu in ecus)
                            flashes.Add(ecu);
                    }
                }
            }

            ecuFlashInfo = flashes;
        }

        private Exception CreateException(string msg) {
            throw new Exception("An error occurred while reading \"" + Path.GetFileName(configPath) + "\" file. " + msg);
        }

        private string ReadAttribute(XmlNode xmlNode, string attributeName) {
            if(xmlNode.Attributes == null)
                return "";
            XmlNode node = xmlNode.Attributes.GetNamedItem(attributeName);
            if(node == null)
                return "";
            return node.Value;
        }

        private List<FlashFileInfo> ReadFlashFiles(XmlNode xmlNode) {
            List<FlashFileInfo> ret = new List<FlashFileInfo>();
            foreach(XmlNode node in xmlNode.ChildNodes) {
                if(node.Name == "File")
                    ret.Add(ReadFlashFirmware(node));
            }
            return ret;
        }

        private string TryGetRelativePath(string path) {
            path = path.Replace("/", "\\");
            string currentFolder = Directory.GetCurrentDirectory().Replace("/", "\\") + "\\";
            currentFolder = currentFolder.Replace("\\\\", "\\");
            if(path.IndexOf(currentFolder) == 0)
                return path.Replace(currentFolder, "");
            return path;
        }

        private FlashFileInfo ReadFlashFirmware(XmlNode xmlNode) {
            string fileName = "";
            string filePath = "";
            string crcPath = "";
            int isDriver = -1;
            foreach(XmlAttribute attribute in xmlNode.Attributes) {
                if(attribute.Name == "Label") {
                    if(fileName != "")
                        throw CreateException("Invalid XML format. Duplicate \"Label\" attribute.");
                    fileName = attribute.Value;
                }
                else if(attribute.Name == "Path") {
                    if(filePath != "")
                        throw CreateException("Invalid XML format. Duplicate \"Path\" attribute.");
                    filePath = attribute.Value;
                }
                else if(attribute.Name == "IsDriver") {
                    if(isDriver > -1)
                        throw CreateException("Invalid XML format. Duplicate \"IsDriver\" attribute.");
                    isDriver = (attribute.Value.ToLower() == "true") ? 1 : 0;
                }
                else if(attribute.Name == "CRC" || attribute.Name == "Signature") {
                    if(crcPath != "")
                        throw CreateException("Invalid XML format. Duplicate \"Path\" attribute.");
                    crcPath = attribute.Value;
                }
            }
            if(filePath != "")
                filePath = TryGetRelativePath(filePath);
            if(crcPath != "")
                crcPath = TryGetRelativePath(crcPath);
            FlashFileInfo ret = new FlashFileInfo() {
                IsUsed = true,
                FileName = fileName,
                FilePath = filePath,
                CrcPath = crcPath,
                IsDriver = isDriver == 1 ? true : false,
            };
            return ret;
        }

        private void SetValue(FlashConfigInfo cfg, Dictionary<string, string> value) {
            if(value.ContainsKey("Device"))
                cfg.Device = value["Device"];
            if(value.ContainsKey("Channel"))
                cfg.Channel = value["Channel"];
            if(value.ContainsKey("CanType"))
                cfg.CanType = value["CanType"].ToUpper();
            if(value.ContainsKey("Bitrate"))
                cfg.Bitrate = Number.ForceToInt(value["Bitrate"]);
            if(value.ContainsKey("TxId"))
                cfg.TxId = Number.ForceToInt(value["TxId"]);
            if(value.ContainsKey("RxId"))
                cfg.RxId = Number.ForceToInt(value["RxId"]);
            if(value.ContainsKey("FunctionalId"))
                cfg.FunctionalId = Number.ForceToInt(value["FunctionalId"]);
            if(value.ContainsKey("STmin")) {
                double ret;
                if(Double.TryParse(value["STmin"], out ret))
                    cfg.STmin = ret;
            }
            if(value.ContainsKey("P2"))
                cfg.P2Value = Number.ForceToInt(value["P2"]);
            if(value.ContainsKey("Timeout"))
                cfg.Timeout = Number.ForceToInt(value["Timeout"]);
            if(value.ContainsKey("SecurityLevel")) {
                int temp = Number.ForceToInt(value["SecurityLevel"]);
                if(temp > 0)
                    cfg.SecurityLevel = temp;
            }
            if(value.ContainsKey("SeedKeyDll"))
                cfg.SeedKeyDll = value["SeedKeyDll"];
            if(value.ContainsKey("FlashActionsPath"))
                cfg.FlashActionsPath = value["FlashActionsPath"];
        }

        private FlashConfigInfo ReadFlashCfg(XmlNode xmlNode) {
            Dictionary<string, string> value = new Dictionary<string, string>();

            foreach(XmlNode node in xmlNode) {
                if(value.ContainsKey(node.Name))
                    throw CreateException("Invalid XML format. Duplicate \"" + node.Name + "\" attribute.");
                value.Add(node.Name, node.InnerText);
            }

            if(value.ContainsKey("SeedKeyDll")) {
                if(value["SeedKeyDll"] != "")
                    value["SeedKeyDll"] = TryGetRelativePath(value["SeedKeyDll"]);
            }
            if(value.ContainsKey("FlashActionsPath")) {
                if(value["FlashActionsPath"] != "")
                    value["FlashActionsPath"] = TryGetRelativePath(value["FlashActionsPath"]);
            }

            FlashConfigInfo flashCfg = new FlashConfigInfo();
            SetValue(flashCfg, value);

            return flashCfg;
        }

        public List<EcuFlashInfo> GetEcuFlashInfo() {
            return ecuFlashInfo;
        }

        public string Title {
            get; private set;
        }
    }
}
