using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace CanSharp {
    public class UDS {
        public static event UDSRequestSentEventHandler AnyRequestSentFailed;
        public static event UDSRequestSentEventHandler AnyRequestStartSent;
        public static event UDSResponseReceivedEventHandler AnyResponseReceived;

        private List<byte[]> rxBuff;
        private const int rxMaxCount = 10;
        EventWaitHandle eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, null);

        public UDS(IDevice device) {
            this.Device = device;
            this.rxBuff = new List<byte[]>();
            this.Device.DataReceived += DataReceived;
        }

        public IDevice Device {
            get; private set;
        }

        private void DataReceived(IDevice sender, byte[] data) {
            lock(rxBuff) {
                if(rxBuff.Count < rxMaxCount) {
                    rxBuff.Add(data);
                    eventWaitHandle.Set();
                    AnyResponseReceived?.Invoke(this, data);
                }
            }
        }

        public bool Connect() {
            return Device.Connect();
        }

        public UDSResponse SendRequest(byte[] request) {
            return SendRequest(request, 0);
        }

        public UDSResponse SendRequest(byte[] request, int timeOut) {
            lock(rxBuff)
                rxBuff.Clear();
            AnyRequestStartSent?.Invoke(this, request);
            if(!Device.DataTransmit(request)) {
                AnyRequestSentFailed?.Invoke(this, request);
                return new UDSResponse(UDSResponseType.SendRequestFailed);
            }
            if(timeOut == 0)    // Do not wait for response
                return new UDSResponse(UDSResponseType.SendRequestSuccessful);
            return WaitResoponse(request, timeOut);
        }

        public UDSResponse WaitResoponse(byte[] msg, int timeOut) {
            Stopwatch stopwatch = Stopwatch.StartNew();
            while(stopwatch.ElapsedMilliseconds < timeOut) {
                eventWaitHandle.WaitOne(10);
                while(rxBuff.Count > 0) {
                    byte[] rxMsg;
                    lock(rxBuff) {
                        rxMsg = rxBuff[0];
                        rxBuff.RemoveAt(0);
                    }

                    if(rxMsg[0] == (msg[0] | 0x40)) {
                        stopwatch.Stop();
                        return new UDSResponse(UDSResponseType.PositiveResponse, rxMsg);
                    }
                    else if(rxMsg[0] == 0x7F && rxMsg[1] == msg[0]) {
                        if(rxMsg.Length == 3 && rxMsg[2] == 0x78)       // Pending message
                            stopwatch.Restart();
                        else {
                            stopwatch.Stop();
                            return new UDSResponse(UDSResponseType.NegativeResponse, rxMsg);
                        }
                    }
                }
            }
            stopwatch.Stop();
            return new UDSResponse(UDSResponseType.NoResponse);
        }

        public void Disconnect() {
            Device.Disconnect();
        }
    }
}
