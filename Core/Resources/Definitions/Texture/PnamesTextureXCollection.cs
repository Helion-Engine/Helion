using System.Collections.Generic;

namespace Helion.Resources.Definitions.Texture
{
    /// <summary>
    /// Holds references to pnames and texture1/2/3 definition entries.
    /// </summary>
    /// <remarks>
    /// Designed as a wrapper around both types since the 
    /// </remarks>
    public class PnamesTextureXCollection
    {
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
        /// Adds a correctly read pnames to the tracker.
        /// </summary>
        /// <param name="pnames">The pnames to add.</param>
        public void Add(Pnames pnames)
        {
            Pnames.Add(pnames);
        }
        
        /// <summary>
        /// Adds a correctly parsed texture1/2/3 to the tracker.
        /// </summary>
        /// <param name="textureX">The texture1/2/3 data.</param>
        public void Add(TextureX textureX)
        {
            TextureX.Add(textureX);
        }
    }
}