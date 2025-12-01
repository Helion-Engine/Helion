using Helion.Graphics;

namespace Helion.Util.Consoles;

public class ConsoleMessage
{
    public string Message = string.Empty;
    public long TimeNanos;
    public Color Color;
    public bool IsCentered;
    public int Count = 1;

    public void Set(string message, long timeNanos, Color color, bool isCentered)
    {
        Message = message;
        TimeNanos = timeNanos;
        Color = color;
        IsCentered = isCentered;
        Count = 1;
    }
}
