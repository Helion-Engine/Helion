using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Images;
using Helion.Resources.Definitions.Id24;

namespace Helion.Resources;

public partial class TextureManager
{
    private readonly Dictionary<int, int> m_flatIndexToSkyTextureIndex = [];
    private readonly Dictionary<int, SkyTransform> m_textureIndexToSkyTransform = [];
    private readonly List<SkyTransform> m_skyTransforms = [];

    public bool TryGetSkyTransform(int textureIndex, [NotNullWhen(true)] out SkyTransform? skyTransform)
    {
        return m_textureIndexToSkyTransform.TryGetValue(textureIndex, out skyTransform);
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

            int? foregroundTextureIndex = null;
            if (sky.ForegroundTex != null)
                foregroundTextureIndex = CreateForegroundSky(sky, sky.ForegroundTex, foregroundTextureIndex);

            if (sky.Fire != null)
                CreateSkyFireTextures(sky, sky.Fire);

            var skyTransform = SkyTransform.FromId24SkyDef(texture.Index, foregroundTextureIndex, sky);
            m_skyTransforms.Add(skyTransform);
            m_textureIndexToSkyTransform[texture.Index] = skyTransform;
            break;
        }

        LoadTextureImage(texture.Index);
        return texture;
    }

    private int? CreateForegroundSky(SkyDef sky, SkyForeTex skyForeTex, int? foregroundTextureIndex)
    {
        if (sky.Type != SkyType.WithForeground)
        {
            Log.Error($"Sky {sky.Name} with ForegroundTex definition has invalid type ${sky.Type}");
            return null;
        }

        var foregroundTexture = GetTexture(skyForeTex.Name, ResourceNamespace.Textures);
        LoadTextureImage(foregroundTexture.Index, GetImageOptions.ClearBlackPixels);
        if (foregroundTexture != null)
            foregroundTextureIndex = foregroundTexture.Index;
        return foregroundTextureIndex;
    }

    private void CreateSkyFireTextures(SkyDef sky, SkyFire fire)
    {
        if (sky.Type != SkyType.Fire)
        {
            Log.Error($"Sky {sky.Name} with Fire definition has invalid type ${sky.Type}");
            return;
        }
    }
}

