using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VFlashFiles {
    internal class HexFileReader {
        private class MCUDataComparer : IComparer<FwData> {
            public int Compare(FwData x, FwData y) {
                if(x.StartAddress == y.StartAddress)
                    return 0;
                return (x.StartAddress > y.StartAddress) ? 1 : -1;
            }
        }

        private string filePath;

        public HexFileReader(string filePath) {
            this.filePath = filePath;

            FillMaskData = 0x00;
            ReadFile(filePath);
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
                    throw new Exception("Input string was not in a correct format.");
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
            throw CreateException("Input string was not in a correct format.");
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

        private void ReadFile(string filePath) {
            uint addressOffset = 0;
            uint lineCount = 0;
            FwData dataBlock = null;
            List<FwData> data = new List<FwData>();
            using(StreamReader reader = new StreamReader(filePath)) {
                string line;
                while((line = reader.ReadLine()) != null) {
                    lineCount++;
                    line = line.Trim();
                    if(line[0] != ':')
                        continue;
                    if(line.Length < 11 || line[7] != '0')
                        throw CreateException("Hex file format is incorrect.");
                    if(line[8] == '0') {
                        uint byteCount = (HexToUint(line[1]) << 4) | HexToUint(line[2]);
                        if(line.Length < byteCount * 2 + 11)
                            throw CreateException("Hex file format is incorrect.");
                        uint address = HexToUint(line.Substring(3, 4)) + addressOffset;
                        uint sum = byteCount + (address & 0xFF) + (address >> 8 & 0xFF);
                        if(dataBlock == null || (dataBlock.StartAddress + dataBlock.Data.Count != address)) {
                            dataBlock = new FwData() { StartAddress = address, Data = new List<byte>() };
                            data.Add(dataBlock);
                        }
                        for(int i = 0; i < byteCount; i++) {
                            int index = i * 2 + 9;
                            byte value = (byte)((HexToUint(line[index]) << 4) | HexToUint(line[index + 1]));
                            dataBlock.Data.Add(value);
                            sum += value;
                        }
                        if(HexToUint(line.Substring(line.Length - 2)) != ((256 - sum & 0xFF) & 0xFF))
                            throw CreateException("Checksum failed in line number" + lineCount + ".");
                    }
                    else if(line[8] == '1')
                        break;
                    else if(line[8] == '2')
                        addressOffset = 16 * HexToUint(line.Substring(9, 4));
                    else if(line[8] == '4')
                        addressOffset = 65536 * HexToUint(line.Substring(9, 4));
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
