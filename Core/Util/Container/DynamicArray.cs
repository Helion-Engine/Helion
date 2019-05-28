using System.Collections;
using System.Collections.Generic;

namespace Helion.Util.Container
{
    /// <summary>
    /// A dynamically resizing array.
    /// </summary>
    /// <remarks>
    /// This was made because we can't access the backing list of List<>, which
    /// means we have to copy the values every time we wanted to use it for any
    /// low level array pinning (or use reflection but that's not worth it).
    /// </remarks>
    /// <typeparam name="T">The type to contain.</typeparam>
    public class DynamicArray<T> : IEnumerable<T>
    {
        /// <summary>
        /// How many elements are in the array.
        /// </summary>
        public int Length { get; private set; } = 0;

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

        public DynamicArray(int capacity = 8) => Data = new T[capacity];

        public T this[int index] => Data[index];

        private void Resize()
        {
            T[] newData = new T[Capacity * 2];
            for (int i = 0; i < Length; i++)
                newData[i] = Data[i];

            Data = newData;
        }

        /// <summary>
        /// Clears the data. The underlying data becomes eligible for garbage
        /// collection, and a new array with the current capacity is created.
        /// </summary>
        public void Clear()
        {
            Data = new T[Capacity];
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
                Resize();

            Data[Length++] = element;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Length; i++)
                yield return Data[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
