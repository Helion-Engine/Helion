using Helion.Util;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Helion.Client;

public class GlfwClipboard(Window window) : IClipboard
{
    private readonly Window m_window = window;

    public unsafe string GetText()
    {
        return GLFW.GetClipboardString(m_window.WindowPtr);
    }

    public unsafe void SetText(string text)
    {
        GLFW.SetClipboardString(m_window.WindowPtr, text);
    }
}
