using System;
using System.IO;
using System.Runtime.InteropServices;

namespace VFlash {
    internal class SigCrcCalculator {
        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32.dll")]
        private static extern bool FreeLibrary(IntPtr hModule);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr Calculator(byte[] data, uint length, out uint result_length);

        private string dllPath;

        public SigCrcCalculator(string dllPath) {
            this.dllPath = dllPath;
        }

        public byte[] Calculate(byte[] data) {
            IntPtr dllHander = LoadLibrary(Path.GetFullPath(dllPath));
            if(dllHander == IntPtr.Zero)
                throw new Exception("Can't load dll file " + Path.GetFileName(dllPath));

            IntPtr pAddressOfFunctionToCall = GetProcAddress(dllHander, "Calculator");
            if(pAddressOfFunctionToCall == IntPtr.Zero)
                throw new Exception("Can't find func Calculator in library");

            Calculator CalculatorFunc = (Calculator)Marshal.GetDelegateForFunctionPointer(pAddressOfFunctionToCall, typeof(Calculator));

            uint result_length = 0;
            IntPtr retPtr = CalculatorFunc(data, (uint)data.Length, out result_length);

            if(result_length == 0)
                throw new Exception("Calculator Signature/CRC failed");

            byte[] ret = new byte[result_length];
            Marshal.Copy(retPtr, ret, 0, (int)result_length);

            FreeLibrary(dllHander);

            return ret;
        }
    }
}
