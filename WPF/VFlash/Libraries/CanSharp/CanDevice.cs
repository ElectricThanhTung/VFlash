using System;
using VFlashDevices;

namespace CanSharp {
    public class CanDevice {
        private AbstractCanDevice instance;
        private int connectedCount = 0;
        public event CanDataReceivedEventHandler DataReceived;

        public CanDevice(string name, string icon, Type deviceType) {
            if(!deviceType.IsSubclassOf(typeof(AbstractCanDevice)))
                throw new Exception(deviceType.Name + " must be base on AbstractCanDevice");
            Name = name;
            Icon = icon;
            DeviceType = deviceType;
            instance = (AbstractCanDevice)Activator.CreateInstance(DeviceType);
            instance.DataReceived += OnDataReceived;
        }

        public void OnDataReceived(int rxId, byte[] data) {
            DataReceived?.Invoke(this, rxId, data);
        }

        public string Name {
            get; private set;
        }

        public string Icon {
            get; private set;
        }

        public Type DeviceType {
            get; private set;
        }

        public int Bitrate {
            get {
                return instance.Bitrate;
            }
            set {
                if(connectedCount != 0)
                    throw new Exception("Can't change Bitrate value while conncted!");
                instance.Bitrate = value;
            }
        }

        public string Channel {
            get {
                return instance.Channel;
            }
            set {
                if(connectedCount != 0)
                    throw new Exception("Can't change CanType value while conncted!");
                instance.Channel = value;
            }
        }

        public CanType CanType {
            get {
                return instance.CanType;
            }
            set {
                if(connectedCount != 0)
                    throw new Exception("Can't change CanType value while conncted!");
                instance.CanType = value;
            }
        }

        public bool Connect() {
            if(connectedCount == 0) {
                if(!instance.Connect())
                    return false;
            }
            connectedCount++;
            return true;
        }

        public void Disconnect() {
            if(--connectedCount == 0)
                instance.Disconnect();
        }

        public bool DataTransmit(int txId, byte[] data) {
            lock(instance)
                return instance.DataTransmit(txId, data);
        }
    }
}
