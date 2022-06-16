using System.Drawing;
using System.Text;
using static Helion.Util.Assertion.Assert;

namespace Helion.Graphics.String;

public class ColoredStringBuilder
{
    private readonly StringBuilder m_builder = new();
    private static readonly ColoredStringBuilder StringBuilder = new();

    public static ColoredString From(params object[] objects)
    {
        StringBuilder.Append(objects);
        var coloredString = StringBuilder.Build();
        StringBuilder.Clear();
        return coloredString;
    }

    public void Clear()
    {
        m_builder.Clear();
    }

    public ColoredStringBuilder Append(params object[] objects)
    {
        foreach (object obj in objects)
        {
            switch (obj)
            {
            case Color color:
                m_builder.Append($@"\c[{color.R},{color.G},{color.B}]");
                break;
            case string str:
                m_builder.Append(str);
                break;
            default:
                Fail($"Passed unexpected object type into colored string builder: {obj.GetType()}");
                break;
            }
        }

        return this;
    }

    public ColoredString Build() => RGBColoredStringDecoder.Decode(m_builder.ToString());
}
