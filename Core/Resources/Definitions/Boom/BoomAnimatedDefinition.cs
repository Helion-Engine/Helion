using Helion.Resources.Archives.Entries;
using System.Collections.Generic;
using System.Text;

namespace Helion.Resources.Definitions.Boom
{
    public class BoomAnimatedDefinition
    {
        private const int AnimLength = 23;
        public readonly IList<BoomAnimatedTexture> AnimatedTextures = new List<BoomAnimatedTexture>();

        public void Parse(Entry entry)
        {
            byte[] data = entry.ReadData();
            for (int i = 0; i < data.Length; i += AnimLength)
            {
                if (data[i] == 255 || data.Length - i < AnimLength)
                    break;

                AnimatedTextures.Add(new BoomAnimatedTexture()
                {
                    IsTexture = (data[i] & 1) != 0,
                    StartTexture = BoomParseUtil.GetString(data, i + 10, BoomParseUtil.NameLength),
                    EndTexture = BoomParseUtil.GetString(data, i + 1, BoomParseUtil.NameLength),
                    Tics = data[i + 19] | data[i + 20] << 8 | data[i + 21] << 16 | data[i + 22] << 24
                });
            }
        }
    }
}
