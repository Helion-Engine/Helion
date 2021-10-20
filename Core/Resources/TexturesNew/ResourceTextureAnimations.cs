using Helion.Util;

namespace Helion.Resources.TexturesNew;

public class ResourceTextureAnimations : ITickable
{
    private readonly IResources m_resources;
    private readonly ResourceTextureManager m_textureManager;
    
    public ResourceTextureAnimations(IResources resources, ResourceTextureManager textureManager)
    {
        m_resources = resources;
        m_textureManager = textureManager;
    }

    public void Tick()
    {
        // TODO
    }
}