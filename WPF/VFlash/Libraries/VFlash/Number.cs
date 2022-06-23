using System;

namespace VFlash {
    internal static class Number {
        private static bool IsDecNumber(string s) {
            int ret;
            return int.TryParse(s, out ret);
        }

        private static bool IsHexNumber(string s) {
            s = s.ToLower().Replace("0x", "");
            for(int i = 0; i < s.Length; i++) {
                char c = s[i];
                if(!(('0' <= c && c <= '9') || ('a' <= c && c <= 'f')))
                    return false;
            }
            return true;
        }

        public static int HexToInt(string s) {
            s = s.ToLower().Replace("0x", "");
            int ret = 0;
            for(int i = 0; i < s.Length; i++) {
                char c = s[i];
                ret <<= 4;
                if('0' <= c && c <= '9')
                    ret |= c - '0';
                else if('a' <= c && c <= 'f')
                    ret |= c - 'a' + 10;
                else
                    throw new Exception("Invalid hex number format");
            }
            return ret;
        }

        public static bool IsInteger(string s) {
            if(s.Length >= 3 && (s[0] == '0' && (s[1] == 'x' || s[1] == 'X')))
                return IsHexNumber(s);
            return IsDecNumber(s);
        }

        public static int ToInt(string s) {
            if(s.Length >= 3 && (s[0] == '0' && (s[1] == 'x' || s[1] == 'X')))
                return HexToInt(s);
            return int.Parse(s);
        }

        public static int ForceToInt(string s) {
            try {
                if(s.Length >= 3 && (s[0] == '0' && (s[1] == 'x' || s[1] == 'X')))
                    return HexToInt(s);
                return int.Parse(s);
            }
            catch {
                return 0;
            }
        }

        public static string ToHex(long value) {
            string x = value.ToString("X");
            if((x.Length % 2) != 0)
                return "0x0" + x;
            return "0x" + x;
        }
    }
}
