using System;
using System.Collections.Generic;
using System.Linq;

namespace CanSharp {
    public class UDSResponse {
        private UDSResponseType responseType;
        private byte[] Data;

        public UDSResponse(UDSResponseType responseType, byte[] data = null) {
            this.responseType = responseType;
            Data = data;
        }

        public static implicit operator Boolean(UDSResponse value) {
            if(value.responseType == UDSResponseType.PositiveResponse)
                return true;
            return false;
        }

        public byte this[int index] {
            get {
                return Data[index];
            }
        }

        public int Count {
            get {
                return Data.Length;
            }
        }

        public byte[] ToArray() {
            return Data;
        }

        public List<byte> ToList() {
            return Data.ToList();
        }

        public static bool operator ==(UDSResponse a, UDSResponseType b) {
            if(a.responseType == b)
                return true;
            return false;
        }

        public static bool operator ==(UDSResponse a, bool b) {
            if((a.responseType == UDSResponseType.PositiveResponse) == b)
                return true;
            return false;
        }

        public static bool operator !=(UDSResponse a, UDSResponseType b) {
            if(a.responseType != b)
                return true;
            return false;
        }

        public static bool operator !=(UDSResponse a, bool b) {
            if((a.responseType == UDSResponseType.PositiveResponse) != b)
                return true;
            return false;
        }

        public override bool Equals(object o) {
            if(o.GetType() == typeof(UDSResponseType) && this == (UDSResponseType)o)
                return true;
            if(o.GetType() != this.GetType())
                return false;
            if(this == o)
                return true;
            return false;
        }

        public override int GetHashCode() {
            return 0;
        }
    }
}
