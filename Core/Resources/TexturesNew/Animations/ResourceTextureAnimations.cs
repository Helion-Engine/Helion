using System;
using System.Collections.Generic;
using NLog;

namespace Helion.Resources.TexturesNew.Animations;

public class ResourceTextureAnimations : IResourceTextureAnimations
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IResources m_resources;
    private readonly ResourceTextureManager m_textureManager;
    private readonly List<ResourceTexture> m_translation = new();
    private readonly List<ResourceAnimation> m_animations = new();
    
    public ResourceTextureAnimations(IResources resources, ResourceTextureManager textureManager)
    {
        m_resources = resources;
        m_textureManager = textureManager;
    }

    public ResourceTexture Lookup(ResourceTexture texture) => m_translation[texture.Index];
    public ResourceTexture Lookup(int textureIndex) => m_translation[textureIndex];

    internal void Add(ResourceTexture texture)
    {
        if (texture.Index < m_translation.Count)
        {
            Log.Error($"Adding the same texture to the animator twice: {texture.Name} ({texture.Namespace}, index {texture.Index})");
            return;
        }
        
        if (texture.Index > m_translation.Count)
            throw new Exception($"Allocated texture but did not track it ({texture.Name}: {texture.Index}, translation count: {m_translation.Count})");
        
        m_translation.Add(texture);
    }

    public void Tick()
    {
        // TODO
    }
}