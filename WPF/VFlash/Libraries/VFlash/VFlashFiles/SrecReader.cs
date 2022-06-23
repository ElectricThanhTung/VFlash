using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace VFlashFiles {
    internal class SrecReader {
        private class MCUDataComparer : IComparer<FwData> {
            public int Compare(FwData x, FwData y) {
                if(x.StartAddress == y.StartAddress)
                    return 0;
                return (x.StartAddress > y.StartAddress) ? 1 : -1;
            }
        }

        private string filePath;

        public SrecReader(string filePath) {
            this.filePath = filePath;

            FillMaskData = 0x00;
            ReadS3File(filePath);
        }

        public byte FillMaskData {
            get; set;
        }

        private Exception CreateException(string msg) {
            throw new Exception("An error occurred while reading \"" + Path.GetFileName(filePath) + "\" file. " + msg);
        }

        private uint HexToUint(string hex) {
            uint ret = 0;
            for(int i = 0; i < hex.Length; i++) {
                ret <<= 4;
                if('0' <= hex[i] && hex[i] <= '9')
                    ret |= (uint)(hex[i] - '0');
                else if('A' <= hex[i] && hex[i] <= 'F')
                    ret |= (uint)(hex[i] - 'A' + 10);
                else if('a' <= hex[i] && hex[i] <= 'f')
                    ret |= (uint)(hex[i] - 'a' + 10);
                else
                    throw CreateException("Input string was not in a correct format");
            }
            return ret;
        }

        private uint HexToUint(char hex) {
            if('0' <= hex && hex <= '9')
                return (uint)(hex - '0');
            else if('A' <= hex && hex <= 'F')
                return (uint)(hex - 'A' + 10);
            else if('a' <= hex && hex <= 'f')
                return (uint)(hex - 'a' + 10);
            throw CreateException("Input string was not in a correct format");
        }

        private List<FwData> MergeNearDataBlock(List<FwData> datas) {
            MCUDataComparer dataComparer = new MCUDataComparer();
            datas.Sort(dataComparer);
            for(int i = 0; i < datas.Count; i++) {
                uint endOfData = (uint)(datas[i].StartAddress + datas[i].Data.Count);
                for(int j = i + 1; j < datas.Count; j++) {
                    if(endOfData == datas[j].StartAddress) {
                        datas[i].Data.AddRange(datas[j].Data);
                        datas.RemoveAt(j);
                        endOfData = (uint)(datas[i].StartAddress + datas[i].Data.Count);
                        j--;
                    }
                }
            }
            return datas;
        }

        private void ReadS3File(string filePath) {
            byte[] fixCountValue = { 3, 5, 5, 4, 3 };
            uint lineCount = 0;
            uint recordCount = 0;
            FwData dataBlock = null;
            List<FwData> data = new List<FwData>();
            using(StreamReader reader = new StreamReader(filePath)) {
                string line;
                while((line = reader.ReadLine()) != null) {
                    lineCount++;
                    line = line.Trim();
                    if(line[0] != 's' && line[0] != 'S')
                        throw CreateException("File format error at line " + lineCount + ".");
                    if(line.Length < 10)
                        throw CreateException("File format error at line " + lineCount + ".");
                    if(!(line[1] == '3' || ('5' <= line[1] && line[1] <= '9')))
                        continue;
                    byte byteCount = (byte)((HexToUint(line[2]) << 4) | HexToUint(line[3]));
                    if(line[1] != '3' && byteCount != fixCountValue[line[1] - '5'])
                        throw CreateException("File format error at line " + lineCount + ".");
                    byte sum = byteCount;
                    if(line[1] == '3') {
                        if(byteCount >= 5)
                            byteCount -= 5;
                        else
                            throw CreateException("File format error at line " + lineCount + ". Byte Count can't be less than 6");

                        uint address = HexToUint(line.Substring(4, 8));
                        sum += (byte)(address >> 24);
                        sum += (byte)(address >> 16);
                        sum += (byte)(address >> 8);
                        sum += (byte)address;
                        if(dataBlock == null || (dataBlock.StartAddress + dataBlock.Data.Count != address)) {
                            dataBlock = new FwData() { StartAddress = address, Data = new List<byte>() };
                            data.Add(dataBlock);
                        }
                        for(int i = 0; i < byteCount; i++) {
                            int index = i * 2 + 12;
                            byte value = (byte)((HexToUint(line[index]) << 4) | HexToUint(line[index + 1]));
                            dataBlock.Data.Add(value);
                            sum += value;
                        }
                        recordCount++;
                    }
                    if(line[1] == '5' || line[1] == '6') {
                        uint count;
                        if(line[1] == '5')
                            count = HexToUint(line.Substring(4, 4));
                        else
                            count = HexToUint(line.Substring(4, 8));
                        if(count != recordCount)
                            throw CreateException("Record count value is not match at line " + lineCount + ".");
                        sum += (byte)(count >> 8);
                        sum += (byte)count;
                    }
                    else if('7' <= line[1] && line[1] <= '9') {
                        uint startAddr;
                        if(line[1] == '7')
                            startAddr = HexToUint(line.Substring(4, 8));
                        else if(line[1] == '8')
                            startAddr = HexToUint(line.Substring(4, 6));
                        else
                            startAddr = HexToUint(line.Substring(4, 4));
                        sum += (byte)(startAddr >> 24);
                        sum += (byte)(startAddr >> 16);
                        sum += (byte)(startAddr >> 8);
                        sum += (byte)startAddr;
                        if((byte)(HexToUint(line.Substring(line.Length - 2)) + sum) != 0xFF)
                            throw CreateException("Checksum failed in line number " + lineCount + ".");
                        break;
                    }
                    if((byte)(HexToUint(line.Substring(line.Length - 2)) + sum) != 0xFF)
                        throw CreateException("Checksum failed in line number " + lineCount + ".");
                }
            }
            if(data.Count == 0)
                throw CreateException("Can't read anything from this file.");
            DataBlocks = MergeNearDataBlock(data);
        }

        public List<FwData> DataBlocks {
            get; private set;
        }

        public FwData MergeData {
            get {
                if(DataBlocks.Count == 1)
                    return DataBlocks[0];
                FwData data = new FwData();
                data.StartAddress = DataBlocks[0].StartAddress;
                int lastIndex = DataBlocks.Count - 1;
                int length = (int)(DataBlocks[lastIndex].StartAddress + DataBlocks[lastIndex].Data.Count - data.StartAddress);
                byte[] buff = new byte[length];
                byte maskData = FillMaskData;
                if(maskData != 0) {
                    for(int i = 0; i < buff.Length; i++)
                        buff[i] = maskData;
                }
                for(int i = 0; i < DataBlocks.Count; i++)
                    Array.Copy(DataBlocks[i].Data.ToArray(), 0, buff, DataBlocks[i].StartAddress - data.StartAddress, DataBlocks[i].Data.Count);
                data.Data = buff.ToList();
                return data;
            }
        }
    }
}
