using System;
using System.IO;
using System.Runtime.InteropServices;

namespace VFlash {
    internal class SeedKeyLoader {
        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32.dll")]
        private static extern bool FreeLibrary(IntPtr hModule);

        private enum VKeyGenResultEx {
            KGRE_Ok = 0,
            KGRE_BufferToSmall = 1,
            KGRE_SecurityLevelInvalid = 2,
            KGRE_VariantInvalid = 3,
            KGRE_UnspecifiedError = 4
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate VKeyGenResultEx GenerateKeyEx(
            byte[] ipSeedArray,
            uint iSeedArraySize,
            uint iSecurityLevel,
            byte[] ipVariant,
            byte[] iopKeyArray,
            uint iMaxKeyArraySize,
            ref uint oActualKeyArraySize
        );

        private string dllPath;

        public SeedKeyLoader(string dllPath) {
            this.dllPath = dllPath;
        }

        public byte[] GenKey(byte[] seed, uint iSecurityLevel) {
            IntPtr dllHander = LoadLibrary(Path.GetFullPath(dllPath));
            if(dllHander == IntPtr.Zero)
                throw new Exception("Can't load dll file " + Path.GetFileName(dllPath));

            IntPtr pAddressOfFunctionToCall = GetProcAddress(dllHander, "GenerateKeyEx");
            if(pAddressOfFunctionToCall == IntPtr.Zero)
                throw new Exception("Can't find func GenerateKeyEx in library");

            GenerateKeyEx GenerateKeyExFunc = (GenerateKeyEx)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(GenerateKeyEx));

            byte[] buff = new byte[1024];
            uint oActualKeyArraySize = 0;

            VKeyGenResultEx error = GenerateKeyExFunc(
                seed,
                (uint)seed.Length,
                iSecurityLevel,
                null,
                buff,
                (uint)buff.Length,
                ref oActualKeyArraySize
            );

            byte[] ret = null;
            if(error == VKeyGenResultEx.KGRE_Ok) {
                ret = new byte[oActualKeyArraySize];
                Array.Copy(buff, ret, oActualKeyArraySize);
            }

            FreeLibrary(dllHander);

            return ret;
        }
    }
}
