using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using VFlashDevices;
using vxlapi_NET;
using static vxlapi_NET.XLClass;

namespace VectorDevices {
    public class VectorCan : AbstractCanDevice {
        private XLDriver driver;
        private int portHandle;
        private Thread rxThread;
        private xl_channel_config vectorCanChannel;

        public override event CanDataReceivedEventHandler DataReceived;

        public VectorCan() {
            Bitrate = 500000;
            CanType = CanType.CAN;
            driver = new XLDriver();
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

        private static List<xl_channel_config> GetVectorCanChannels(XLDriver xlDriver) {
            xl_driver_config driverConfig = new xl_driver_config();

            XLDefine.XL_Status status = xlDriver.XL_GetDriverConfig(ref driverConfig);
            if(status != XLDefine.XL_Status.XL_SUCCESS)
                return null;

            List<xl_channel_config> channels = new List<xl_channel_config>();
            for(int i = 0; i < driverConfig.channel.Length; i++) {
                xl_channel_config channel = driverConfig.channel[i];
                if(channel.name != null &&
                    channel.hwType != XLDefine.XL_HardwareType.XL_HWTYPE_VIRTUAL &&
                    driverConfig.channel[i].transceiverName.Contains("CAN")) {
                    channels.Add(driverConfig.channel[i]);
                }
            }
            if(channels.Count == 0)
                return null;

            return channels;
        }

        private static string TrimString(string text) {
            StringBuilder ret = new StringBuilder();
            for(int i = 0; i < text.Length; i++)
                if(text[i] != 0)
                    ret.Append(text[i]);
            return ret.ToString().Trim();
        }

        private xl_channel_config GetVectorCanChannel(string channelName) {
            List<xl_channel_config> vectorChannels = GetVectorCanChannels(driver);
            string temp = TrimString(channelName).ToLower();
            foreach(xl_channel_config canChannel in vectorChannels) {
                if(temp == TrimString(canChannel.name).ToLower())
                    return canChannel;
            }
            return null;
        }

        public new static List<string> GetChannels() {
            XLDriver xlDriver = new XLDriver();
            XLDefine.XL_Status status = xlDriver.XL_OpenDriver();
            if(status != XLDefine.XL_Status.XL_SUCCESS)
                return null;

            List<xl_channel_config> vectorCanChannels = GetVectorCanChannels(xlDriver);
            if(vectorCanChannels == null)
                return null;

            List<string> channels = new List<string>();
            foreach(xl_channel_config canChannel in vectorCanChannels)
                channels.Add(TrimString(canChannel.name));

            return channels;
        }

        private bool CanConnect() {
            XLDefine.XL_Status status = driver.XL_OpenDriver();
            if(status != XLDefine.XL_Status.XL_SUCCESS) {
                driver.XL_CloseDriver();
                return false;
            }

            vectorCanChannel = GetVectorCanChannel(Channel);
            if(vectorCanChannel == null) {
                driver.XL_CloseDriver();
                return false;
            }

            ulong permissionMask = vectorCanChannel.channelMask;
            status = driver.XL_OpenPort(ref portHandle, "", vectorCanChannel.channelMask, ref permissionMask, 524288, XLDefine.XL_InterfaceVersion.XL_INTERFACE_VERSION, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN);
            if(status != XLDefine.XL_Status.XL_SUCCESS) {
                driver.XL_CloseDriver();
                return false;
            }

            status = driver.XL_CanSetChannelBitrate(portHandle, vectorCanChannel.channelMask, (uint)Bitrate);
            if(status != XLDefine.XL_Status.XL_SUCCESS) {
                driver.XL_ClosePort(portHandle);
                driver.XL_CloseDriver();
                return false;
            }

            status = driver.XL_ActivateChannel(portHandle, vectorCanChannel.channelMask, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN, XLDefine.XL_AC_Flags.XL_ACTIVATE_RESET_CLOCK);
            if(status != XLDefine.XL_Status.XL_SUCCESS) {
                driver.XL_ClosePort(portHandle);
                driver.XL_CloseDriver();
                return false;
            }

            rxThread = new Thread(new ThreadStart(CanRXHandler)) { IsBackground = true };
            rxThread.Start();

            return true;
        }

        private bool CanFDConnect() {
            XLDefine.XL_Status status = driver.XL_OpenDriver();
            if(status != XLDefine.XL_Status.XL_SUCCESS) {
                driver.XL_CloseDriver();
                return false;
            }

            vectorCanChannel = GetVectorCanChannel(Channel);
            if(vectorCanChannel == null) {
                driver.XL_CloseDriver();
                return false;
            }

            ulong permissionMask = vectorCanChannel.channelMask;
            status = driver.XL_OpenPort(ref portHandle, "", vectorCanChannel.channelMask, ref permissionMask, 16000, XLDefine.XL_InterfaceVersion.XL_INTERFACE_VERSION_V4, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN);
            if(status != XLDefine.XL_Status.XL_SUCCESS) {
                driver.XL_ClosePort(portHandle);
                driver.XL_CloseDriver();
                return false;
            }

            XLcanFdConf canFdConf = new XLcanFdConf();

            canFdConf.arbitrationBitRate = (uint)Bitrate;
            canFdConf.tseg1Abr = 6;
            canFdConf.tseg2Abr = 3;
            canFdConf.sjwAbr = 2;

            canFdConf.dataBitRate = canFdConf.arbitrationBitRate * 2;
            canFdConf.tseg1Dbr = 6;
            canFdConf.tseg2Dbr = 3;
            canFdConf.sjwDbr = 2;
            canFdConf.options = 0;

            status = driver.XL_CanFdSetConfiguration(portHandle, vectorCanChannel.channelMask, canFdConf);
            if(status != XLDefine.XL_Status.XL_SUCCESS) {
                driver.XL_ClosePort(portHandle);
                driver.XL_CloseDriver();
                return false;
            }

            status = driver.XL_ActivateChannel(portHandle, vectorCanChannel.channelMask, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN, XLDefine.XL_AC_Flags.XL_ACTIVATE_RESET_CLOCK);
            if(status != XLDefine.XL_Status.XL_SUCCESS) {
                driver.XL_ClosePort(portHandle);
                driver.XL_CloseDriver();
                return false;
            }

            rxThread = new Thread(new ThreadStart(CanFDRXThread)) { IsBackground = true };
            rxThread.Start();

            return true;
        }

        public override bool Connect() {
            portHandle = -1;
            if(CanType == CanType.CAN_FD)
                return CanFDConnect();
            return CanConnect();
        }

        private void CanRXHandler() {
            xl_event receivedEvent = new xl_event();

            while(true) {
                XLDefine.XL_Status xlStatus = XLDefine.XL_Status.XL_SUCCESS;

                while(xlStatus != XLDefine.XL_Status.XL_ERR_QUEUE_IS_EMPTY) {
                    xlStatus = driver.XL_Receive(portHandle, ref receivedEvent);
                    if(xlStatus != XLDefine.XL_Status.XL_SUCCESS)
                        continue;

                    else if(receivedEvent.tag == XLDefine.XL_EventTags.XL_RECEIVE_MSG) {
                        if((receivedEvent.tagData.can_Msg.flags & XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_OVERRUN) != 0) {
                            //Console.WriteLine("-- XL_CAN_MSG_FLAG_OVERRUN --");
                        }

                        if((receivedEvent.tagData.can_Msg.flags & XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_ERROR_FRAME) == XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_ERROR_FRAME) {
                            //Console.WriteLine("ERROR FRAME");
                        }
                        else if((receivedEvent.tagData.can_Msg.flags & XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_REMOTE_FRAME) == XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_REMOTE_FRAME) {
                            //Console.WriteLine("REMOTE FRAME");
                        }
                        else {
                            //Console.WriteLine(driver.XL_GetEventString(receivedEvent));
                            DataReceived?.Invoke((int)receivedEvent.tagData.can_Msg.id, receivedEvent.tagData.can_Msg.data);
                        }
                    }
                }

                Thread.Sleep(1);
            }
        }

        private void CanFDRXThread() {
            XLcanRxEvent receivedEvent = new XLcanRxEvent();

            while(true) {
                XLDefine.XL_Status xlStatus = XLDefine.XL_Status.XL_SUCCESS;

                while(xlStatus != XLDefine.XL_Status.XL_ERR_QUEUE_IS_EMPTY) {
                    xlStatus = driver.XL_CanReceive(portHandle, ref receivedEvent);
                    if(xlStatus != XLDefine.XL_Status.XL_SUCCESS)
                        continue;

                    if(receivedEvent.tag == XLDefine.XL_CANFD_RX_EventTags.XL_CAN_EV_TAG_RX_OK) {
                        Console.WriteLine(driver.XL_CanGetEventString(receivedEvent));
                        DataReceived?.Invoke((int)receivedEvent.tagData.canRxOkMsg.canId, receivedEvent.tagData.canRxOkMsg.data);
                    }
                }

                Thread.Sleep(1);
            }
        }

        private bool CanTransmit(int txId, byte[] data) {
            xl_event xlEvent = new xl_event();
            xlEvent.tagData.can_Msg.id = (uint)txId;
            xlEvent.tagData.can_Msg.dlc = (ushort)data.Length;
            xlEvent.flags = XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_NONE;
            Array.Copy(data, xlEvent.tagData.can_Msg.data, data.Length);
            xlEvent.tag = XLDefine.XL_EventTags.XL_TRANSMIT_MSG;
            XLDefine.XL_Status ret = driver.XL_CanTransmit(portHandle, vectorCanChannel.channelMask, xlEvent);
            return ret == XLDefine.XL_Status.XL_SUCCESS;
        }

        public bool CanFDTransmit(int txId, byte[] data) {
            if(data.Length > 8)
                throw new Exception("Data length must be less than or equal to 8");
            XLcanTxEvent xlEvent = new XLcanTxEvent();
            xlEvent.tag = XLDefine.XL_CANFD_TX_EventTags.XL_CAN_EV_TAG_TX_MSG;
            xlEvent.tagData.canId = (uint)txId;
            xlEvent.tagData.dlc = XLDefine.XL_CANFD_DLC.DLC_CAN_CANFD_8_BYTES;
            xlEvent.tagData.msgFlags = XLDefine.XL_CANFD_TX_MessageFlags.XL_CAN_TXMSG_FLAG_BRS | XLDefine.XL_CANFD_TX_MessageFlags.XL_CAN_TXMSG_FLAG_EDL;
            Array.Copy(data, xlEvent.tagData.data, data.Length);
            uint messageCounterSent = 0;
            XLDefine.XL_Status ret = driver.XL_CanTransmitEx(portHandle, vectorCanChannel.channelMask, ref messageCounterSent, xlEvent);
            return ret == XLDefine.XL_Status.XL_SUCCESS;
        }

        public override bool DataTransmit(int txId, byte[] data) {
            if(CanType == CanType.CAN_FD)
                return CanFDTransmit(txId, data);
            return CanTransmit(txId, data);
        }

        public override void Disconnect() {
            if(rxThread != null)
                rxThread.Abort();
            if(portHandle != -1)
                driver.XL_ClosePort(portHandle);
            driver.XL_CloseDriver();
        }
    }
}
