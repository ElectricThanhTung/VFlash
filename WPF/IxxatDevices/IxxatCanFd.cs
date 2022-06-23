using Ixxat.Vci4;
using Ixxat.Vci4.Bal;
using Ixxat.Vci4.Bal.Can;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using VFlashDevices;

namespace IxxatDevices {
    internal class IxxatCanFd : AbstractCanDevice {
        private IVciDevice mDevice;
        private ICanControl2 mCanCtl;
        private ICanChannel2 mCanChn;
        private ICanScheduler2 mCanSched;
        private ICanMessageWriter mWriter;
        private ICanMessageReader mReader;
        private Thread rxThread;
        private AutoResetEvent mRxEvent;
        private AutoResetEvent mTxEvent;

        public override event CanDataReceivedEventHandler DataReceived;

        public IxxatCanFd() {
            Bitrate = 500000;
        }

        public override int Bitrate {
            get; set;
        }

        public override string Channel {
            get; set;
        }

        public override CanType CanType {
            get {
                return CanType.CAN_FD;
            }
            set {
                throw new Exception("Can't change CanType for IxxatClassicalCan");
            }
        }

        private static void DisposeVciObject(object obj) {
            if(null != obj) {
                IDisposable dispose = obj as IDisposable;
                if(null != dispose)
                    dispose.Dispose();
            }
        }

        private bool SelectDevice(string deviceName) {
            IVciDeviceManager deviceManager = null;
            IVciDeviceList deviceList = null;
            IEnumerator deviceEnum = null;

            deviceName = deviceName.ToLower();

            try {
                deviceManager = VciServer.Instance().DeviceManager;
                deviceList = deviceManager.GetDeviceList();
                deviceEnum = deviceList.GetEnumerator();
                while(deviceEnum.MoveNext()) {
                    IVciDevice device = deviceEnum.Current as IVciDevice;
                    if(device.Description.ToLower() == deviceName) {
                        mDevice = device;
                        return true;
                    }
                }
            }
            catch {
                DisposeVciObject(deviceManager);
                DisposeVciObject(deviceEnum);
                DisposeVciObject(deviceList);
                Disconnect();
            }
            return false;
        }

        private CanBitrate2 GetCanBitrate2(int bitrate) {
            Dictionary<int, CanBitrate2> bitrates = new Dictionary<int, CanBitrate2>() {
                {250000, CanBitrate2.CANFD250KBit },
                {500000, CanBitrate2.CANFD500KBit },
                {833333, CanBitrate2.CANFD833KBit },
                {1000000, CanBitrate2.CANFD1000KBit },
                {1538461, CanBitrate2.CANFD1538KBit },
                {2000000, CanBitrate2.CANFD2000KBit },
                {4000000, CanBitrate2.CANFD4000KBit },
                {5000000, CanBitrate2.CANFD5000KBit },
                {6666666, CanBitrate2.CANFD6667KBit },
                {8000000, CanBitrate2.CANFD8000KBit },
                {10000000, CanBitrate2.CANFD10000KBit },
            };
            if(!bitrates.ContainsKey(bitrate))
                throw new Exception("Unable set Bitrate value to " + bitrate);
            return bitrates[bitrate];
        }

        private bool InitSocket(Byte canNo) {
            IBalObject bal = null;

            try {
                bal = mDevice.OpenBusAccessLayer();
                mCanChn = bal.OpenSocket(canNo, typeof(ICanChannel2)) as ICanChannel2;
                mCanSched = bal.OpenSocket(canNo, typeof(ICanScheduler2)) as ICanScheduler2;
                mCanChn.Initialize(1024, 128, 100, CanFilterModes.Pass, false);
                mReader = mCanChn.GetMessageReader();
                mReader.Threshold = 1;
                mRxEvent = new AutoResetEvent(false);
                mReader.AssignEvent(mRxEvent);
                mWriter = mCanChn.GetMessageWriter();
                mWriter.Threshold = 1;
                mTxEvent = new AutoResetEvent(false);
                mWriter.AssignEvent(mTxEvent);
                mCanChn.Activate();

                mCanCtl = bal.OpenSocket(canNo, typeof(ICanControl2)) as ICanControl2;

                try {
                    CanOperatingModes mode = CanOperatingModes.Standard | CanOperatingModes.Extended | CanOperatingModes.ErrFrame;
                    CanExtendedOperatingModes extendedMode = CanExtendedOperatingModes.ExtendedDataLength | CanExtendedOperatingModes.FastDataRate;
                    CanBitrate2 bitrate = GetCanBitrate2(Bitrate);
                    mCanCtl.InitLine(mode, extendedMode, CanFilterModes.Pass, 2048, CanFilterModes.Pass, 2048, bitrate, bitrate);
                }
                catch(Exception ex) {
                    throw new Exception("Cannot configure CANFD for this device", ex);
                }

                mCanCtl.StartLine();
                DisposeVciObject(bal);
                return true;
            }
            catch(Exception ex) {
                DisposeVciObject(bal);
                Disconnect();
                throw ex;
            }
        }

        public override bool Connect() {
            if(!SelectDevice(Channel))
                return false;
            if(!InitSocket(0))
                return false;
            rxThread = new Thread(rxThreadHandler) {
                IsBackground = true
            };
            rxThread.Start();
            return true;
        }

        private void rxThreadHandler() {
            while(true) {
                if(mRxEvent.WaitOne(10, false)) {
                    ICanMessage2 canMessage;
                    while(mReader.ReadMessage(out canMessage)) {
                        if(canMessage.FrameType != CanMsgFrameType.Data)
                            continue;

                        byte[] buff = new byte[canMessage.DataLength];
                        for(int i = 0; i < canMessage.DataLength; i++)
                            buff[i] = canMessage[i];

                        DataReceived?.Invoke((int)canMessage.Identifier, buff);
                    }
                }
            }
        }

        public override bool DataTransmit(int txId, byte[] data) {
            IMessageFactory factory = VciServer.Instance().MsgFactory;
            if(data.Length > 8)
                throw new Exception("Data length must be less than or equal to 8");
            ICanMessage2 canMsg = (ICanMessage2)factory.CreateMsg(typeof(ICanMessage2));
            canMsg.TimeStamp = 0;
            canMsg.Identifier = (uint)txId;
            canMsg.FrameType = CanMsgFrameType.Data;
            canMsg.DataLength = (byte)data.Length;
            canMsg.SelfReceptionRequest = false;
            canMsg.FastDataRate = true;
            canMsg.ExtendedDataLength = true;
            for(int i = 0; i < data.Length; i++)
                canMsg[i] = data[i];
            bool ret = mWriter.SendMessage(canMsg);
            mTxEvent.WaitOne();
            return ret;
        }

        public override void Disconnect() {
            if(rxThread != null && rxThread.IsAlive)
                rxThread.Abort();
            DisposeVciObject(mDevice);
            DisposeVciObject(mReader);
            DisposeVciObject(mWriter);
            DisposeVciObject(mCanCtl);
            DisposeVciObject(mCanChn);
            DisposeVciObject(mCanSched);
        }
    }
}
