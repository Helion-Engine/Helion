namespace Helion.Resources;

public struct AnimationEvent
{
    public int TextureTranslationHandle;
    public int TextureHandleTo;

    public AnimationEvent(int textureTranslationHandle, int textureHandleTo)
    {
        TextureTranslationHandle = textureTranslationHandle;
        TextureHandleTo = textureHandleTo;
    }
}