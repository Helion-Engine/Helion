using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Graphics;
using Helion.Graphics.Fonts;
using Helion.Render.Common.Textures;
using Helion.Render.OpenGL.Shared;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configs;
using Font = Helion.Graphics.Fonts.Font;
using Image = Helion.Graphics.Image;

namespace Helion.Render.OpenGL.Texture;

[Flags]
public enum TextureFlags
{
    Default,
    ClampX = 1,
    ClampY = 2,
}

public abstract class GLTextureManager<GLTextureType> : IRendererTextureManager
    where GLTextureType : GLTexture
{
    protected readonly IConfig Config;
    protected readonly ArchiveCollection ArchiveCollection;
    protected readonly ResourceTracker<GLTextureType> TextureTracker = new();
    protected readonly ResourceTracker<GLTextureType> TextureTrackerClamp = new();
    private readonly Dictionary<string, GLFontTexture<GLTextureType>> m_fonts = new(StringComparer.OrdinalIgnoreCase);
    private bool m_disposed;

    private TextureManager TextureManager => ArchiveCollection.TextureManager;

    public abstract IImageDrawInfoProvider ImageDrawInfoProvider { get; }

    /// <summary>
    /// The null texture, intended to be used when the actual texture
    /// cannot be found.
    /// </summary>
    public GLTextureType NullTexture { get; }

    /// <summary>
    /// A fully white texture that can be used for drawing shapes of a
    /// solid color.
    /// </summary>
    public GLTextureType WhiteTexture { get; }

    /// <summary>
    /// The null sprite rotation for when a sprite cannot be found.
    /// </summary>
    public SpriteRotation NullSpriteRotation { get; }

    /// <summary>
    /// The null font, intended to be used when a font cannot be found.
    /// </summary>
    public GLFontTexture<GLTextureType> NullFont { get; }

    protected GLTextureManager(IConfig config, ArchiveCollection archiveCollection)
    {
        Config = config;
        ArchiveCollection = archiveCollection;
        NullTexture = config.Render.NullTexture ? CreateNullTexture() : CreateTransparentNullTexture();
        WhiteTexture = CreateWhiteTexture();
        NullSpriteRotation = CreateNullSpriteRotation();
        NullFont = CreateNullFont();
    }

    private SpriteRotation CreateNullSpriteRotation()
    {
        SpriteRotation spriteFrame = new(new Resources.Texture("NULL", ResourceNamespace.Sprites, 0), false);
        spriteFrame.Texture.RenderStore = NullTexture;
        return spriteFrame;
    }

    ~GLTextureManager()
    {
        Dispose();
    }

    public bool TryGet(string name, [NotNullWhen(true)] out IRenderableTextureHandle? handle, ResourceNamespace? specificNamespace = null)
    {
        if (TryGet(name, specificNamespace ?? ResourceNamespace.Undefined, out GLTextureType texture))
        {
            handle = texture;
            return true;
        }

        handle = null;
        return false;
    }

    public IEnumerable<string> GetNames(ResourceNamespace specificNamespace)
    {
        return ArchiveCollection.ImageRetriever.GetNames(specificNamespace);
    }

    /// <summary>
    /// Checks if the texture manager contains the image.
    /// </summary>
    /// <param name="name">The name of the image.</param>
    /// <returns>True if it does, false if not.</returns>
    public bool Contains(string name)
    {
        return TryGet(name, ResourceNamespace.Global, out _);
    }

    /// <summary>
    /// Gets the texture, with priority given to the namespace provided. If
    /// it cannot be found, the null texture handle is used instead.
    /// </summary>
    /// <param name="name">The texture name.</param>
    /// <param name="priorityNamespace">The namespace to search first.
    /// </param>
    /// <param name="texture">The populated texture. This will either be
    /// the texture you want, or it will be the null image texture.</param>
    /// <returns>True if the texture was found, false if it was not found
    /// and the out value is the null texture handle.</returns>
    public bool TryGet(string name, ResourceNamespace priorityNamespace, out GLTextureType texture)
    {
        texture = NullTexture;
        if (name == Constants.NoTexture)
            return false;

        GLTextureType? textureForNamespace = TextureTracker.GetOnly(name, priorityNamespace);
        if (textureForNamespace != null)
        {
            texture = textureForNamespace;
            return true;
        }

        // The reason we do this check before checking other namespaces is
        // that we can end up missing the texture for the namespace in some
        // pathological scenarios. Suppose we draw some texture that shares
        // a name with some flat. Then suppose we try to draw the flat. If
        // we check the GL texture cache first, we will find the texture
        // and miss the flat and then never know that there is a specific
        // flat that should have been used.
        Image? imageForNamespace = ArchiveCollection.ImageRetriever.GetOnly(name, priorityNamespace);
        if (imageForNamespace != null)
        {
            texture = CreateTexture(imageForNamespace, name, priorityNamespace);
            return true;
        }

        // Now that nothing in the desired namespace was found, we will
        // accept anything.
        GLTextureType? anyTexture = TextureTracker.Get(name, priorityNamespace);
        if (anyTexture != null)
        {
            texture = anyTexture;
            return true;
        }

        // Note that because we are getting any texture, we don't want to
        // use the provided namespace since if we ask for a flat, but get a
        // texture, and then index it as a flat... things probably go bad.
        Image? image = ArchiveCollection.ImageRetriever.Get(name, priorityNamespace);
        if (image == null)
            return false;

        texture = CreateTexture(image, name, image.Namespace);
        return true;
    }

    public GLTextureType GetTexture(int index, bool repeatY = true)
    {
        var texture = TextureManager.GetTexture(index);
        var renderTexture = repeatY ? texture.RenderStore : texture.RenderStoreClamp;

        if (renderTexture != null)
            return (GLTextureType)renderTexture;

        if (texture.Image == null)
        {
            renderTexture = CreateTexture(texture.Image, repeatY);
            texture.SetGLTexture(renderTexture, repeatY);
            return (GLTextureType)renderTexture;
        }

        renderTexture = CreateTexture(texture.Image, texture.Name, texture.Image.Namespace, repeatY);
        texture.SetGLTexture(renderTexture, repeatY);
        return (GLTextureType)renderTexture;
    }

    /// <summary>
    /// Get a sprite rotation.
    /// </summary>
    /// <param name="spriteDefinition">The sprite definition.</param>
    /// <param name="frame">Sprite frame.</param>
    /// <param name="rotation">Rotation.</param>
    /// <returns>Returns a SpriteRotation if sprite name, frame, and rotation are valid. Otherwise null.</returns>
    public SpriteRotation GetSpriteRotation(SpriteDefinition spriteDefinition, int frame, uint rotation, int colorMapIndex)
    {
        SpriteRotation? spriteRotation = spriteDefinition.GetSpriteRotation(frame, rotation);
        if (spriteRotation == null)
            return NullSpriteRotation;

        colorMapIndex--;
        if (!ArchiveCollection.StoreImageIndices && colorMapIndex >= 0)
        {
            // For true color mode the image needs to be recreated using a different palette
            if (spriteRotation.TranslationSpriteRotations.TryGetValue(colorMapIndex, out var translationRotation))
                return translationRotation;

            if (colorMapIndex >= ArchiveCollection.Definitions.Colormaps.Count)
                return NullSpriteRotation;

            string translatedName = spriteRotation.Texture.Name + "-ColorMap-" + colorMapIndex;
            var texture = new Resources.Texture(translatedName, ResourceNamespace.Sprites, 0)
            {
                Image = ArchiveCollection.ImageRetriever.GetOnlyMapped(translatedName, spriteRotation.Texture.Name, ResourceNamespace.Sprites,
                    colorTranslation: ArchiveCollection.Definitions.Colormaps[colorMapIndex].IndexLayer(0))
            };

            translationRotation = new SpriteRotation(texture, spriteRotation.Mirror)
            {
                RenderStore = CreateTexture(texture.Image, translatedName, ResourceNamespace.Sprites)
            };

            spriteRotation.TranslationSpriteRotations[colorMapIndex] = translationRotation;
            return translationRotation;
        }
        else
        {
            spriteRotation.RenderStore ??= CreateTexture(spriteRotation.Texture.Image, spriteRotation.Texture.Name, ResourceNamespace.Sprites);
        }

        return spriteRotation;
    }

    public void CacheSpriteRotations(SpriteDefinition spriteDef)
    {
        for (int i = 0; i < SpriteDefinition.MaxFrames; i++)
        {
            for (int j = 0; j < SpriteDefinition.MaxRotations; j++)
            {
                var rotation = spriteDef.Rotations[i, j];
                if (rotation == null)
                    continue;

                if (rotation.Texture.RenderStore != null)
                    continue;

                rotation.Texture.RenderStore = CreateTexture(rotation.Texture.Image, rotation.Texture.Name, ResourceNamespace.Sprites);
            }
        }
    }

    public SpriteDefinition? GetSpriteDefinition(int spriteIndex)
    {
        SpriteDefinition? spriteDef = TextureManager.GetSpriteDefinition(spriteIndex);
        return spriteDef;
    }

    /// <summary>
    /// Gets the texture, with priority given to the sprite namespace. If
    /// it cannot be found, the null texture handle is returned.
    /// </summary>
    /// <param name="name">The flat texture name.</param>
    /// <param name="texture">The populated texture. This will either be
    /// the texture you want, or it will be the null image texture.</param>
    /// <returns>True if the texture was found, false if it was not found
    /// and the out value is the null texture handle.</returns>
    public bool TryGetSprite(string name, out GLTextureType texture)
    {
        return TryGet(name, ResourceNamespace.Sprites, out texture);
    }

    /// <summary>
    /// Gets the font for the name, or returns a default font that will
    /// contain null images.
    /// </summary>
    /// <param name="name">The name of the font.</param>
    /// <returns>The font for the provided name, otherwise the 'Null
    /// texture' (which isn't null but is made up of textures that are the
    /// missing texture image).</returns>
    public GLFontTexture<GLTextureType> GetFont(string name)
    {
        if (m_fonts.TryGetValue(name, out GLFontTexture<GLTextureType>? existingFontTexture))
            return existingFontTexture;

        Font? font = ArchiveCollection.GetFont(name);
        if (font != null)
            return CreateNewFont(font, name);

        return NullFont;
    }

    public virtual void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    protected static int CalculateMipmapLevels(Dimension dimension)
    {
        int smallerAxis = Math.Min(dimension.Width, dimension.Height);
        return (int)Math.Floor(Math.Log(smallerAxis, 2));
    }

    protected GLTextureType CreateTexture(Image? image, bool repeatY) => CreateTexture(image, null, ResourceNamespace.Global, repeatY);

    protected GLTextureType CreateTexture(Image? image, string? name, ResourceNamespace resourceNamespace, bool repeatY = true)
    {
        var textureTracker = GetTextureTracker(repeatY);
        GLTextureType? texture;
        if (name != null)
        {
            texture = textureTracker.GetOnly(name, resourceNamespace);
            if (texture != null)
                return texture;
        }

        if (image == null)
        {
            texture = NullTexture;
            return texture;
        }

        TextureFlags flags = repeatY ? TextureFlags.Default : TextureFlags.ClampY;
        texture = GenerateTexture(image, name ?? string.Empty, resourceNamespace, flags);
        if (name != null)
            textureTracker.Insert(name, resourceNamespace, texture);

        return texture;
    }

    private ResourceTracker<GLTextureType> GetTextureTracker(bool repeatY) =>
        repeatY ? TextureTracker : TextureTrackerClamp;

    protected void DeleteTexture(GLTextureType texture, string name, ResourceNamespace resourceNamespace)
    {
        TextureTracker.Remove(name, resourceNamespace);
        TextureTrackerClamp.Remove(name, resourceNamespace);
        texture.Dispose();
    }

    private void ReleaseUnmanagedResources()
    {
        if (m_disposed)
            return;

        NullTexture.Dispose();
        WhiteTexture.Dispose();
        NullFont.Dispose();

        TextureTracker.GetValues().ForEach(texture => texture?.Dispose());
        TextureTrackerClamp.GetValues().ForEach(texture => texture?.Dispose());

        foreach (var pair in m_fonts)
            pair.Value.Dispose();

        m_disposed = true;
    }

    protected abstract GLTextureType GenerateTexture(Image image, string name, ResourceNamespace resourceNamespace, TextureFlags flags = TextureFlags.Default);

    protected abstract GLFontTexture<GLTextureType> GenerateFont(Font font, string name);

    private GLTextureType CreateNullTexture()
    {
        return GenerateTexture(Image.NullImage, "NULL", ResourceNamespace.Global);
    }

    private GLTextureType CreateTransparentNullTexture()
    {
        return GenerateTexture(Image.TransparentImage, "NULL", ResourceNamespace.Global);
    }

    private GLTextureType CreateWhiteTexture()
    {
        Image whiteImage = new(1, 1, ImageType.Argb);
        whiteImage.Fill(Color.White);

        return GenerateTexture(whiteImage, "NULLWHITE", ResourceNamespace.Global);
    }

    private GLFontTexture<GLTextureType> CreateNullFont()
    {
        const string NullFontName = "NULL";

        Dictionary<char, Glyph> glyphs = new()
        {
            ['?'] = new Glyph('?', Box2F.UnitBox, Box2I.UnitBox)
        };

        Font font = new(NullFontName, glyphs, Image.NullImage);
        return GenerateFont(font, NullFontName);
    }

    private GLFontTexture<GLTextureType> CreateNewFont(Font font, string name)
    {
        GLFontTexture<GLTextureType> fontTexture = GenerateFont(font, name);

        DeleteOldFontIfAny(name);
        m_fonts[name] = fontTexture;

        return fontTexture;
    }

    private void DeleteOldFontIfAny(string name)
    {
        if (m_fonts.TryGetValue(name, out GLFontTexture<GLTextureType>? texture))
        {
            texture.Dispose();
            m_fonts.Remove(name);
        }
    }
}
