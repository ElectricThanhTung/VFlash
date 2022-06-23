using System;
using System.Collections.Generic;

namespace VFlashDevices {
    public delegate void CanDataReceivedEventHandler(int rxId, byte[] data);

    public enum CanType {
        CAN,
        CAN_FD,
    }

    public abstract class AbstractCanDevice {
        public abstract event CanDataReceivedEventHandler DataReceived;

        public abstract int Bitrate {
            get; set;
        }

        public abstract string Channel {
            get; set;
        }

        public abstract CanType CanType {
            get; set;
        }

        public static List<string> GetChannels() => throw new NotImplementedException();

        public abstract bool Connect();

        public abstract bool DataTransmit(int txId, byte[] data);

        public abstract void Disconnect();
    }
}
