using Helion.Util;
using System.Collections.Generic;

namespace Helion.Resources.Definitions
{
    /// <summary>
    /// The data that represents a Pnames entry.
    /// </summary>
    public class Pnames
    {
        /// <summary>
        /// All the names that make up their respective indices.
        /// </summary>
        public List<UpperString> Names { get; }

        public Pnames() => Names = new List<UpperString>();
        private Pnames(List<UpperString> names) => Names = names;

        /// <summary>
        /// Tries to read the Pnames data into a Pnames object.
        /// </summary>
        /// <param name="data">The Pnames data.</param>
        /// <returns>The object if reading was successful, false otherwise.
        /// </returns>
        public Optional<Pnames> From(byte[] data)
        {
            if ((data.Length - 4) % 8 != 0)
                return Optional.Empty;

            List<UpperString> names = new List<UpperString>();

            try
            {
                ByteReader reader = new ByteReader(data);
                int count = reader.ReadInt32();
                int actual = (data.Length - 4) / 8;

                if (count > actual)
                    return Optional.Empty;

                for (int i = 0; i < count; i++)
                    names.Add(reader.ReadEightByteString());
            }
            catch
            {
                return Optional.Empty;
            }

            return new Pnames(names);
        }
    }
}
