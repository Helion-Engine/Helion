using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using System;
using System.Collections.Generic;

namespace Helion.Layer.Options;

internal class MenuPositionList
{
    private readonly List<(Box2I, int)> m_posToRowIndex = new();

    public void Clear() =>
        m_posToRowIndex.Clear();

    public void Add(Box2I dimension, int rowIndex) =>
        m_posToRowIndex.Add((dimension, rowIndex));

    public bool GetRowIndexForMouse(Vec2I mousePos, out int index)
    {
        for (int i = 0; i < m_posToRowIndex.Count; i++)
        {
            if (!m_posToRowIndex[i].Item1.Contains(mousePos))
                continue;
            index = m_posToRowIndex[i].Item2;
            return true;
        }

        index = -1;
        return false;
    }
}
