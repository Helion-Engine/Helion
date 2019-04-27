using Helion.Resources;
using Helion.Util;
using Helion.Util.Geometry;
using System.Drawing;
using System.IO;

namespace Helion.Graphics
{
    /// <summary>
    /// A native image reading subsystem that leverages the standard library to
    /// read images.
    /// </summary>
    public class ImageReader
    {
        public static bool CanRead(byte[] data, UpperString extension)
        {
            // TODO: Check if PNG header by looking at the bytes in case the
            // extension is insufficient.
            // TODO: Add other types if this subsystem supports it.
            switch (extension.ToString())
            {
            case "PNG":
                return true;
            }

            return false;
        }

        public static Optional<Image> Read(byte[] data, ResourceNamespace resourceNamespace)
        {
            try
            {
                using (MemoryStream stream = new MemoryStream(data))
                {
                    // TODO: Read PNG offsets if header is PNG.
                    Vec2i offset = new Vec2i(0, 0);
                    ImageMetadata metadata = new ImageMetadata(offset, resourceNamespace);
                    return new Image(new Bitmap(stream, true), metadata);
                }
            }
            catch
            {
                return Optional.Empty;
            }
        }
    }
}
