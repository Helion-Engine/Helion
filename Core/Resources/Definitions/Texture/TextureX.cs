using System;
using System.Collections.Generic;
using Helion.Util;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;
using NLog;
using static Helion.Util.Assertion.Assert;

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
        public readonly CIString Name;

        /// <summary>
        /// The width of the texture.
        /// </summary>
        public readonly int Width;

        /// <summary>
        /// The height of the texture.
        /// </summary>
        public readonly int Height;

        /// <summary>
        /// All the patches that make up the texture.
        /// </summary>
        public readonly List<TextureXPatch> Patches;
        
        /// <summary>
        /// Gets the dimension for this image.
        /// </summary>
        public Dimension Dimension => new Dimension(Width, Height);

        /// <summary>
        /// Creates a new texture1/2/3 image.
        /// </summary>
        /// <param name="name">The name of the image.</param>
        /// <param name="width">The image width.</param>
        /// <param name="height">The image height.</param>
        /// <param name="patches">All the patches that make up the texture.
        /// </param>
        public TextureXImage(CIString name, int width, int height, List<TextureXPatch> patches)
        {
            Precondition(width >= 0, "TextureX image width must not be negative");
            Precondition(height >= 0, "TextureX image height must not be negative");

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
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// All the different textures that make up this data structure.
        /// </summary>
        public readonly List<TextureXImage> Definitions;

        private TextureX(List<TextureXImage> definitions)
        {
            Definitions = definitions;            
        }

        /// <summary>
        /// Attempts to read the Texture1/2/3 data.
        /// </summary>
        /// <param name="data">The data to read.</param>
        /// <returns>The Texture1/2/3 data, or an empty value if the data is
        /// corrupt.</returns>
        public static TextureX? From(byte[] data)
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

                    string name = reader.ReadEightByteString().ToUpper();
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
                Log.Warn("Corrupt TextureX entry, textures will likely be missing");
                return null;
            }
        }

        /// <summary>
        /// Creates a series of texture definitions from the pnames provided.
        /// </summary>
        /// <param name="pnames">The pnames to make the texture definitions
        /// with.</param>
        /// <returns>A list of all the texture definitions.</returns>
        public List<TextureDefinition> ToTextureDefinitions(Pnames pnames)
        {
            List<TextureDefinition> definitions = new List<TextureDefinition>();

            foreach (TextureXImage image in Definitions)
            {
                List<TextureDefinitionComponent> components = CreateComponents(image, pnames);
                definitions.Add(new TextureDefinition(image.Name, image.Dimension, ResourceNamespace.Textures, components));
            }

            return definitions;
        }

        private List<TextureDefinitionComponent> CreateComponents(TextureXImage image, Pnames pnames)
        {
            List<TextureDefinitionComponent> components = new List<TextureDefinitionComponent>();
            
            foreach (TextureXPatch patch in image.Patches)
            {
                if (patch.PnamesIndex < 0 || patch.PnamesIndex >= pnames.Names.Count)
                {
                    Log.Warn("Corrupt pnames index in texture {0}, texture will be corrupt", image.Name);
                    continue;
                }

                CIString name = pnames.Names[patch.PnamesIndex];
                TextureDefinitionComponent component = new TextureDefinitionComponent(name, patch.Offset);
                components.Add(component);
            }

            return components;
        }
    }
}