using Helion.Util;
using System;

namespace Helion.Graphics.Fonts
{
    public class FontReader
    {
        public const int RenderFontSize = 64;

        /// <summary>
        /// A native font reading subsystem that leverages the standard library to
        /// read fonts.
        /// </summary>
        public static Optional<Font> Read(byte[] data, float alphaCutoff)
        {
            using (System.Drawing.Text.PrivateFontCollection fontCollection = new System.Drawing.Text.PrivateFontCollection())
            {
                try
                {
                    unsafe
                    {
                        fixed (byte* ptr = data)
                        {
                            fontCollection.AddMemoryFont((IntPtr)ptr, data.Length);
                        }
                    }

                    if (fontCollection.Families.Length == 0)
                        return Optional<Font>.Empty();

                    return CreateFont(fontCollection.Families[0], alphaCutoff);
                }
                catch
                {
                }
            }

            return Optional<Font>.Empty();
        }

        private static Optional<Font> CreateFont(System.Drawing.FontFamily fontFamily, float alphaCutoff)
        {
            System.Drawing.Font font = new System.Drawing.Font(fontFamily, RenderFontSize);
            // TODO: Use it like so:
            // https://stackoverflow.com/questions/34473139/new-system-drawing-font-code-behind

            return Optional<Font>.Empty();
        }
    }
}
