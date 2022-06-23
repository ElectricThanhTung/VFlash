namespace CanSharp {
    public delegate void CanDataReceivedEventHandler(CanDevice sender, int rxId, byte[] data);
    
    public delegate void DataReceivedEventHandler(IDevice sender, byte[] data);

    public delegate void UDSRequestSentEventHandler(UDS sender, byte[] data);
    public delegate void UDSResponseReceivedEventHandler(UDS sender, byte[] data);
}
