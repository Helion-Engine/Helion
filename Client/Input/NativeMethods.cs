using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using NLog;

namespace Helion.Client.Input
{
    public static class NativeMethods
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static bool RegisterRawMouseInput(IntPtr handle)
        {
            RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];
            rid[0].usUsagePage = WinMouseConstants.HID_USAGE_PAGE_GENERIC;
            rid[0].usUsage = WinMouseConstants.HID_USAGE_GENERIC_MOUSE;
            rid[0].dwFlags = WinMouseConstants.RIDEV_INPUTSINK;
            rid[0].hwndTarget = handle;

            return RegisterRawInputDevices(rid, 1, Convert.ToUInt32(Marshal.SizeOf(rid[0])));
        }

        public static void GetRawInput(ref Message message, out int x, out int y)
        {
            uint dwSize = (uint)Marshal.SizeOf(typeof(RAWINPUT));
            uint headerSize = (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER));
            IntPtr rawData = Marshal.AllocHGlobal((int)dwSize);

            if (GetRawInputData(message.LParam, WinMouseConstants.RID_INPUT, rawData, ref dwSize, headerSize) != -1)
            {
                // In the future if we register for any other form of input we would need to check for header.dwType == RIM_TYPEMOUSE
                object? rawInputObj = Marshal.PtrToStructure(rawData, typeof(RAWINPUT));
                if (rawInputObj == null)
                {
                    Log.Error("Reading raw input returns a null value, should never happen");
                    x = 0;
                    y = 0;
                    return;
                }

                RAWINPUT rawInput = (RAWINPUT)rawInputObj;
                x = rawInput.mouse.lLastX;
                y = rawInput.mouse.lLastY;
                // TODO: Mask `rawInput.mouse.ulButtons` with Constants for button presses.
            }
            else
            {
                x = y = 0;
            }

            Marshal.FreeHGlobal(rawData);
        }

        public static void SetMousePosition(int x, int y)
        {
            SetCursorPos(x, y);
        }

        [DllImport("User32.dll", SetLastError = true)]
        private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevice, uint uiNumDevices, uint cbSize);

        [DllImport("User32.dll", SetLastError = true)]
        private static extern int GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

        [DllImport("User32.dll", SetLastError = true)]
        private static extern bool SetCursorPos(int x, int y);

        public static class WinMouseConstants
        {
            public const int WM_INPUT = 255;
            public const ushort HID_USAGE_PAGE_GENERIC = 1;
            public const ushort HID_USAGE_GENERIC_MOUSE = 2;
            public const uint RIDEV_INPUTSINK = 0x00000100;
            public const uint RID_INPUT = 0x10000003;
            public const uint RI_MOUSE_LEFT_BUTTON_DOWN = 0x0001;
            public const uint RI_MOUSE_LEFT_BUTTON_UP = 0x0002;
            public const uint RI_MOUSE_MIDDLE_BUTTON_DOWN = 0x0010;
            public const uint RI_MOUSE_MIDDLE_BUTTON_UP = 0x0020;
            public const uint RI_MOUSE_RIGHT_BUTTON_DOWN = 0x0004;
            public const uint RI_MOUSE_RIGHT_BUTTON_UP = 0x0008;
        }

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
    }
}
