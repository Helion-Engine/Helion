using System;
using System.Collections;
using System.Collections.Generic;
using static Helion.Util.Assertion.Assert;

namespace Helion.Util.Container
{
    /// <summary>
    /// A dynamically resizing array.
    /// </summary>
    /// <remarks>
    /// This was made because we can't access the backing list of List, which
    /// means we have to copy the values every time we wanted to use it for any
    /// low level array pinning (or use reflection but that's not worth it).
    /// </remarks>
    /// <typeparam name="T">The type to contain.</typeparam>
    public class DynamicArray<T> : IEnumerable<T>
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

        /// <summary>
        /// Accesses the element at the provided index.
        /// </summary>
        /// <param name="index">The index to get/set.</param>
        /// <exception cref="IndexOutOfRangeException">If the index is out of
        /// range.</exception>
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

        /// <summary>
        /// Adds a new element to the array, and resizes if full. Amortized
        /// insertion time is O(1).
        /// </summary>
        /// <param name="element">The element to add.</param>
        public void Add(T element)
        {
            if (Length == Capacity)
                Resize(Capacity * 2);

            Data[Length++] = element;
        }

        /// <summary>
        /// Adds a series of elements efficiently to the dynamic array.
        /// </summary>
        /// <param name="elements">The elements to add.</param>
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

        /// <summary>
        /// Removes the last element, if any.
        /// </summary>
        public void RemoveLast()
        {
            if (Length > 0)
                Length--;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Length; i++)
                yield return Data[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        private void EnsureCapacity(int desiredCapacity)
        {
            Precondition(desiredCapacity > 0, "Trying to ensure a zero or negative capacity");
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

            Resize(newCapacity);
        }
        
        private void Resize(int newCapacity)
        {
            T[] newData = new T[newCapacity];
            Array.Copy(Data, newData, Data.Length);
            Data = newData;
        }
    }
}