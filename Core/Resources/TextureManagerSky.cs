using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Images;
using Helion.Resources.Definitions.Id24;
using System.Linq;
using Helion.Graphics;
using Helion.Util.RandomGenerators;
using Helion.Util;
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;
using Helion.Util.Extensions;
using Helion.Geometry.Vectors;

namespace Helion.Resources;

public class SkyFireAnimation(int[] firePalette, Texture texture, Image fireImage, int ticks)
{
    public int[] FirePalette = firePalette;
    public Texture Texture = texture;
    public Image FireImage = fireImage;
    public int Ticks = ticks;
    public int CurrentTick;
    public bool RenderUpdate = true;
}

public partial class TextureManager
{
    public List<SkyFireAnimation> GetSkyFireTextures() => m_skyFireTextures;

    private readonly Dictionary<int, int> m_flatIndexToSkyTextureIndex = [];
    private readonly Dictionary<int, SkyTransform> m_textureIndexToSkyTransform = [];
    private readonly List<SkyTransform> m_skyTransforms = [];
    private readonly List<SkyFireAnimation> m_skyFireTextures = [];
    private readonly DoomRandom m_fireRandom = new();

    public bool TryGetSkyTransform(int textureIndex, [NotNullWhen(true)] out SkyTransform? skyTransform)
    {
        return m_textureIndexToSkyTransform.TryGetValue(textureIndex, out skyTransform);
    }
    
    private void TickSkyFire()
    {
        for (int i = 0; i < m_skyFireTextures.Count; i++)
        {
            var skyFire = m_skyFireTextures[i];
            if (skyFire.Texture.Image == null)
                continue;

            skyFire.CurrentTick++;
            if (skyFire.CurrentTick < skyFire.Ticks)
                continue;
                        
            skyFire.CurrentTick = 0;
            skyFire.RenderUpdate = true;
            UpdateSkyFire(skyFire);
            var palette = m_archiveCollection.Data.Palette.DefaultLayer;
            WriteSkyFireToTexture(palette, skyFire.FirePalette, skyFire.FireImage, skyFire.Texture.Image);
        }
    }

    private void UpdateSkyFire(SkyFireAnimation skyFire)
    {
        var fireImage = skyFire.FireImage;
        var fireImageIndices = fireImage.m_indices;
        int fireImageWidth = fireImage.Width;
        int fireImageHeight = fireImage.Height;
        for (int x = 0; x < fireImageWidth; x++)
        {
            for (int y = 1; y < fireImageHeight; y++)
            {
                int src = y * fireImageWidth + x;
                var pixel = fireImageIndices[src];
                if (pixel == 0)
                {
                    fireImageIndices[src - fireImageWidth] = 0;
                }
                else
                {
                    var randIdx = m_fireRandom.NextByte() & 3;
                    var dst = src - randIdx + 1;
                    var pixelIndex = dst - fireImageWidth;
                    if (pixelIndex < 0 || pixelIndex >= fireImageIndices.Length)
                        return;
                    fireImageIndices[pixelIndex] = (byte)(pixel - (randIdx & 1));
                }
            }
        }
    }

    private static void WriteSkyFireToTexture(Color[] palette, int[] skyFirePalette, Image fireImage, Image textureImage)
    {
        var fireIndices = fireImage.m_indices;
        var texturePixels = textureImage.m_pixels;
        var transparentColor = Color.Transparent.Uint;
        int skyFirePaletteLength = skyFirePalette.Length;
        for (int p = 0; p < fireIndices.Length; p++)
        {
            if (fireIndices[p] == 0)
            {
                texturePixels[p] = ShaderVars.PaletteColorMode ? 0 : transparentColor;
                continue;
            }

            var index = fireIndices[p];
            if (index >= skyFirePaletteLength)
                continue;

            // Directly writing the magic values for palette color mode here.
            // This prevents creating a new array and iterating the indices again when uploading to the GPU.
            var paletteIndex = (byte)skyFirePalette[index];
            texturePixels[p] = ShaderVars.PaletteColorMode ? (uint)(Image.AlphaFlag << 24 | (byte)paletteIndex << 16) : palette[paletteIndex].m_value;
        }
    }

    private void SetSkyFireTextures()
    {
        const int FireImageWidth = 320;
        const int FireImageHeight = 168;
        var skyDefinition = m_archiveCollection.Definitions.Id24SkyDefinition;
        foreach (var sky in skyDefinition.Data.Skies)
        {
            if (sky.Type != SkyType.Fire || sky.Fire == null)
                continue;

            var texture = GetTexture(sky.Name, ResourceNamespace.Textures);
            if (texture == null)
                continue;

            texture.Image = new Image((FireImageWidth, FireImageHeight), ImageType.PaletteWithArgb);
            texture.Image.Fill(Color.Transparent);
            texture.Image.FillIndices(0);

            var fireImage = new Image((FireImageWidth, FireImageHeight), ImageType.Palette);
            InitSkyFireImage(sky.Fire.Palette, fireImage);
            var skyFireAnimation = new SkyFireAnimation(sky.Fire.Palette, texture, fireImage, CalcFireTicks(sky.Fire));
            m_skyFireTextures.Add(skyFireAnimation);

            for (int i = 0; i < 64; i++)
                UpdateSkyFire(skyFireAnimation);

            var palette = m_archiveCollection.Data.Palette.DefaultLayer;
            WriteSkyFireToTexture(palette, sky.Fire.Palette, fireImage, texture.Image);

            FlagSkyTrasformForegroundAsFire(sky);
        }
    }

