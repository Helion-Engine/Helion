using System.Drawing;
using System.IO;
using Helion.Resources;
using Helion.Util.Geometry;

namespace Helion.Graphics
{
    /// <summary>
    /// A native image reading subsystem that leverages the standard library to
    /// read images.
    /// </summary>
    public class ImageReader
    {
        public static bool CanRead(byte[] data)
        {
            return IsPng(data) || IsJpg(data) || IsBmp(data);
        }

        public static bool IsPng(byte[] data)
        {
            return data.Length > 8 && data[0] == 137 && data[1] == 'P' && data[2] == 'N' && data[3] == 'G';
        }

        public static bool IsJpg(byte[] data)
        {
            return data.Length > 10 && data[0] == 0xFF && data[1] == 0xD8;
        }

        public static bool IsBmp(byte[] data)
        {
            return data.Length > 14 && data[0] == 'B' && data[1] == 'M';
        }

        public static Image? Read(byte[] data)
        {
            try
            {
                using (MemoryStream stream = new MemoryStream(data))
                {
                    // TODO: Read PNG offsets if header is PNG.
                    Vec2I offset = new Vec2I(0, 0);
                    ImageMetadata metadata = new ImageMetadata(offset, ResourceNamespace.Global);
                    return new Image(new Bitmap(stream, true), metadata);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}