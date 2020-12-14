﻿using System.Collections.Generic;
using Helion.Util;
using Helion.Util.Bytes;
using Helion.Util.Geometry.Vectors;
using NLog;

namespace Helion.ResourcesNew.Definitions.Textures
{
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
            List<TextureXImage> definitions = new();

            try
            {
                ByteReader reader = new(data);

                int numTextures = reader.Int();

                List<int> dataOffsets = new();
                for (int offsetIndex = 0; offsetIndex < numTextures; offsetIndex++)
                    dataOffsets.Add(reader.Int());

                foreach (int dataOffset in dataOffsets)
                {
                    reader.Position = dataOffset;

                    string name = reader.EightByteString().ToUpper();
                    reader.Advance(4); // Skip flags/scalex/scaley.
                    int width = reader.Short();
                    int height = reader.Short();
                    reader.Advance(4); // Skip columndirectory, so no Strife.
                    int numPatches = reader.Short();

                    List<TextureXPatch> patches = new();
                    for (int patchIndex = 0; patchIndex < numPatches; patchIndex++)
                    {
                        Vec2I patchOffset = new Vec2I(reader.Short(), reader.Short());
                        short index = reader.Short();
                        reader.Advance(4); // Skip stepdir/colormap

                        TextureXPatch patch = new(index, patchOffset);
                        patches.Add(patch);
                    }

                    TextureXImage textureXImage = new(name, width, height, patches);
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
            List<TextureDefinition> definitions = new();

            foreach (TextureXImage image in Definitions)
            {
                List<TextureDefinitionComponent> components = CreateComponents(image, pnames);
                TextureDefinition texDefinition = new(image.Name, image.Dimension, Namespace.Textures, components);
                definitions.Add(texDefinition);
            }

            return definitions;
        }

        private List<TextureDefinitionComponent> CreateComponents(TextureXImage image, Pnames pnames)
        {
            List<TextureDefinitionComponent> components = new();

            foreach (TextureXPatch patch in image.Patches)
            {
                if (patch.PnamesIndex < 0 || patch.PnamesIndex >= pnames.Names.Count)
                {
                    Log.Warn("Corrupt pnames index in texture {0}, texture will be corrupt", image.Name);
                    continue;
                }

                CIString name = pnames.Names[patch.PnamesIndex];
                TextureDefinitionComponent component = new(name, patch.Offset);
                components.Add(component);
            }

            return components;
        }
    }
}