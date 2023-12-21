using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using System.Collections.Generic;

namespace Helion.Layer.Options;

internal class BoxList
{
    private readonly List<(Box2I, int)> m_posToRowIndex = new();

    public void Clear() =>
        m_posToRowIndex.Clear();

    public void Add(Box2I dimension, int index) =>
        m_posToRowIndex.Add((dimension, index));

    public bool GetIndex(Vec2I pos, out int index)
    {
        for (int i = 0; i < m_posToRowIndex.Count; i++)
        {
            if (!m_posToRowIndex[i].Item1.Contains(pos))
                continue;
            index = m_posToRowIndex[i].Item2;
            return true;
        }

        index = -1;
        return false;
    }
}
