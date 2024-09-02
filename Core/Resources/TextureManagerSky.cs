using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Images;
using Helion.Resources.Definitions.Id24;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

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

