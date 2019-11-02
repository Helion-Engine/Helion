using System.Collections.Generic;

namespace Helion.Util.Container
{
    /// <summary>
    /// This is a workaround dictionary to provide covariance for the value
    /// type of a dictionary.
    /// </summary>
    /// <typeparam name="K">The key type.</typeparam>
    /// <typeparam name="V">The value type.</typeparam>
    public interface ICovariantReadOnlyDictionary<in K, out V> : IEnumerable<V> where V : class
    {
        bool Empty { get; }
        int Count { get; }
        V this[K key] { get; }
        bool ContainsKey(K key);
    }
}