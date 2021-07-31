using System.Text;

namespace Helion.Resources.Definitions.Boom
{
    public static class BoomParseUtil
    {
        public const int NameLength = 8;

        public static string GetString(byte[] data, int index, int maxStringLength = int.MaxValue)
        {
            int countIndex = index;
            while (countIndex < data.Length && countIndex - index < maxStringLength && data[countIndex] != 0)
                countIndex++;
            return Encoding.ASCII.GetString(data, index, countIndex - index);
        }
    }
}
