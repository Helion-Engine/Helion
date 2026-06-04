using System;

namespace Helion.Util.Container;

public sealed class SparseSet<T>(int initialCapacity) where T : class
{
    private readonly DynamicArray<int> m_sparse = new(initialCapacity);
    private readonly DynamicArray<int> m_dense = new(initialCapacity);
    private readonly DynamicArray<T?> m_items = new(initialCapacity);

    public int Count;

    public bool Contains(int id)
    {
        if (id >= m_sparse.Length)
            return false;

        int denseIndex = m_sparse[id];
        return denseIndex < Count && m_dense.Data[denseIndex] == id;
    }

    public T? Get(int id)
    {
        if (!Contains(id))
            return default;

        return m_items[m_sparse[id]];
    }

    public void Add(int id, T item)
    {
        m_sparse.EnsureCapacity(id + 1);

        m_dense.EnsureCapacity(Count + 1);
        m_items.EnsureCapacity(Count + 1);

        var denseIndex = Count++;

        m_sparse[id] = denseIndex;
        m_dense[denseIndex] = id;
        m_items[denseIndex] = item;
    }

    public void Remove(int id)
    {
        if (!Contains(id))
            return;

        int denseIndex = m_sparse[id];
        int lastIndex = Count - 1;

        int lastId = m_dense[lastIndex];

        m_dense[denseIndex] = lastId;
        m_items[denseIndex] = m_items[lastIndex];

        m_sparse[lastId] = denseIndex;

        m_items[lastIndex] = null;

        Count--;
    }

    public void Clear()
    {
        Array.Clear(m_items.Data, 0, Count);
        Count = 0;
    }
}
