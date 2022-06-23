namespace VFlash.Flashing {
    public static class UDSMessage {
        public static byte[] TesterPresent = { 0x3E, 0x80 };
        public static byte[] DefaultSession = { 0x10, 0x01 };
        public static byte[] ProgrammingSession = { 0x10, 0x02 };
        public static byte[] DisableDTCSetting = { 0x85, 0x02 };
        public static byte[] EnableDTCSetting = { 0x85, 0x01 };
        public static byte[] DisableNormalCommunication = { 0x28, 0x01, 0x03 };
        public static byte[] EnableNormalCommunication = { 0x28, 0x00, 0x03 };
        public static byte[] ExtendedSession = { 0x10, 0x03 };
        public static byte[] CheckProgrammingPreconditions = { 0x31, 0x01, 0x02, 0x03 };
        public static byte[] HardReset = { 0x11, 0x01 };
        public static byte[] CheckDependencies = { 0x31, 0x01, 0xFF, 0x01 };
    }
}
