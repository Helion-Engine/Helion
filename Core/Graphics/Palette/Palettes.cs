using Helion.Util;

namespace Helion.Graphics.Palette
{
    /// <summary>
    /// A helper class that stores default palettes.
    /// </summary>
    public class Palettes
    {
        private static Palette defaultPalette;

        /// <summary>
        /// Gets a default palette if one doesn't exist.
        /// </summary>
        /// <returns>The default palette.</returns>
        public static Palette GetDefaultPalette()
        {
            if (defaultPalette == null)
            {
                byte[] data = new byte[Palette.NUM_COLORS * Palette.COLOR_COMPONENTS];

                int offset = 0;
                for (int i = 0; i < Palette.NUM_COLORS; i++)
                {
                    data[i] = (byte)i;
                    data[i + 1] = (byte)i;
                    data[i + 2] = (byte)i;
                    offset += 3;
                }

                Palette? palette = Palette.From(data);
                if (palette != null)
                    defaultPalette = palette;
                else
                    throw new HelionException("Should never fail to create a default palette");
            }

            return defaultPalette;
        }
    }
}
