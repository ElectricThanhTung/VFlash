using System;
using System.Collections.Generic;
using System.IO;

namespace VFlashFiles {
    internal class CrcReader {
        private List<byte> crcValue;
        private string filePath;

        public CrcReader(string filePath) {
            this.filePath = filePath;

            StreamReader streamReader = new StreamReader(filePath);
            string[] strs = streamReader.ReadToEnd().Split(',');
            streamReader.Close();

            crcValue = new List<byte>();

            for(int i = 0; i < strs.Length; i++) {
                string temp = strs[i].Trim();
                if(temp[0] == '0' && (temp[1] == 'x' || temp[1] == 'X')) {
                    uint value = HexToUint(temp.Substring(2));
                    if(value > 255)
                        throw CreateException("Signature/CRC file format is invalid.");
                    crcValue.Add((byte)value);
                }
                else
                    crcValue.Add(byte.Parse(temp));
            }
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
                    throw CreateException("Input string + \"" + hex + "\" was not in a correct format.");
            }
            return ret;
        }

        public List<byte> CrcValue {
            get {
                return crcValue;
            }
        }
    }
}
