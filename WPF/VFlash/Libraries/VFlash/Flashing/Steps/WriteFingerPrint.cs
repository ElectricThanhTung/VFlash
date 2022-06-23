using CanSharp;
using System;

namespace VFlash.Flashing {
    public class WriteFingerPrint : FlashAction {
        public override void Execute(FlashActionArgs actionArgs) {
            StartNewAction("Write Finger Print", () => {
                byte[] writeFingerArr = new byte[33] {
                    0x2E, 0xF1, 0x07, 0x00, 0x00, 0x00, 0x11, 0x22, 0x33, 0x44, 0x55,
                    0x66, 0x77, 0x88, 0x99, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
                    0x88, 0x99, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99,
                };

                DateTime dateTime = DateTime.Now;
                writeFingerArr[3] = (byte)(dateTime.Year % 100);
                writeFingerArr[3] = (byte)((writeFingerArr[3] % 10) + (writeFingerArr[3] / 10 * 16));
                writeFingerArr[4] = (byte)dateTime.Month;
                writeFingerArr[4] = (byte)((writeFingerArr[4] % 10) + (writeFingerArr[4] / 10 * 16));
                writeFingerArr[5] = (byte)dateTime.Day;
                writeFingerArr[5] = (byte)((writeFingerArr[5] % 10) + (writeFingerArr[5] / 10 * 16));

                UDSResponse response = actionArgs.EcuUDS.SendRequest(writeFingerArr, actionArgs.Timeout);
                if(!response)
                    throw new FlashException("Write Finger Print Failed");
            });
        }
    }
}
