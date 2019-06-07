using Helion.Util;
using Helion.Util.Geometry;
using System;
using System.Collections.Generic;
using static Helion.Util.Assert;

namespace Helion.Resources.Definitions.Texture
{
    /// <summary>
    /// A single patch that makes up a Texture1/2/3 data structure.
    /// </summary>
    public class TextureXPatch
    {
        /// <summary>
        /// The index into the Pnames data for this texture.
        /// </summary>
        public short PnamesIndex { get; }

        /// <summary>
        /// The X/Y offset of this texture.
        /// </summary>
        public Vec2I Offset { get; }

        public TextureXPatch(short pnamesIndex, Vec2I offset)
        {
            Precondition(pnamesIndex >= 0, "Texture X patch has a negative pnames index");

            PnamesIndex = Math.Max((short)0, pnamesIndex);
            Offset = offset;
        }
    }

    /// <summary>
    /// A single image in a TextureX data structure.
    /// </summary>
    public class TextureXImage
    {
        /// <summary>
        /// The name of the texture.
        /// </summary>
        public UpperString Name { get; }

        /// <summary>
        /// The width of the texture.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// The height of the texture.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// All the patches that make up the texture.
        /// </summary>
        public List<TextureXPatch> Patches { get; }

        public TextureXImage(UpperString name, int width, int height, List<TextureXPatch> patches)
        {
            Precondition(width >= 0, "Texture X image width must not be negative");
            Precondition(height >= 0, "Texture X image height must not be negative");

            Name = name;
            Patches = patches;
            Width = Math.Max(0, width);
            Height = Math.Max(0, height);
        }
    }

    /// <summary>
    /// The data structure for a Texture1/2/3 entry.
    /// </summary>
    public class TextureX
    {
        /// <summary>
        /// All the different textures.
        /// </summary>
        public List<TextureXImage> Definitions;

        public TextureX() : this(new List<TextureXImage>())
        {
        }

        private TextureX(List<TextureXImage> definitions) => Definitions = definitions;

        /// <summary>
        /// Attempts to read the Texture1/2/3 data.
        /// </summary>
        /// <param name="data">The data to read.</param>
        /// <returns>The Texture1/2/3 data, or an empty value if the data is
        /// corrupt.</returns>
        public TextureX? From(byte[] data)
        {
            List<TextureXImage> definitions = new List<TextureXImage>();

            try
            {
                ByteReader reader = new ByteReader(data);

                int numTextures = reader.ReadInt32();

                List<int> dataOffsets = new List<int>();
                for (int offsetIndex = 0; offsetIndex < numTextures; offsetIndex++)
                    dataOffsets.Add(reader.ReadInt32());

                foreach (int dataOffset in dataOffsets)
                {
                    reader.Offset(dataOffset);

                    string name = reader.ReadEightByteString();
                    reader.Advance(4); // Skip flags/scalex/scaley.
                    int width = reader.ReadInt16();
                    int height = reader.ReadInt16();
                    reader.Advance(4); // Skip columndirectory, so no Strife.
                    int numPatches = reader.ReadInt16();

                    List<TextureXPatch> patches = new List<TextureXPatch>();
                    for (int patchIndex = 0; patchIndex < numPatches; patchIndex++)
                    {
                        Vec2I patchOffset = new Vec2I(reader.ReadInt16(), reader.ReadInt16());
                        short index = reader.ReadInt16();
                        reader.Advance(4); // Skip stepdir/colormap

                        patches.Add(new TextureXPatch(index, patchOffset));
                    }

                    TextureXImage textureXImage = new TextureXImage(name, width, height, patches);
                    definitions.Add(textureXImage);
                }

                return new TextureX(definitions);
            }
            catch
            {
                return null;
            }
        }
    }
}
