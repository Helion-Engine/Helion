using System;
using System.Collections;
using System.Collections.Generic;
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
public class DynamicArray<T>
{
    /// <summary>
    /// How many elements are in the array.
    /// </summary>
    public int Length { get; private set; }

    /// <summary>
    /// The exposed underlying array of data. This list may be longer than
    /// the number of elements contained, use <see cref="Length"/>.
    /// </summary>
    public T[] Data { get; private set; }

    /// <summary>
    /// How large the array is. This is not equal to how many elements are
    /// in the array.
    /// </summary>
    public int Capacity => Data.Length;

    public int Version { get; private set; }

    public bool Empty() => Length == 0;

    /// <summary>
    /// Creates a new dynamic array.
    /// </summary>
    /// <param name="capacity">How large the array should initially be. If
    /// no value is provided it defaults to 8. This value should not be
    /// negative or zero. It will be clamped to being at least a value of
    /// 1 to avoid certain resizing issues.</param>
    public DynamicArray(int capacity = 8)
    {
        Precondition(capacity > 0, "Must have a positive capacity");

        Data = new T[Math.Max(1, capacity)];
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

    public void Add(params T[] elements)
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

    public void AddRange(IList<T> elements)
    {
        EnsureCapacity(Length + elements.Count);

        for (int i = 0; i < elements.Count; i++)
            Data[Length + i] = elements[i];

        Length += elements.Count;
    }

    public void AddRange(DynamicArray<T> elements)
    {
        EnsureCapacity(Length + elements.Length);

        for (int i = 0; i < elements.Length; i++)
            Data[Length + i] = elements[i];

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
        if (Length == 0)
            throw new InvalidOperationException("No data to remove.");
        T data = Data[Length - 1];
        Length--;
        return data;
    }

    public void Sort()
    {
        Array.Sort<T>(Data, 0, Length, null);
    }

    public void Sort(int index, int length)
    {
        Array.Sort<T>(Data, index, length, null);
    }

    //public void Sort(IComparer<T> comparer)
    //{
    //    Span<T> span = Data.AsSpan(0, Length);
    //    MemoryExtensions.Sort(span, comparer);
    //}

    public void Sort(Comparison<T> comparison)
    {
        Span<T> span = Data.AsSpan(0, Length);
        MemoryExtensions.Sort(span, comparison);
    }

    private void EnsureCapacity(int desiredCapacity)
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

    private void SetCapacity(int newCapacity)
    {
        T[] newData = new T[newCapacity];
        Array.Copy(Data, newData, Data.Length);
        Data = newData;
        Version++;
    }
}
