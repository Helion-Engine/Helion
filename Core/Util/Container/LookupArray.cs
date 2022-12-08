using System.Diagnostics.CodeAnalysis;

namespace Helion.Util.Container;

public class LookupArray<T>
{
    private DynamicArray<T> m_items = new();

    public void Set(int key, T value)
    {
        if (key >= m_items.Data.Length)
            m_items.Resize(key * 2);

        m_items[key] = value;
    }

    public bool TryGetValue(int key, [NotNullWhen(true)] out T? value)
    {
        if (key >= m_items.Data.Length)
        {
            value = default(T);
            return false;
        }

        value = m_items.Data[key];
        return value != null;
    }

    public void SetAll(T value)
    {
        for (int i = 0; i < m_items.Data.Length; i++)
            m_items[i] = value;
    }
}
