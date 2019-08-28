using System;

namespace Helion.Graphics.Palette
{
    /// <summary>
    /// A helper class that stores default palettes.
    /// </summary>
    public static class Palettes
    {
        private static Palette DefaultPalette;

        /// <summary>
        /// Gets a default palette if one doesn't exist.
        /// </summary>
        /// <returns>The default palette.</returns>
        public static Palette GetDefaultPalette()
        {
            if (DefaultPalette != null) 
                return DefaultPalette;
            
            byte[] data = new byte[Palette.NumColors * Palette.ColorComponents];

            for (int i = 0; i < Palette.NumColors; i++)
            {
                data[i] = (byte)i;
                data[i + 1] = (byte)i;
                data[i + 2] = (byte)i;
            }

            Palette? palette = Palette.From(data);
            if (palette == null)
                throw new NullReferenceException("Failed to create the default palette, shouldn't be possible");

            DefaultPalette = palette;
            return DefaultPalette;
        }
    }
}
