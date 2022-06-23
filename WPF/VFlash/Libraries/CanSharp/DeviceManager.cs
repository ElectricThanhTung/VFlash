using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml;
using VFlashDevices;

namespace CanSharp {
    public static class DeviceManager {
        private class DeviceInfo {
            public string Name;
            public string Icon;
            public string Path;
            public Type DeviceType;
            public long LastTime = -100;
            public List<string> Channels;
        }

        private static Stopwatch stopwatch;
        private static string privatePathPriority;
        private static List<DeviceInfo> devicesInfo;

        static DeviceManager() {
            devicesInfo = LoadDeviceList();
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            stopwatch = Stopwatch.StartNew();
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
            AssemblyName assyName = new AssemblyName(args.Name);

            if(privatePathPriority != null) {
                string dllPath = Path.Combine(privatePathPriority, assyName.Name);
                if(!dllPath.EndsWith(".dll"))
                    dllPath = dllPath + ".dll";
                if(File.Exists(dllPath))
                    return Assembly.LoadFile(dllPath);
            }

            foreach(DeviceInfo deviceInfo in devicesInfo) {
                if(deviceInfo.Path != privatePathPriority) {
                    string dllPath = Path.Combine(deviceInfo.Path, assyName.Name);
                    if(!dllPath.EndsWith(".dll"))
                        dllPath = dllPath + ".dll";
                    if(File.Exists(dllPath))
                        return Assembly.LoadFile(dllPath);
                }
            }

            return null;
        }

        private static List<DeviceInfo> LoadDeviceList() {
            XmlDocument xmlDocument = new XmlDocument();
            string exePath = Assembly.GetEntryAssembly().Location;
            string exeFolder = Path.GetDirectoryName(exePath);
            string devicesCfgPath = Path.Combine(exeFolder, "devices", "devices.cfg");
            if(!File.Exists(devicesCfgPath))
                throw new Exception("Can't find file devices/devices.cfg");
            xmlDocument.Load(devicesCfgPath);

            if(xmlDocument.FirstChild.Name != "VFlashDevices")
                throw new Exception("Root node in devices.cfg file must be VFlashDevices");

            List<DeviceInfo> ret = new List<DeviceInfo>();

            string currentFolder = Environment.CurrentDirectory;
            Environment.CurrentDirectory = Path.GetDirectoryName(devicesCfgPath);
            foreach(XmlNode node in xmlDocument.DocumentElement.ChildNodes) {
                if(node.Name != "CanDevice")
                    continue;

                DeviceInfo deviceInfo = new DeviceInfo();
                deviceInfo.Name = (node.Attributes["Name"] != null && node.Attributes["Name"].Value.Trim() != "") ? node.Attributes["Name"].Value.Trim() : "No Name";
                deviceInfo.Icon = (node.Attributes["Icon"] != null && node.Attributes["Icon"].Value.Trim() != "") ? node.Attributes["Icon"].Value.Trim() : "";
                if(deviceInfo.Icon != "" && File.Exists(deviceInfo.Icon))
                    deviceInfo.Icon = Path.GetFullPath(deviceInfo.Icon);

                if(node.Attributes["Library"] != null) {
                    string libPath = Path.GetFullPath(node.Attributes["Library"].Value.Trim());
                    if(File.Exists(libPath)) {
                        if(node.Attributes["Class"] != null && node.Attributes["Class"].Value.Trim() != "") {
                            Assembly asm = Assembly.LoadFrom(libPath);
                            deviceInfo.DeviceType = asm.GetType(node.Attributes["Class"].Value.Trim());
                            deviceInfo.Path = Path.GetDirectoryName(libPath);
                        }
                    }
                }

                if(deviceInfo.DeviceType != null)
                    ret.Add(deviceInfo);
            }
            Environment.CurrentDirectory = currentFolder;

            return ret;
        }

        public static List<string> GetDeviceList() {
            List<string> devices = new List<string>();
            foreach(DeviceInfo device in devicesInfo) {
                string deviceName = device.Name;
                devices.Add(deviceName);
            }
            return devices;
        }

        public static CanDevice GetCanInstance(string deviceName, string channel, string canType, int bitrate) {
            deviceName = deviceName.ToLower();

            foreach(DeviceInfo device in devicesInfo) {
                if(deviceName == device.Name.ToLower()) {
                    privatePathPriority = device.Path;
                    CanDevice canDevice = new CanDevice(device.Name, device.Icon, device.DeviceType);
                    canDevice.Bitrate = bitrate;
                    canDevice.Channel = channel;
                    canDevice.CanType = (canType.ToUpper() == "CAN") ? CanType.CAN : CanType.CAN_FD;
                    return canDevice;
                }
            }

            return null;
        }

        public static string GetDeviceIcon(string devieName) {
            devieName = devieName.ToLower();
            foreach(DeviceInfo device in devicesInfo) {
                if(devieName == device.Name.ToLower())
                    return device.Icon;
            }
            return null;
        }

        public static List<string> GetCanChannels(string devieName) {
            devieName = devieName.ToLower();
            foreach(DeviceInfo device in devicesInfo) {
                if(devieName == device.Name.ToLower()) {
                    if(((ulong)(stopwatch.ElapsedMilliseconds - device.LastTime)) >= 100) {
                        device.LastTime = stopwatch.ElapsedMilliseconds;
                        MethodInfo getChannelsMethod = device.DeviceType.GetMethod("GetChannels");
                        if(!getChannelsMethod.IsStatic)
                            throw new Exception("Can't find static method \"GetChannels\" in " + device.DeviceType.Name + "!");
                        device.Channels = (List<string>)getChannelsMethod.Invoke(null, null);
                    }
                    return device.Channels;
                }
            }
            return null;
        }
    }
}
