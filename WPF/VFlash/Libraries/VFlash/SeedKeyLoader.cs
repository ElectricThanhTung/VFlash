using System;
using System.IO;
using System.Runtime.CompilerServices;
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

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int ZLGKey(
            byte[] ipSeedArray,
            ushort iSeedArraySize,
            uint iSecurityLevel,
            byte[] ipVariant,
            byte[] iopKeyArray,
            ref ushort iKeyArraySize
        );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool Seed2Key(
            ulong VendorCode,
            byte[] Seed,
            uint SeedSize,
            byte[] Key,
            ref uint KeySize,
            ushort SAAlg
        );

        private string dllPath;

        public SeedKeyLoader(string dllPath) {
            this.dllPath = dllPath;
        }

        public byte[] GenKey(byte[] seed, uint iSecurityLevel) {
            IntPtr dllHander = LoadLibrary(Path.GetFullPath(dllPath));
            if(dllHander == IntPtr.Zero)
                throw new Exception("Can't load dll file " + Path.GetFileName(dllPath));

            byte[] ret = null;
            byte[] buff = new byte[1024];
            IntPtr pAddressOfFunctionToCall;
            if((pAddressOfFunctionToCall = GetProcAddress(dllHander, "GenerateKeyEx")) != IntPtr.Zero) {
                GenerateKeyEx GenerateKeyFunc = (GenerateKeyEx)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(GenerateKeyEx));
                uint oActualKeyArraySize = 0;
                VKeyGenResultEx error = GenerateKeyFunc(seed, (uint)seed.Length, iSecurityLevel, null, buff, (uint)buff.Length, ref oActualKeyArraySize);
                if(error == VKeyGenResultEx.KGRE_Ok) {
                    ret = new byte[oActualKeyArraySize];
                    Array.Copy(buff, ret, oActualKeyArraySize);
                }
            }
            else if((pAddressOfFunctionToCall = GetProcAddress(dllHander, "ZLGKey")) != IntPtr.Zero) {
                ZLGKey GenerateKeyFunc = (ZLGKey)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(ZLGKey));
                ushort oActualKeyArraySize = (ushort)buff.Length;
                int error = GenerateKeyFunc(seed, (ushort)seed.Length, iSecurityLevel, null, buff, ref oActualKeyArraySize);
                if(error == 0) {
                    ret = new byte[oActualKeyArraySize];
                    Array.Copy(buff, ret, oActualKeyArraySize);
                }
            }
            else if((pAddressOfFunctionToCall = GetProcAddress(dllHander, "Seed2Key")) != IntPtr.Zero) {
                Seed2Key GenerateKeyFunc = (Seed2Key)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(Seed2Key));
                uint oActualKeyArraySize = (ushort)buff.Length;
                bool error = GenerateKeyFunc(0, seed, (ushort)seed.Length, buff, ref oActualKeyArraySize, (ushort)iSecurityLevel);
                if(error) {
                    ret = new byte[oActualKeyArraySize];
                    Array.Copy(buff, ret, oActualKeyArraySize);
                }
            }
            FreeLibrary(dllHander);
            if(ret != null)
                return ret;
            if(pAddressOfFunctionToCall == IntPtr.Zero)
                throw new Exception("Can't find function to generate key in " + Path.GetFileName(dllPath));
            throw new Exception("Error while generating key");
        }
    }
}
