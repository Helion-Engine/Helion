using Helion.Util.Container;
using SixLabors.Shapes;
using System;
using static OneOf.Types.TrueFalseOrNull;

namespace Helion.Strings;

public partial class SpanString
{
    private readonly DynamicArray<char> m_chars;

    public int Length => m_chars.Length;
    public int Capacity => m_chars.Capacity;

    public SpanString(int initialCapacity = 32)
    {
        m_chars = new(32);
    }

    public SpanString(string str)
    {
        m_chars = new(32);
        m_chars.EnsureCapacity(str.Length);
        Append(str);
    }

    public void Clear()
    {
        m_chars.Clear();
    }

    public unsafe void Append(string text)
    {
        if (text.Length == 0)
            return;

        int length = m_chars.Length;
        int copyLength = text.Length * sizeof(char);
        m_chars.EnsureCapacity(m_chars.Length + text.Length);
        fixed (char* to = m_chars.Data)
        {
            fixed (char* from = text)
            {
                Buffer.MemoryCopy(from, to + length,
                    (m_chars.Capacity * sizeof(char)) - (text.Length * sizeof(char)),
                    copyLength);
            }
        }

        m_chars.SetLength(length + text.Length);
    }

    public void Append(char c)
    {
        m_chars.Add(c);
    }

    public void Append(int number, int pad = 0, char padChar = '0')
    {
        if (number == 0)
        {
            pad--;
            while (pad > 0)
            {
                Append(padChar);
                pad--;
            }
            Append('0');
            return;
        }

        int length = m_chars.Length;
        int addCount = 0;
        int countValue = Math.Abs(number);
        while (countValue > 0)
        {
            addCount++;
            countValue /= 10;
        }

        if (number < 0)
        {
            Append('-');
            addCount++;
        }

        pad -= addCount;
        while (pad > 0)
        {
            Append(padChar);
            addCount++;
            pad--;
        }

        m_chars.EnsureCapacity(length + addCount);

        int index = 0;
        int value = Math.Abs(number);
        while (value > 0)
        {
            int digit = (value % 10);
            value /= 10;
            m_chars.Data[length + addCount - index - 1] = (char)(digit + '0');
            index++;
        }

        m_chars.SetLength(length + addCount);
    }

    public ReadOnlySpan<char> AsSpan()
    {
        return m_chars.Data.AsSpan(0, m_chars.Length);
    }

    public string ToString()
    {
        return new string(AsSpan());
    }
}
