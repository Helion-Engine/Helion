using GlmSharp;
using Helion.Graphics;
using Helion.Util.Container;
using System.Collections.Generic;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Automap;

public class AutomapColorPoints
{
    private readonly Dictionary<Color, DynamicArray<vec2>> m_lookup = [];
    private readonly List<Color> m_colors = [];

    public void GetColors(List<Color> colors)
    {
        for (int i = 0; i < m_colors.Count; i++)
        {
            var color = m_colors[i];
            if (!m_lookup.TryGetValue(m_colors[i], out var points))
                continue;

            if (points.Length > 0)
                colors.Add(color);
        }
    }

    public void Clear()
    {
        for (int i = 0; i < m_colors.Count; i++)
        {
            if (!m_lookup.TryGetValue(m_colors[i], out var points))
                continue;
            points.Clear();
        }
    }

    public DynamicArray<vec2> GetPoints(Color color)
    {
        if (m_lookup.TryGetValue(color, out var points))
            return points;

        points = new();
        m_lookup[color] = points;
        m_colors.Add(color);
        return points;
    }
}
