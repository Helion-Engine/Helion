using System;
using System.Windows.Forms;
using static Helion.Client.Input.NativeMethods;

namespace Helion.Client.Input
{
    public delegate void WinMouseMove(int deltaX, int deltaY);

    public class NativeWinMouse : Form
    {
        private readonly WinMouseMove m_callback;

        /// <summary>
        /// Registers to receive windows raw input mouse events.
        /// </summary>
        /// <remarks>
        /// Creates a form that just exists for the sole purpose of receing the WM_INPUT message.
        /// In the future if we get access to the GameWindow Handle we can use that.
        /// Uses HID_USAGE_GENERIC_MOUSE so mouse movement from any mouse device will trigger the callback.
        /// See https://docs.microsoft.com/en-us/windows/win32/dxtecharts/taking-advantage-of-high-dpi-mouse-movement
        /// for more information.
        /// </remarks>
        /// <param name="callback">Callback function for when the mouse moves.</param>
        public NativeWinMouse(WinMouseMove callback)
        {
            m_callback = callback;

            if (!RegisterRawMouseInput(Handle))
                throw new Exception("Unable to register raw mouse input");
        }

        protected override void WndProc(ref Message message)
        {
            switch (message.Msg)
            {
                case WinMouseConstants.WM_INPUT:
                    int x, y;
                    NativeMethods.GetRawInput(ref message, out x, out y);
                    m_callback(x, y);
                    break;
            }

            base.WndProc(ref message);
        }
    }
}
