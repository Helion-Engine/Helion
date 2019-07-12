using Helion.Util;
using System.Collections.Generic;

namespace Helion.Resources.Definitions.Texture
{
    /// <summary>
    /// The data that represents a Pnames entry.
    /// </summary>
    public class Pnames
    {
        /// <summary>
        /// All the names that make up their respective indices.
        /// </summary>
        public List<CiString> Names { get; }

        public Pnames() => Names = new List<CiString>();
        private Pnames(List<CiString> names) => Names = names;

        /// <summary>
        /// Tries to read the Pnames data into a Pnames object.
        /// </summary>
        /// <param name="data">The Pnames data.</param>
        /// <returns>The object if reading was successful, false otherwise.
        /// </returns>
        public static Pnames? From(byte[] data)
        {
            if ((data.Length - 4) % 8 != 0)
                return null;

            List<CiString> names = new List<CiString>();

            try
            {
                ByteReader reader = new ByteReader(data);
                int count = reader.ReadInt32();
                int actual = (data.Length - 4) / 8;

                if (count > actual)
                    return null;

                for (int i = 0; i < count; i++)
                    names.Add(reader.ReadEightByteString().ToUpper());
            }
            catch
            {
                return null;
            }

            return new Pnames(names);
        }
    }
}
