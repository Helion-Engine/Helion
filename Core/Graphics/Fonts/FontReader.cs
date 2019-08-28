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
        /// <param name="data">The data to read.</param>
        /// <param name="alphaCutoff">The cutoff to which anything under it is
        /// made transparent.</param>
        /// <returns>The font, or null on failure.</returns>
        public static Font? Read(byte[] data, float alphaCutoff)
        {
            // TODO: Is there a better way than this? I don't like using unsafe
            //       unless I'm forced to...
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
                        return null;

                    return CreateFont(fontCollection.Families[0], alphaCutoff);
                }
                catch
                {
                    // TODO
                }
            }

            return null;
        }

        private static Font? CreateFont(System.Drawing.FontFamily fontFamily, float alphaCutoff)
        {
            System.Drawing.Font font = new System.Drawing.Font(fontFamily, RenderFontSize);
            // TODO: Use it like so:
            // https://stackoverflow.com/questions/34473139/new-system-drawing-font-code-behind

            return null;
        }
    }
}