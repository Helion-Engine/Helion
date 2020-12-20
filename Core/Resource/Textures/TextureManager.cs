using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using Helion.Graphics;
using Helion.Graphics.Palette;
using Helion.Resource.Archives;
using Helion.Resource.Definitions.Textures;
using Helion.Util;
using NLog;
using Image = Helion.Graphics.Image;
using static Helion.Graphics.Palette.PaletteReaders;

namespace Helion.Resource.Textures
{
    public class TextureManager
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly Namespace[] NamespacesToCheck =
        {
            Namespace.Textures,
            Namespace.Graphics,
            Namespace.Sprites,
            Namespace.Flats,
            Namespace.Global
        };

        public readonly Texture MissingTexture;
        private readonly Resources m_resources;
        private readonly TextureDefinitionManager m_textureDefinitions;
        private readonly List<Texture> m_textures = new();
        private readonly NamespaceTracker<Texture> m_textureTracker = new();
        private readonly NamespaceTracker<Image> m_imageTracker = new();

        public TextureManager(Resources resources, TextureDefinitionManager textureDefinitions)
        {
            m_resources = resources;
            m_textureDefinitions = textureDefinitions;

            MissingTexture = CreateMissingTexture();
            m_textures.Add(MissingTexture);
        }

        /// <summary>
        /// Gets any texture, but will look first for whatever priority is
        /// provided (by default is global)
        /// </summary>
        /// <param name="name">The texture name.</param>
        /// <param name="priorityNamespace">The priority namespace to search
        /// for first before looking elsewhere.</param>
        /// <returns>The texture, or the missing texture.</returns>
        public Texture Get(CIString name, Namespace priorityNamespace = Namespace.Global)
        {
            // We need to check for stuff specific to our namespace first. This
            // is required because if we ask for a flat, but happen to have a
            // texture with the same name (which wads like to do a lot...) then
            // we want to look for the flat first. It would be a mistake to get
            // the wall texture first, because we will always succeed and never
            // look for the flat.
            if (TryGetOnlyInternal(name, priorityNamespace, out Texture? texture))
                return texture;

            foreach (Namespace resourceNamespace in NamespacesToCheck)
            {
                if (resourceNamespace == priorityNamespace)
                    continue;

                if (TryGetOnlyInternal(name, resourceNamespace, out texture))
                {
                    TrackTexture(name, priorityNamespace, texture);
                    return texture;
                }
            }

            return MissingTexture;
        }

        /// <summary>
        /// Gets the texture with the specific namespace provided. Will not
        /// look in any other namespace.
        /// </summary>
        /// <param name="name">The texture name.</param>
        /// <param name="resourceNamespace">The namespace.</param>
        /// <returns>The texture, or the missing texture if unable to find a
        /// texture from the namespace provided.</returns>
        public Texture GetOnly(CIString name, Namespace resourceNamespace)
        {
            if (TryGetOnlyInternal(name, resourceNamespace, out Texture? texture))
                return texture;

            // If we can't find it, remember that we could not find it so we
            // can do an early out with one check, instead of doing a ton of
            // checks again.
            m_textureTracker.Insert(name, resourceNamespace, MissingTexture);
            return MissingTexture;
        }

        private bool TryGetOnlyInternal(CIString name, Namespace resourceNamespace,
            [NotNullWhen(true)] out Texture? texture)
        {
            texture = m_textureTracker.GetOnly(name, resourceNamespace);
            if (texture != null)
                return true;

            // We want to look for definitions before the image storage, as it
            // may be the case that someone redefines an image through a new
            // definition with the same name (which happens very frequently)
            // and does something that changes it.
            TextureDefinition? definition = m_textureDefinitions.GetOnly(name, resourceNamespace);
            if (definition != null)
            {
                texture = CreateFromDefinitionAndTrack(name, resourceNamespace, definition);
                return true;
            }

            Image? image = m_imageTracker.GetOnly(name, resourceNamespace);
            if (image != null)
            {
                texture = CreateFromImageAndTrack(name, resourceNamespace, image);
                return true;
            }

            image = TryEntryToImageAndTrack(name, resourceNamespace);
            if (image != null)
            {
                texture = CreateFromImageAndTrack(name, resourceNamespace, image);
                return true;
            }

            return false;
        }

        private Texture CreateFromDefinitionAndTrack(CIString name, Namespace resourceNamespace,
            TextureDefinition definition)
        {
            Image image = new(definition.Width, definition.Height, Color.Transparent, definition.Namespace);

            foreach (TextureDefinitionComponent component in definition.Components)
            {
                Image? existingImage = FindImageNonRecursive(component.Name, resourceNamespace);
                if (existingImage == null)
                {
                    Log.Warn("Texture '{0}' is missing definition component '{1}'", name, component.Name);
                    continue;
                }

                existingImage.DrawOnTopOf(image, component.Offset);
            }

            return CreateFromImageAndTrack(name, resourceNamespace, image);
        }

        private Image? FindImageNonRecursive(CIString name, Namespace resourceNamespace)
        {
            // We do not call any functions that could be recursive since we
            // don't want to blow the stack. This is intended for the cases
            // where there's an image defined as itself, which happens a lot
            // in wads (thanks to PNAMES/TEXTUREx).
            Texture? texture = m_textureTracker.GetOnly(name, resourceNamespace);
            if (texture != null)
                return texture.Image;

            Image? image = m_imageTracker.GetOnly(name, resourceNamespace);
            if (image != null)
                return image;

            return TryEntryToImageAndTrack(name, resourceNamespace);
        }

        private Image? TryEntryToImageAndTrack(CIString name, Namespace resourceNamespace)
        {
            Entry? entry = m_resources.Find(name, resourceNamespace);
            return entry != null ? EntryToImageAndTrack(entry, resourceNamespace) : null;
        }

        private Texture CreateFromImageAndTrack(CIString name, Namespace resourceNamespace, Image image)
        {
            Texture texture = new(name, image, resourceNamespace, m_textures.Count);
            TrackTexture(name, resourceNamespace, texture);

            return texture;
        }

        private Image? EntryToImageAndTrack(Entry entry, Namespace resourceNamespace)
        {
            Image? image = null;
            byte[] data = entry.ReadData();

            Func<byte[], Namespace, PaletteImage?> reader = LikelyFlat(data) ? ReadFlat : ReadColumn;
            PaletteImage? paletteImage = reader(data, resourceNamespace);
            if (paletteImage != null)
                image = paletteImage.ToImage(m_resources.Palette);

            if (image != null)
                m_imageTracker.Insert(entry.Path.Name, resourceNamespace, image);

            return image;
        }

        private void TrackTexture(CIString name, Namespace resourceNamespace, Texture texture)
        {
            m_textures.Add(texture);
            m_textureTracker.Insert(name, resourceNamespace, texture);
        }

        private static Texture CreateMissingTexture()
        {
            Image nullImage = ImageHelper.CreateNullImage();
            return new("", nullImage, Namespace.Global, 0, true);
        }
    }
}
