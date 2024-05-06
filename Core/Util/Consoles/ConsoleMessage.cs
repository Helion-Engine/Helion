using Helion.Graphics;

namespace Helion.Util.Consoles;

public class ConsoleMessage
{
    public string Message;
    public long TimeNanos;
    public Color Color;

    public void Set(string message, long timeNanos, Color color)
    {
        Message = message;
        TimeNanos = timeNanos;
        Color = color;
    }
}
