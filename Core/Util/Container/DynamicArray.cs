using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using static Helion.Util.Assertion.Assert;

namespace Helion.Util.Container;

/// <summary>
/// A dynamically resizing array.
/// </summary>
/// <remarks>
/// This was made because we can't access the backing list of List, which
/// means we have to copy the values every time we wanted to use it for any
/// low level array pinning (or use reflection but that's not worth it).
/// </remarks>
/// <typeparam name="T">The type to contain.</typeparam>
public class DynamicArray<T> : IList<T>
{
    /// <summary>
    /// How many elements are in the array.
    /// </summary>
    public int Length;

    /// <summary>
    /// The exposed underlying array of data. This list may be longer than
    /// the number of elements contained, use <see cref="Length"/>.
    /// </summary>
    public T[] Data;

    /// <summary>
    /// How large the array is. This is not equal to how many elements are
    /// in the array.
    /// </summary>
    public int Capacity;

    public int Version;

    private readonly bool m_arrayPool;

    public int Count => Length;

    public bool IsReadOnly => true;

    public DynamicArray() : this(8, false)
    {

    }

    /// <summary>
    /// Creates a new dynamic array.
    /// </summary>
    /// <param name="capacity">How large the array should initially be. If
    /// no value is provided it defaults to 8. This value should not be
    /// negative or zero. It will be clamped to being at least a value of
    /// 1 to avoid certain resizing issues.</param>
    public DynamicArray(int capacity = 8, bool arrayPool = false, Func<T>? capacityAlloc = null)
    {
        Precondition(capacity > 0, "Must have a positive capacity");
        capacity = Math.Max(1, capacity);
        Capacity = capacity;
        m_arrayPool = arrayPool;
        if (arrayPool)
            Data = ArrayPool<T>.Shared.Rent(capacity);
        else
            Data = new T[capacity];

        if (capacityAlloc != null)
        {
            for (int i = 0; i < Capacity; i++)
                Data[i] = capacityAlloc();
            Length = Capacity;
        }
    }

    public DynamicArray(T[] data)
    {
        Capacity = data.Length;
        Data = data;
    }

    public T this[int index]
    {
        get => Data[index];
        set => Data[index] = value;
    }

    /// <summary>
    /// Clears the data.
    /// </summary>
    /// <remarks>
    /// For optimization reasons, this isn't cleared but rather has the
    /// count set to zero. This means any previous data is still held in
    /// the array. To fully clear it out (if this contains references) it
    /// should be prefixed with a loop that sets each field to null.
    /// </remarks>
    public void Clear()
    {
        Length = 0;
    }

