using System;
using System.Runtime.InteropServices;
using NFluidsynth.Native;

namespace NFluidsynth
{
    internal static class Utility
    {
        public static void CheckReturnValue(int value)
        {
            if (value != LibFluidsynth.FluidOk)
            {
                throw new FluidSynthInteropException();
            }
        }

        public static IntPtr PassDelegatePointer<T>(T input, out T output) where T : Delegate
        {
            output = input;
            return Marshal.GetFunctionPointerForDelegate(input);
        }

#if !NETCOREAPP
        private static unsafe int FindNullTerminator(byte* ptr)
        {
            if (ptr == null)
            {
                throw new ArgumentNullException(nameof(ptr));
            }

            var i = 0;
            while (*(ptr + i) != 0)
            {
                i++;
            }

            return i;
        }
#endif

        public static unsafe string PtrToStringUTF8(byte* ptr)
        {
#if NETCOREAPP
            return Marshal.PtrToStringUTF8((IntPtr) ptr);
#else
            if (ptr == null)
            {
                return null;
            }

            var length = FindNullTerminator(ptr);

            return System.Text.Encoding.UTF8.GetString(ptr, length);
#endif
        }
    }
}