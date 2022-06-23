using CanSharp;
using System;
using System.IO;
using System.Threading;
using VFlashFiles;

namespace VFlash.Flashing {
    public class FlashBinaries : FlashAction {
        private FlashActionArgs actionArgs;
        private int maxDataLength;
        private int currentFileIndex;

        public FlashBinaries() {
            maxDataLength = 4095;
            currentFileIndex = 0;
            EraseEnabled = true;
        }

        public bool EraseEnabled {
            get; set;
        }

        private void RequestErase(string fileName, uint startAddr, int size) {
            StartNewAction("Request Erase For " + fileName, () => {
                byte[] requestBuffer = new byte[13];
                requestBuffer[0] = 0x31;
                requestBuffer[1] = 0x01;
                requestBuffer[2] = 0xFF;
                requestBuffer[3] = 0x00;
                requestBuffer[4] = 0x44;
                requestBuffer[5] = (byte)(startAddr >> 24);
                requestBuffer[6] = (byte)(startAddr >> 16);
                requestBuffer[7] = (byte)(startAddr >> 8);
                requestBuffer[8] = (byte)(startAddr >> 0);
                requestBuffer[9] = (byte)(size >> 24);
                requestBuffer[10] = (byte)(size >> 16);
                requestBuffer[11] = (byte)(size >> 8);
                requestBuffer[12] = (byte)(size >> 0);

                UDSResponse response = actionArgs.EcuUDS.SendRequest(requestBuffer, actionArgs.Timeout);
                if(!response)
                    throw new FlashException("Request Erase Failed");
            });
        }

        private void RequestDownload(string fileName, uint startAddr, int size) {
            StartNewAction("Request Download For " + fileName, () => {
                byte[] requestBuffer = new byte[11];

                requestBuffer[0] = 0x34;
                requestBuffer[1] = 0x00;
                requestBuffer[2] = 0x44;

                requestBuffer[3] = (byte)(startAddr >> 24);
                requestBuffer[4] = (byte)(startAddr >> 16);
                requestBuffer[5] = (byte)(startAddr >> 8);
                requestBuffer[6] = (byte)(startAddr >> 0);

                requestBuffer[7] = (byte)(size >> 24);
                requestBuffer[8] = (byte)(size >> 16);
                requestBuffer[9] = (byte)(size >> 8);
                requestBuffer[10] = (byte)(size >> 0);

                UDSResponse response = actionArgs.EcuUDS.SendRequest(requestBuffer, actionArgs.Timeout);
                if(!response)
                    throw new FlashException("Request Download Failed");

                if(response.Count > 3) {
                    int lengthFormatIdentifier = response[1] >> 4;
                    if(response.Count >= (lengthFormatIdentifier + 2)) {
                        int maxBlockLength = 0;
                        for(int i = 0; i < lengthFormatIdentifier; i++) {
                            maxBlockLength <<= 8;
                            maxBlockLength |= response[i + 2];
                        }
                        if(maxBlockLength == 0)
                            throw new Exception("\"Max Block Length\" Cannot be equal to 0. Please check the config in your device with \"Request Download Response\"");
                        maxDataLength = maxBlockLength;
                    }
                }
            });
        }

        private void DataTransfer(string fileName, byte[] data) {
            StartNewAction("Transmit Data For " + fileName, () => {
                int blockSize = maxDataLength - 2;
                int N = data.Length / blockSize;

                Thread.Sleep(100);

                if(N > 0) {
                    byte[] requestBuffer = new byte[maxDataLength];
                    requestBuffer[0] = 0x36;

                    for(int i = 0; i < N; i++) {
                        requestBuffer[1] = (byte)(i + 1);
                        Array.Copy(data, i * blockSize, requestBuffer, 2, blockSize);

                        UDSResponse response = actionArgs.EcuUDS.SendRequest(requestBuffer, actionArgs.Timeout);
                        if(!response)
                            throw new FlashException("Transmit Data Failed");

                        OnProgressChanged(currentFileIndex, i * blockSize, data.Length);
                    }
                }

                int remaining = data.Length % blockSize;
                if(remaining > 0) {
                    byte[] requestBuffer = new byte[remaining + 2];
                    requestBuffer[0] = 0x36;
                    requestBuffer[1] = (byte)(N + 1);

                    Array.Copy(data, N * blockSize, requestBuffer, 2, remaining);

                    UDSResponse response = actionArgs.EcuUDS.SendRequest(requestBuffer, actionArgs.Timeout);
                    if(!response)
                        throw new FlashException("Transmit Data Failed");

                    OnProgressChanged(currentFileIndex, data.Length, data.Length);
                }
            });
        }

        private void DownloadStop(string fileName) {
            StartNewAction("Tranfer Exit For " + fileName, () => {
                UDSResponse response = actionArgs.EcuUDS.SendRequest(new byte[] { 0x37 }, actionArgs.Timeout);
                if(!response)
                    throw new FlashException("Tranfer Exit Failed");
            });
        }

        private void WriteAndCheckSignatureCRC(string fileName, byte[] data, string crcPath) {
            StartNewAction("Check Signature/CRC For " + fileName, () => {
                byte[] crc;
                if(Path.GetExtension(crcPath).ToLower() != ".dll")
                    crc = new CrcReader(crcPath).CrcValue.ToArray();
                else
                    crc = new SigCrcCalculator(crcPath).Calculate(data);
                if(crc.Length > maxDataLength)
                    throw new FlashException("Signature/CRC data size too long");

                byte[] requestBuffer = new byte[crc.Length + 4];
                requestBuffer[0] = 0x31;
                requestBuffer[1] = 0x01;
                requestBuffer[2] = 0x02;
                requestBuffer[3] = 0x02;

                Array.Copy(crc, 0, requestBuffer, 4, crc.Length);

                Thread.Sleep(100);

                UDSResponse response = actionArgs.EcuUDS.SendRequest(requestBuffer, actionArgs.Timeout);
                if(!response || response[response.Count - 1] != 0)
                    throw new FlashException("Check Signature/CRC Failed");
            });
        }

        private void StartTranferFile(FlashFileInfo file) {
            FwReader reader = new FwReader(file.FilePath);
            FwData data = reader.MergeData;
            byte[] dataArr = data.Data.ToArray();
            if(!file.IsDriver)
                RequestErase(file.FileName, data.StartAddress, data.Data.Count);
            RequestDownload(file.FileName, data.StartAddress, data.Data.Count);
            DataTransfer(file.FileName, dataArr);
            DownloadStop(file.FileName);
            WriteAndCheckSignatureCRC(file.FileName, dataArr, file.CrcPath);
        }

        public override void Execute(FlashActionArgs actionArgs) {
            this.actionArgs = actionArgs;
            currentFileIndex = 0;
            foreach(FlashFileInfo file in actionArgs.Files) {
                currentFileIndex = actionArgs.Files.IndexOf(file);
                StartTranferFile(file);
            }
        }
    }
}