    public void Add(T element)
    {
        if (Length == Capacity)
            SetCapacity(Capacity * 2);

        Data[Length++] = element;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddUnsafe(T element)
    {
        Data[Length++] = element;
    }

    public unsafe void AddMemoryCopy(Span<T> elements)
    {
        Debug.Assert(Unsafe.SizeOf<T>() == sizeof(T), "T must be an unmanaged type.");
        EnsureCapacity(Length + elements.Length);

        ref var srcRef = ref elements[0];
        ref var dstRef = ref Data[Length];

        var pSrc = Unsafe.AsPointer(ref srcRef);
        var pDst = Unsafe.AsPointer(ref dstRef);

        var byteCount = (ulong)(elements.Length * Unsafe.SizeOf<T>());

        Buffer.MemoryCopy(pSrc, pDst, byteCount, byteCount);

        Length += elements.Length;
    }

    public void Add(T[] elements)
    {
        EnsureCapacity(Length + elements.Length);

        if (elements.Length < 10)
        {
            for (int i = 0; i < elements.Length; i++)
                Data[Length + i] = elements[i];
        }
        else
        {
            Array.Copy(elements, 0, Data, Length, elements.Length);
        }

        Length += elements.Length;
    }

    public void Add(T[] elements, int length)
    {
        EnsureCapacity(Length + length);

        if (length < 10)
        {
            for (int i = 0; i < length; i++)
                Data[Length + i] = elements[i];
        }
        else
        {
            Array.Copy(elements, 0, Data, Length, length);
        }

        Length += length;
    }

    public void Add(Span<T> elements)
    {
        var length = elements.Length;
        EnsureCapacity(Length + length);

        if (length < 10)
        {
            for (int i = 0; i < length; i++)
                Data[Length + i] = elements[i];
        }
        else
        {
            elements.CopyTo(Data.AsSpan(Length));
        }

        Length += length;
    }

    public void AddRange(DynamicArray<T> elements)
    {
        EnsureCapacity(Length + elements.Length);

        if (elements.Length < 10)
        {
            for (int i = 0; i < elements.Length; i++)
                Data[Length + i] = elements[i];
        }
        else
        {
            Array.Copy(elements.Data, 0, Data, Length, elements.Length);
        }

        Length += elements.Length;
    }

    /// <summary>
    /// Resizes to fit the exact size given. Will copy the elements over and
    /// fill the remaining with default values. If smaller, will shrink the
    /// array and lose any values that are beyond the size.
    /// </summary>
    /// <param name="size">The new size to use. Should never be negative.
    /// </param>
    public void Resize(int size)
    {        
        SetCapacity(size);
        Length = size;
    }

    public T RemoveLast()
    {
        Precondition(Length > 0, "Count must be greater than zero");
        T data = Data[Length - 1];
        Length--;
        return data;
    }

    public void RemoveRange(int index, int count)
    {
        Precondition(count > 0, "Count must be great than zero");
        if (index != Length -1 || count > 1)
            Array.Copy(Data, index + count, Data, index, Length - index - count);
        Length -= count;
    }

    public void Sort()
    {
        Array.Sort(Data, 0, Length, null);
    }

    public void Sort(int index, int length)
    {
        Array.Sort(Data, index, length, null);
    }

    public void Sort(Comparison<T> comparison)
    {
        Span<T> span = Data.AsSpan(0, Length);
        MemoryExtensions.Sort(span, comparison);
    }

    public void EnsureCapacity(int desiredCapacity)
    {
        Precondition(Capacity > 0, "Should never have a zero capacity");

        if (desiredCapacity <= Capacity)
            return;

        // This is done this way to prevent the possibility of overflow. We
        // likely have more problems than this if we ever trigger this case
        // though.
        int newCapacity = Capacity;
        if (desiredCapacity >= int.MaxValue / 2)
            newCapacity = int.MaxValue;
        else
            while (newCapacity < desiredCapacity)
                newCapacity *= 2;

        SetCapacity(newCapacity);
    }

    public void EnsureCapacityExact(int desiredCapacity)
    {
        if (Capacity >= desiredCapacity)
            return;

        SetCapacity(desiredCapacity);
    }

    public void SetLength(int length)
    {
        Length = length;
    }

    private void SetCapacity(int newCapacity)
    {
        if (m_arrayPool)
        {
            T[] newData = ArrayPool<T>.Shared.Rent(newCapacity);
            Array.Copy(Data, newData, Data.Length);
            ArrayPool<T>.Shared.Return(Data, true);
            Data = newData;
        }
        else
        {
            T[] newData = new T[newCapacity];
            Array.Copy(Data, newData, Data.Length);
            Data = newData;
        }
        Capacity = newCapacity;
        Version++;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return Data.Take(Length).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return Data.Take(Length).GetEnumerator();
    }

    public int IndexOf(T item)
    {
        return Array.IndexOf(Data, item, 0, Length);
    }

    public void Insert(int index, T item)
    {
        EnsureCapacity(Length + 1);
        if (index < Length)
            Array.Copy(Data, index, Data, index + 1, Length - index);
        
        Data[index] = item;
        Length++;
    }

    public void RemoveAt(int index)
    {
        Length--;
        if (index < Length)
            Array.Copy(Data, index + 1, Data, index, Length - index);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        Array.Copy(Data, 0, array, arrayIndex, Length);
    }

    public bool Remove(T item)
    {
        var index = IndexOf(item);
        if (index < 0)
            return false;

        RemoveAt(index);
        return true;
    }

    public bool Contains(T item)
    {
        return Array.IndexOf(Data, item, 0, Length) != -1;
    }
}
