using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WinMouse.Native
{
    public static class Constants
    {
        public const int WM_INPUT = 255;
        public const ushort HID_USAGE_PAGE_GENERIC = 1;
        public const ushort HID_USAGE_GENERIC_MOUSE = 2;
        public const uint RIDEV_INPUTSINK = 0x00000100;
        public const uint RID_INPUT = 0x10000003;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct RAWINPUTDEVICE
    {
        public ushort usUsagePage;
        public ushort usUsage;
        public uint dwFlags;
        public IntPtr hwndTarget;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RAWINPUTHEADER
    {
        public uint dwType;
        public uint dwSize;
        public IntPtr hDevice;
        public int wParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RAWMOUSE
    {
        public ushort usFlags;
        public uint ulButtons;
        public uint ulRawButtons;
        public int lLastX;
        public int lLastY;
        public uint ulExtraInformation;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RAWINPUT
    {
        public RAWINPUTHEADER header;
        public RAWMOUSE mouse;
    }

    public static class NativeMethods
    {
        public static bool RegisterRawMouseInput(IntPtr handle)
        {
            RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];
            rid[0].usUsagePage = Constants.HID_USAGE_PAGE_GENERIC;
            rid[0].usUsage = Constants.HID_USAGE_GENERIC_MOUSE;
            rid[0].dwFlags = Constants.RIDEV_INPUTSINK;
            rid[0].hwndTarget = handle;

            return RegisterRawInputDevices(rid, 1, Convert.ToUInt32(Marshal.SizeOf(rid[0])));
        }

        public static void GetRawInput(ref Message message, out int x, out int y)
        {
            uint dwSize = (uint)Marshal.SizeOf(typeof(RAWINPUT));
            uint headerSize = (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER));
            IntPtr rawData = Marshal.AllocHGlobal((int)dwSize);

            if (GetRawInputData(message.LParam, Constants.RID_INPUT, rawData, ref dwSize, headerSize) != -1)
            {
                // In the future if we register for any other form of input we would need to check for header.dwType == RIM_TYPEMOUSE
                RAWINPUT rawInput = (RAWINPUT)Marshal.PtrToStructure(rawData, typeof(RAWINPUT));
                x = rawInput.mouse.lLastX;
                y = rawInput.mouse.lLastY;
            }
            else
            {
                x = y = 0;
            }

            Marshal.FreeHGlobal(rawData);
        }

        [DllImport("User32.dll", SetLastError = true)]
        private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevice, uint uiNumDevices, uint cbSize);

        [DllImport("User32.dll", SetLastError = true)]
        private static extern int GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

        [DllImport("User32.dll", SetLastError = true)]
        private static extern bool SetCursorPos(int x, int y);
    }
}
