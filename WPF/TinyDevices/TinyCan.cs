using System;
using System.Collections.Generic;
using System.Threading;
using TinyDotNet;
using VFlashDevices;

namespace TinyDevices {
    public class TinyCan : AbstractCanDevice {
        private Tiny tiny;
        private Thread rxThread;

        public override event CanDataReceivedEventHandler DataReceived;

        public TinyCan() {
            tiny = new Tiny();
        }

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
            List<TinyDevice> devices = Tiny.GetDevices();
            if(devices == null)
                return null;
            List<string> channels = new List<string>();
            foreach(TinyDevice device in devices)
                channels.Add(device.Name);
            return channels;
        }

        private TinyDevice GetDevice(string deviceName) {
            List<TinyDevice> devices = Tiny.GetDevices();
            List<string> channels = new List<string>();
            foreach(TinyDevice device in devices) {
                if(device.Name == deviceName)
                    return device;
            }
            throw new Exception("Can't find device " + deviceName);
        }

        public override bool Connect() {
            if(CanType == CanType.CAN_FD)
                throw new Exception("Tiny device not support CAN FD");
            lock(tiny) {
                if(!tiny.Open(GetDevice(Channel)))
                    return false;
                if(!tiny.Disconnect())
                    return false;
                if(!tiny.SetBitrate(Bitrate)) {
                    tiny.Close();
                    return false;
                }
                if(Bitrate != tiny.GetBitrate()) {
                    tiny.Close();
                    throw new Exception("Tiny device not support bitrate " + Bitrate);
                }
                if(!tiny.Connect()) {
                    tiny.Close();
                    return false;
                }
            }
            rxThread = new Thread(new ThreadStart(CanRXHandler)) { IsBackground = true };
            rxThread.Start();
            return true;
        }

        public override bool DataTransmit(int txId, byte[] data) {
            TinyCanMsg msg = new TinyCanMsg(data);
            msg.ID = txId;
            msg.RTR = TinyCanRTR.RTR_DATA;
            msg.IDE = TinyCanIDE.IDE_STD;
            lock(tiny) {
                if(!tiny.TransmitData(msg))
                    return false;
            }
            TinyCanStatus status;
            //do {
            //    lock(tiny)
            //        status = tiny.TransmitStatus();
            //    if(status == TinyCanStatus.TINY_CAN_ERROR)
            //        return false;
            //} while(status == TinyCanStatus.TINY_CAN_BUSY);
            return true;
        }

        private void CanRXHandler() {
            TinyCanMsg msg;
            while(true) {
                do {
                    lock(tiny)
                        msg = tiny.ReadData();
                    if(msg != null && msg.RTR == TinyCanRTR.RTR_DATA) {
                        byte[] data = new byte[msg.DLC];
                        for(int i = 0; i < data.Length; i++)
                            data[i] = msg[i];
                        DataReceived?.Invoke(msg.ID, data);
                    }
                } while(msg != null);
                Thread.Sleep(2);
            }
        }

        public override void Disconnect() {
            if(rxThread != null)
                rxThread.Abort();
            lock(tiny) {
                tiny.Disconnect();
                tiny.Close();
            }
        }
    }
}
