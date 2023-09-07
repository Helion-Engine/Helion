using System.Collections.Generic;
using System.Linq;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Graphics.Fonts;
using Helion.Util.Extensions;

namespace Helion.RenderNew.Textures;

public class FontHandle
{
    public const int MaxSupportedCharIndex = 256;

    private readonly Font m_font;
    private readonly List<TextureHandle> m_charIndexToHandle = new();

    public string Name => m_font.Name;
    
    public FontHandle(Font font, Dictionary<char, TextureHandle> letterHandles)
    {
        if (letterHandles.Empty())
            throw new($"Do not support font {font.Name} with zero characters");
        
        // Get some default character.
        if (!letterHandles.TryGetValue(Font.DefaultChar, out TextureHandle? defaultHandle))
            defaultHandle = letterHandles.First().Value;
        
        for (int c = 0; c < MaxSupportedCharIndex; c++)
        {
            if (!letterHandles.TryGetValue((char)c, out TextureHandle? handle))
                handle = defaultHandle;
            m_charIndexToHandle[c] = handle;
        }
    }

    public TextureHandle this[char c] => m_charIndexToHandle[c < m_charIndexToHandle.Count ? c : Font.DefaultChar];

    public Box2I Measure(string text, Vec2I origin, int fontSize, float scale = 1.0f)
    {
        int width = 0;
        for (int i = 0; i < text.Length; i++)
            width += this[text[i]].Dimension.Width;
        
        Vec2I size = (new Vec2F(width, fontSize) * (scale, scale)).Int;
        return new(origin, origin + size);
    }
}