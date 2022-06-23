using Ixxat.Vci4;
using System;
using System.Collections;
using System.Collections.Generic;
using VFlashDevices;

namespace IxxatDevices {
    public class IxxatCan : AbstractCanDevice {
        AbstractCanDevice canDevice;

        public override event CanDataReceivedEventHandler DataReceived;

        public override int Bitrate {
            get; set;
        }

        public override string Channel {
            get; set;
        }

        public override CanType CanType {
            get; set;
        }

        public new static List<string> GetChannels() {
            try {
                IVciDeviceManager deviceManager = VciServer.Instance().DeviceManager;
                IVciDeviceList deviceList = deviceManager.GetDeviceList();
                IEnumerator deviceEnum = deviceList.GetEnumerator();

                List<string> channels = new List<string>();

                while(deviceEnum.MoveNext()) {
                    IVciDevice device = deviceEnum.Current as IVciDevice;
                    if(device.Equipment[0].BusType == VciBusType.Can)
                        channels.Add(device.Description);
                }

                DisposeVciObject(deviceManager);
                DisposeVciObject(deviceEnum);
                DisposeVciObject(deviceList);

                return channels;
            }
            catch {
                return null;
            }
        }

        private static void DisposeVciObject(object obj) {
            if(null != obj) {
                IDisposable dispose = obj as IDisposable;
                if(null != dispose)
                    dispose.Dispose();
            }
        }

        public override bool Connect() {
            if(CanType == CanType.CAN)
                canDevice = new IxxatClassicalCan();
            else
                canDevice = new IxxatCanFd();
            canDevice.Bitrate = Bitrate;
            canDevice.Channel = Channel;
            canDevice.DataReceived += DataReceived;
            return canDevice.Connect();
        }

        public override bool DataTransmit(int txId, byte[] data) {
            return canDevice.DataTransmit(txId, data);
        }

        public override void Disconnect() {
            canDevice.DataReceived -= DataReceived;
            canDevice.Disconnect();
        }
    }
}
