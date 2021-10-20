namespace Helion.Resources.TexturesNew.Animations;

public class ResourceTextureAnimations : IResourceTextureAnimations
{
    private readonly IResources m_resources;
    private readonly ResourceTextureManager m_textureManager;
    
    public ResourceTextureAnimations(IResources resources, ResourceTextureManager textureManager)
    {
        m_resources = resources;
        m_textureManager = textureManager;
    }

    internal void Add(ResourceTexture texture)
    {
        // TODO
    }

    public void Tick()
    {
        // TODO
    }
}