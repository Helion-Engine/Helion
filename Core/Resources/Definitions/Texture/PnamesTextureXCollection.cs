using System.Collections.Generic;
using Helion.Resources.Archives.Entries;
using NLog;

namespace Helion.Resources.Definitions.Texture
{
    /// <summary>
    /// Holds references to pnames and texture1/2/3 definition entries.
    /// </summary>
    /// <remarks>
    /// Designed as a wrapper around both types since we want to make sure when
    /// we operate on a collection of pnames/textureX that we have both entries
    /// and they are not corrupt in some way.
    /// </remarks>
    public class PnamesTextureXCollection
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// A list of all the pnames that were parsed correctly.
        /// </summary>
        public readonly List<Pnames> Pnames = new List<Pnames>();
        
        /// <summary>
        /// A list of all the texture1/2/3 that were parsed correctly.
        /// </summary>
        public readonly List<TextureX> TextureX = new List<TextureX>();

        /// <summary>
        /// True if it is safe to read the pnames/textureX since both entries
        /// have been parsed, false otherwise. Note that this being true does
        /// not imply the data is not malformed.
        /// </summary>
        public bool Valid => Pnames.Count > 0 && TextureX.Count > 0;
        
        /// <summary>
        /// Reads the entry as a Pnames lump. Does nothing on failure.
        /// </summary>
        /// <param name="entry">The entry to read.</param>
        public void AddPnames(Entry entry)
        {
            Pnames? pnames = Texture.Pnames.From(entry.ReadData());
            if (pnames != null)
                Pnames.Add(pnames);
            else
                Log.Warn("Unable to parse Pnames from {0}", entry.Path);
        }
        
        /// <summary>
        /// Reads the entry as a TextureX lump. Does nothing on failure.
        /// </summary>
        /// <param name="entry">The entry to read.</param>
        public void AddTextureX(Entry entry)
        {
            TextureX? textureX = Texture.TextureX.From(entry.ReadData());
            if (textureX != null)
                TextureX.Add(textureX);
            else
                Log.Warn("Unable to parse TextureX from {0}", entry.Path);
        }
    }
}