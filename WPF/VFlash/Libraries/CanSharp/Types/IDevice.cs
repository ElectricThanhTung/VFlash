namespace CanSharp {
    public interface IDevice {
        bool Connect();
        void Disconnect();
        bool DataTransmit(byte[] data);
        event DataReceivedEventHandler DataReceived;
    }
}
