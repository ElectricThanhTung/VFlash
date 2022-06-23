using Kvaser.CanLib;
using System;
using System.Collections.Generic;
using System.Threading;
using VFlashDevices;

namespace KvaserDevices {
    public class KvaserCan : AbstractCanDevice {
        private class ChannelInfo {
            public string Name;
            public int ChannelId;
        };

        private int channelHwnd;
        private Thread rxThread;

        public override event CanDataReceivedEventHandler DataReceived;

        public KvaserCan() {
            channelHwnd = -1;
            this.CanType = CanType.CAN;
        }

        static KvaserCan() {
            Canlib.canInitializeLibrary();
        }

        public override int Bitrate {
            get; set;
        }

        private int setBitRate(int bitrate) {
            switch(bitrate) {
                case 1000000:
                    return Canlib.canBITRATE_1M;
                case 500000:
                    return Canlib.canBITRATE_500K;
                case 250000:
                    return Canlib.canBITRATE_250K;
                case 125000:
                    return Canlib.canBITRATE_125K;
                case 100000:
                    return Canlib.canBITRATE_100K;
                case 62000:
                    return Canlib.canBITRATE_62K;
                case 50000:
                    return Canlib.canBITRATE_50K;
                case 83000:
                    return Canlib.canBITRATE_83K;
                case 10000:
                    return Canlib.canBITRATE_10K;
                default:
                    throw new Exception("Bit rate of " + bitrate + " not supported, supported are 10k, 50k, 62k, 83k, 100k, 125k, 250k, 500k, 1MB");
            }
        }

        private int setBitRateFd(int bitrate) {
            switch(bitrate) {
                case 500000:
                    return Canlib.canFD_BITRATE_500K_80P;
                case 1000000:
                    return Canlib.canFD_BITRATE_1M_80P;
                case 2000000:
                    return Canlib.canFD_BITRATE_2M_80P;
                case 4000000:
                    return Canlib.canFD_BITRATE_4M_80P;
                case 8000000:
                    return Canlib.canFD_BITRATE_8M_80P;
                default:
                    throw new Exception("Bit rate of " + bitrate + " not supported, supported are 500k, 1MB, 2MB, 4MB, 8MB.");
            }
        }

        public override string Channel {
            get; set;
        }

        public override CanType CanType {
            get; set;
        }

        private static List<ChannelInfo> GetChannelNames() {
            int channelCount;

            //Canlib.canStatus status = Canlib.canGetNumberOfChannels(out channelCount);
            Canlib.canStatus status = Canlib.canEnumHardwareEx(out channelCount);
            if(status != Canlib.canStatus.canOK) {
                throw new Exception("Error during get channel information of Kvaser (" + status.ToString() + ")");
            }

            List<ChannelInfo> channels = new List<ChannelInfo>();
            for(int index = 0; index < channelCount; index++) {
                object channelName, type;
                Canlib.canGetChannelData(index, Canlib.canCHANNELDATA_DEVDESCR_ASCII, out channelName);
                Canlib.canGetChannelData(index, Canlib.canCHANNELDATA_CARD_TYPE, out type);

                if(channelName != null && type != null) {
                    // Ignore virtual channel
                    if(Convert.ToInt32(type) != Canlib.canHWTYPE_VIRTUAL) {
                        channels.Add(new ChannelInfo() {
                            Name = channelName.ToString(),
                            ChannelId = index,
                        });
                    }
                }
            }

            return channels;
        }

        public new static List<string> GetChannels() {
            try {
                List<ChannelInfo> channels = GetChannelNames();

                // Remove all null channel means virtual channel
                List<string> channelList = new List<string>();
                foreach(ChannelInfo channel in channels)
                    channelList.Add(channel.Name);

                return channelList;
            }
            catch {
                return null;
            }
        }

        public int GetCanChannelHwnd(string channelName) {
            List<ChannelInfo> channels = GetChannelNames();
            channelName = channelName.ToLower();
            foreach(ChannelInfo channel in channels) {
                if(channel.Name.ToLower() == channelName)
                    return Canlib.canOpenChannel(channel.ChannelId, Canlib.canOPEN_EXCLUSIVE);
            }
            throw new Exception("Channel name " + Channel + " is not valid");
        }

        public override bool Connect() {
            Canlib.canStatus status;

            // Open channel
            channelHwnd = GetCanChannelHwnd(Channel);
            if(channelHwnd < 0)
                return false;

            // Set bit rate on can handle
            if(CanType == CanType.CAN)
                status = Canlib.canSetBusParams(channelHwnd, setBitRate(this.Bitrate), 0, 0, 0, 0);
            else
                status = Canlib.canSetBusParamsFd(channelHwnd, setBitRateFd(this.Bitrate), 0, 0, 0);
            if(status != Canlib.canStatus.canOK)
                return false;

            // Go on bus
            status = Canlib.canBusOn(channelHwnd);
            if(status != Canlib.canStatus.canOK)
                return false;

            // Start receiver thread
            rxThread = new Thread(this.rxThreadHandler) {
                IsBackground = true
            };
            rxThread.Start();

            return true;
        }

        public override bool DataTransmit(int txId, byte[] data) {
            if(channelHwnd < 0)
                return false;

            Canlib.canStatus status = Canlib.canWriteWait(channelHwnd, txId, data, data.Length, Canlib.canMSG_STD, 0xFFFFFFFF);
            if(status != Canlib.canStatus.canOK)
                return false;

            return true;
        }

        public override void Disconnect() {
            if(channelHwnd >= 0) {
                // Stop receiver thread
                if(rxThread != null)
                    rxThread.Abort();

                // Bus off and close
                Canlib.canBusOff(channelHwnd);
                Canlib.canClose(channelHwnd);

                channelHwnd = -1;
            }
        }

        private void rxThreadHandler() {
            int id = 0;
            byte[] data = new byte[8];
            int dlc;
            int flags;
            long time;

            while(true) {
                while(Canlib.canRead(channelHwnd, out id, data, out dlc, out flags, out time) == Canlib.canStatus.canOK)
                    DataReceived?.Invoke(id, data);
                Thread.Sleep(5);
            }
        }
    }
}
