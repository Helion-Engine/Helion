using System.Collections.Generic;
using Helion.Util.Bytes;

namespace Helion.Resources.Definitions.Texture;

/// <summary>
/// The data that represents a Pnames entry.
/// </summary>
public class Pnames
{
    /// <summary>
    /// All the names that make up their respective indices.
    /// </summary>
    public readonly List<string> Names;

    private Pnames(List<string> names)
    {
        Names = names;
    }

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

        List<string> names = new List<string>();

        try
        {
            using ByteReader reader = new ByteReader(data);
            int count = reader.ReadInt32();
            int actual = (data.Length - 4) / 8;

            if (count > actual)
                return null;

            for (int i = 0; i < count; i++)
                names.Add(reader.ReadEightByteString());
        }
        catch
        {
            return null;
        }

        return new Pnames(names);
    }
}
