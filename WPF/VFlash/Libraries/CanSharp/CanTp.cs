using System;
using System.Diagnostics;
using System.Threading;

namespace CanSharp {
    public class CanTp : IDevice {
        private byte[] dataRxBuff;
        private int dataRxCount;
        private int dataRxSequence;
        private bool flowControlIsReceived;
        public event DataReceivedEventHandler DataReceived;

        public CanTp(CanDevice device, int txId, int rxId) {
            Device = device;

            TxId = txId;
            RxId = rxId;

            STmin = 0;

            dataRxSequence = 0;
            flowControlIsReceived = false;
        }

        public CanDevice Device {
            get; private set;
        }

        public int TxId {
            get; private set;
        }

        public int RxId {
            get; private set;
        }

        public double STmin {
            get; set;
        }

        public int P2Value {
            get; set;
        }

        private void Delay(double time_ms) {
            Stopwatch stopwatch = Stopwatch.StartNew();
            while(stopwatch.Elapsed.TotalMilliseconds < time_ms) {
                ((Action)(() => { }))(); // Do Nothing
            }
        }

        public bool Connect() {
            Device.DataReceived += MsgReceived;
            return Device.Connect();
        }

        private bool SendFC() {
            byte[] canMsg = new byte[8];
            canMsg[0] = 0x30;
            for(int i = 3; i < 8; i++)
                canMsg[i] = 0xAA;
            return Device.DataTransmit(TxId, canMsg);
        }

        private bool WaitFC(int timeOut) {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            while(stopwatch.ElapsedMilliseconds < timeOut) {
                if(flowControlIsReceived) {
                    flowControlIsReceived = false;
                    return true;
                }
                Thread.Sleep(1);
            }
            stopwatch.Stop();
            return false;
        }

        private void MsgReceived(CanDevice sender, int rxId, byte[] msg) {
            if(rxId != RxId)
                return;
            if((msg[0] & 0xF0) == 0x00) {               // Single Frame
                int length = msg[0] & 0x0F;
                byte[] data = new byte[length];
                Array.Copy(msg, 1, data, 0, length);
                DataReceived?.Invoke(this, data);
            }
            else if((msg[0] & 0xF0) == 0x10) {          // First Frame
                int length = ((msg[0] & 0x0F) << 8) | msg[1];
                dataRxBuff = new byte[length];
                Array.Copy(msg, 2, dataRxBuff, 0, 6);
                if(P2Value > 0)
                    Delay(P2Value);
                SendFC();
                dataRxCount = 6;
                dataRxSequence = 1;
            }
            else if((msg[0] & 0xF0) == 0x20) {          // Consecutive Frame
                if((msg[0] & 0x0F) == dataRxSequence) {
                    dataRxSequence++;
                    dataRxSequence &= 0x0F;
                    int length = dataRxBuff.Length - dataRxCount;
                    if(length > 7)
                        length = 7;
                    Array.Copy(msg, 1, dataRxBuff, dataRxCount, length);
                    dataRxCount += length;
                    if(dataRxCount == dataRxBuff.Length) {
                        dataRxSequence = 0;
                        DataReceived?.Invoke(this, dataRxBuff);
                    }
                }
            }
            else if((msg[0] & 0xF0) == 0x30) {          // Flow Control Frame
                flowControlIsReceived = true;
            }
        }

        public bool DataTransmit(byte[] data) {
            byte[] buff = new byte[8];
            if(data.Length <= 7) {
                buff[0] = (byte)data.Length;
                for(int i = 0; i < data.Length; i++)
                    buff[i + 1] = data[i];
                for(int i = data.Length + 1; i < 8; i++)
                    buff[i] = 0xAA;
                return Device.DataTransmit(TxId, buff);
            }
            else {
                int count = 0;
                buff[0] = (byte)(0x10 | (data.Length >> 8));
                buff[1] = (byte)data.Length;
                for(int i = 0; i < 6; i++) {
                    buff[count + 2] = data[i];
                    count++;
                }
                flowControlIsReceived = false;
                if(!Device.DataTransmit(TxId, buff))
                    return false;
                if(!WaitFC(500))
                    return false;
                byte SequenceNumber = 1;
                while(count < data.Length) {
                    buff[0] = (byte)(0x20 | SequenceNumber);
                    int length = data.Length - count;
                    if(length > 7)
                        length = 7;
                    for(int i = 0; i < length; i++) {
                        buff[i + 1] = data[count];
                        count++;
                    }
                    if(length < 7) {
                        for(int i = length + 1; i < 8; i++) {
                            buff[i] = 0xAA;
                            count++;
                        }
                    }
                    if(!Device.DataTransmit(TxId, buff))
                        return false;
                    SequenceNumber++;
                    SequenceNumber &= 0x0F;
                    if(STmin > 0)
                        Delay(STmin);
                }
                return true;
            }
        }

        public void Disconnect() {
            Device.DataReceived -= MsgReceived;
            Device.Disconnect();
        }
    }
}
