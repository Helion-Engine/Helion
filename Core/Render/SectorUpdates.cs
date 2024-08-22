using Helion.Util.Container;
using Helion.World.Geometry.Sectors;

namespace Helion.Render;

internal class SectorUpdates
{
    public DynamicArray<Sector> UpdateSectors = new();

    private readonly DynamicArray<int> m_updateLookup = new();
    private int m_counter = 1;

    public void EnsureCapacity(int capacity)
    {
        if (m_updateLookup.Capacity < capacity)
            m_updateLookup.Resize(capacity);
    }

    public void ClearAndReset()
    {
        UpdateSectors.Clear();
        UpdateSectors.FlushReferences();

        for (int i = 0; i < m_updateLookup.Capacity; i++)
            m_updateLookup.Data[i] = 0;
    }

    public void Add(Sector sector)
    {
        if (m_updateLookup.Data[sector.Id] == m_counter)
            return;

        m_updateLookup.Data[sector.Id] = m_counter;
        UpdateSectors.Add(sector);
    }

    public void Clear()
    {
        UpdateSectors.Clear();
        m_counter++;
    }
}
