using Helion.Util.Container;
using System.Diagnostics.CodeAnalysis;

namespace Helion.Resources;

public class SpriteRotation(Texture texture, bool mirror)
{
    public Texture Texture = texture;
    public bool Mirror = mirror;
    public float FlipU = mirror ? 1 : 0;
    public object? RenderStore;
    private LookupArray<SpriteRotation>? m_translationRotations;

    public bool TryGetTranslationRotation(int index, [NotNullWhen(true)] out SpriteRotation? rotation)
    {
        if (m_translationRotations == null)
        {
            rotation = null;
            return false;
        }

        return m_translationRotations.TryGetValue(index, out rotation);
    }

    public void SetTranslationRotation(int index, SpriteRotation rotation)
    {
        m_translationRotations ??= new();
        m_translationRotations.Set(index, rotation);
    }
}
