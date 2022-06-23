using Ixxat.Vci4;
using Ixxat.Vci4.Bal;
using Ixxat.Vci4.Bal.Can;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using VFlashDevices;

namespace IxxatDevices {
    internal class IxxatClassicalCan : AbstractCanDevice {
        private IVciDevice mDevice;
        private ICanControl mCanCtl;
        private ICanChannel mCanChn;
        private ICanScheduler mCanSched;
        private ICanMessageWriter mWriter;
        private ICanMessageReader mReader;
        private Thread rxThread;
        private AutoResetEvent mRxEvent;
        private AutoResetEvent mTxEvent;

        public override event CanDataReceivedEventHandler DataReceived;

        public IxxatClassicalCan() {
            Bitrate = 500000;
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

        public override int Bitrate {
            get; set;
        }

        public override string Channel {
            get; set;
        }

        public override CanType CanType {
            get {
                return CanType.CAN;
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

        private CanBitrate GetCanBitrate(int bitrate) {
            Dictionary<int, CanBitrate> bitrates = new Dictionary<int, CanBitrate>() {
                { 10000, CanBitrate.Cia10KBit },
                { 20000, CanBitrate.Cia20KBit },
                { 50000, CanBitrate.Cia50KBit },
                { 100000, CanBitrate._100KBit },
                { 125000, CanBitrate.Cia125KBit },
                { 250000, CanBitrate.Cia250KBit },
                { 500000, CanBitrate.Cia500KBit },
                { 800000, CanBitrate.Cia800KBit },
                { 1000000, CanBitrate.Cia1000KBit },
            };
            if(!bitrates.ContainsKey(bitrate))
                throw new Exception("Unable set Bitrate value to " + bitrate);
            return bitrates[bitrate];
        }

        private bool InitSocket(Byte canNo) {
            IBalObject bal = null;
            try {
                bal = mDevice.OpenBusAccessLayer();
                mCanChn = bal.OpenSocket(canNo, typeof(ICanChannel)) as ICanChannel;
                mCanSched = bal.OpenSocket(canNo, typeof(ICanScheduler)) as ICanScheduler;
                mCanChn.Initialize(32768, 128, false);
                mReader = mCanChn.GetMessageReader();
                mReader.Threshold = 1;
                mRxEvent = new AutoResetEvent(false);
                mReader.AssignEvent(mRxEvent);
                mWriter = mCanChn.GetMessageWriter();
                mWriter.Threshold = 1;
                mTxEvent = new AutoResetEvent(false);
                mWriter.AssignEvent(mTxEvent);
                mCanChn.Activate();
                mCanCtl = bal.OpenSocket(canNo, typeof(ICanControl)) as ICanControl;
                mCanCtl.InitLine(CanOperatingModes.Standard | CanOperatingModes.Extended | CanOperatingModes.ErrFrame, GetCanBitrate(Bitrate));
                mCanCtl.SetAccFilter(CanFilter.Std, (uint)CanAccCode.All, (uint)CanAccMask.All);
                mCanCtl.SetAccFilter(CanFilter.Ext, (uint)CanAccCode.All, (uint)CanAccMask.All);
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
                    ICanMessage canMessage;
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
            ICanMessage canMsg = (ICanMessage)factory.CreateMsg(typeof(ICanMessage));
            canMsg.TimeStamp = 0;
            canMsg.Identifier = (uint)txId;
            canMsg.FrameType = CanMsgFrameType.Data;
            canMsg.SelfReceptionRequest = false;
            canMsg.DataLength = (byte)data.Length;
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