    private void FlagSkyTrasformForegroundAsFire(SkyDef sky)
    {
        foreach (var skyTransform in m_skyTransforms)
        {
            if (skyTransform.Foreground == null || !skyTransform.Foreground.TextureName.EqualsIgnoreCase(sky.Name))
                continue;

            skyTransform.Foreground.Type = SkyTransformType.Fire;
            skyTransform.Foreground.Scale.Y = 1;
            skyTransform.Foreground.Scale.X = 1;
            skyTransform.Foreground.Offset = Vec2F.Zero;
        }
    }

    private static void InitSkyFireImage(int[] firePalette, Image fireImage)
    {
        fireImage.FillIndices(0);

        if (firePalette.Length == 0)
            return;

        byte fillIndex = (byte)(firePalette.Length - 1);
        for (int x = 0; x < fireImage.Width; x++)
            fireImage.SetIndex(x, fireImage.Height - 1, fillIndex);
    }

    private static int CalcFireTicks(SkyFire fire)
    {
        if (fire.UpdateTime <= 0)
            return 1;
        var seconds = 1 / Constants.TicksPerSecond;
        return Math.Max((int)(fire.UpdateTime / seconds), 1);
    }

    private void MapSkyFlat(Entry flat, int textureIndex)
    {
        var skyDefinition = m_archiveCollection.Definitions.Id24SkyDefinition;
        if (!skyDefinition.FlatMapping.TryGetValue(flat.Path.Name, out var textureName))
            return;

        var texture = MapSkyTexture(textureName, skyDefinition);
        if (texture != null)
            m_flatIndexToSkyTextureIndex[textureIndex] = texture.Index;
    }

    private Texture? MapSkyTexture(string textureName, Id24SkyDefinition skyDefinition)
    {
        if (!m_textureLookup.TryGetValue(textureName, out var texture))
        {
            Log.Error($"Could not find texture {textureName} for sky {textureName}");
            return null;
        }

        for (int i = 0; i < skyDefinition.Data.Skies.Count; i++)
        {
            var sky = skyDefinition.Data.Skies[i];
            if (!sky.Name.Equals(texture.Name, StringComparison.OrdinalIgnoreCase))
                continue;

            int? foregroundSkyHandle = null;
            if (sky.ForegroundTex != null)
                foregroundSkyHandle = CreateForegroundSky(sky, sky.ForegroundTex);

            if (sky.Fire != null)
                CreateSkyFireTexture(sky, sky.Fire);

            var skyTransform = SkyTransform.FromId24SkyDef(texture.Index, foregroundSkyHandle, sky);
            m_skyTransforms.Add(skyTransform);
            m_textureIndexToSkyTransform[texture.Index] = skyTransform;
            break;
        }

        LoadTextureImage(texture.Index);
        return texture;
    }

    private int? CreateForegroundSky(SkyDef sky, SkyForeTex skyForeTex)
    {
        if (sky.Type != SkyType.WithForeground)
        {
            Log.Error($"Sky {sky.Name} with ForegroundTex definition has invalid type ${sky.Type}");
            return null;
        }

        var fireSkyDef = m_archiveCollection.Definitions.Id24SkyDefinition.Data.Skies
            .FirstOrDefault(x => x.Type == SkyType.Fire && x.Name.Equals(skyForeTex.Name, StringComparison.OrdinalIgnoreCase));

        if (fireSkyDef != null)
            return CreateSkyFireTexture(fireSkyDef, fireSkyDef.Fire);

        return CreateSkyForegroundTexture(skyForeTex.Name);
    }

    private int? CreateSkyForegroundTexture(string textureName)
    {
        var foregroundTexture = GetTexture(textureName, ResourceNamespace.Textures);
        LoadTextureImage(foregroundTexture.Index, GetImageOptions.ClearBlackPixels);
        if (foregroundTexture != null)
            return foregroundTexture.Index;
        return null;
    }

    private int? CreateSkyFireTexture(SkyDef sky, SkyFire? fire)
    {
        if (sky.Type != SkyType.Fire)
        {
            Log.Error($"Sky {sky.Name} with Fire definition has invalid type ${sky.Type}");
            return null;
        }

        if (fire == null)
        {
            Log.Error($"Sky {sky.Name} with Fire type missing fire definition");
            return null;
        }

        return CreateSkyForegroundTexture(sky.Name);
    }
}

