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

namespace Helion.Resources;

public class SkyFireAnimation(int[] firePalette, Texture texture, Image fireImage, int ticks)
{
    public int[] FirePalette = firePalette;
    public Texture Texture = texture;
    public Image FireImage = fireImage;
    public int Ticks = ticks;
    public int CurrentTick;
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
            UpdateSkyFire(skyFire);
        }
    }

    private void UpdateSkyFire(SkyFireAnimation skyFire)
    {
        var fireImage = skyFire.FireImage;

        var fireImageIndices = fireImage.Indices;
        for (int x = 0; x < fireImage.Width; x++)
            for (int y = 1; y < fireImage.Height; y++)
                SpreadFire(fireImageIndices, y * fireImage.Width + x, fireImage.Width);

        var palette = m_archiveCollection.Data.Palette.DefaultLayer;
        WriteSkyFireToTexture(palette, skyFire.FirePalette, fireImage, skyFire.Texture.Image);
    }

    private static void WriteSkyFireToTexture(Color[] palette, int[] skyFirePalette, Image fireImage, Image textureImage)
    {
        var fireIndices = fireImage.Indices;
        var textureIndices = textureImage.Indices;
        var texturePixels = textureImage.Pixels;
        for (int p = 0; p < fireImage.Indices.Length; p++)
        {
            if (fireImage.Indices[p] == 0)
            {
                textureIndices[p] = 0;
                texturePixels[p] = Color.Transparent.Uint;
                continue;
            }

            var index = fireIndices[p];
            if (index >= skyFirePalette.Length)
                continue;

            var paletteIndex = (byte)skyFirePalette[index];
            textureIndices[p] = paletteIndex;
            texturePixels[p] = palette[paletteIndex].Uint;
        }
    }

    private void SpreadFire(Span<byte> indices, int src, int imageWidth)
    {
        var pixel = indices[src];
        if (pixel == 0)
        {
            indices[src - imageWidth] = 0;
        }
        else
        {
            var randIdx = m_fireRandom.NextByte() & 3;
            var dst = src - randIdx + 1;
            var pixelIndex = dst - imageWidth;
            if (pixelIndex < 0 || pixelIndex >= indices.Length)
                return;
            indices[pixelIndex] = (byte)(pixel - (randIdx & 1));
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

