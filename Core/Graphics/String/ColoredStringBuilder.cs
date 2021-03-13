using System.Drawing;
using System.Text;
using Helion.Util;
using static Helion.Util.Assertion.Assert;

namespace Helion.Graphics.String
{
    public class ColoredStringBuilder
    {
        private readonly StringBuilder m_builder = new();

        public static ColoredString From(params object[] objects)
        {
            ColoredStringBuilder builder = new();
            builder.Append(objects);
            return builder.Build();
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
                case CIString str:
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
}