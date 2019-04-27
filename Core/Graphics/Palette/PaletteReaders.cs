using Helion.Resources;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.Util.Geometry;

namespace Helion.Graphics.Palette
{
    /// <summary>
    /// A collection of palette image reader helper methods.
    /// </summary>
    public class PaletteReaders
    {
        /// <summary>
        /// Gets the flat dimension from the full length of the data provided.
        /// </summary>
        /// <param name="length">The length of the palette entry bytes.</param>
        /// <returns>The dimension, or zero if the length is bad.</returns>
        private static int FlatDimension(int length)
        {
            switch (length)
            {
            case 64 * 64:
                return 64;
            case 128 * 128:
                return 128;
            case 256 * 256:
                return 256;
            default:
                return 0;
            }
        }

        /// <summary>
        /// Checks if the data provided is likely a flat palette image.
        /// </summary>
        /// <param name="data">The data to check.</param>
        /// <returns>True if it's likely a flat, false otherwise.</returns>
        public static bool LikelyFlat(byte[] data)
        {
            switch (data.Length)
            {
            case 64 * 64:
            case 128 * 128:
            case 256 * 256:
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the data provided is likely a column palette image.
        /// </summary>
        /// <param name="data">The data to check.</param>
        /// <returns>True if it's likely a column, false otherwise.</returns>
        public static bool LikelyColumn(byte[] data)
        {
            try
            {
                ByteReader reader = new ByteReader(data);

                int width = reader.ReadInt16();

                // We want to read the very last offset. Our heuristic is that
                // this needs to do the minimal amount of work possible since
                // image classification for column palette images is slow, so
                // we're going to assume that if the last offset is valid and
                // the last column is valid, it's likely a valid palette image.
                // We also assume no column image has a zero width.
                reader.Advance(6 + (4 * (width - 1)));
                int lastOffset = reader.ReadInt32();
                reader.Offset(lastOffset);

                while (true)
                {
                    byte rowStart = reader.ReadByte();
                    if (rowStart == 0xFF)
                        break;

                    byte pixelCount = reader.ReadByte();
                    reader.Advance(2 + pixelCount);
                }

                // We should be somewhere near the end of the file now if it is
                // valid. We have to give a bit of buffer room since there are
                // some images that have a few bytes padded at the end though.
                return reader.BaseStream.Position >= data.Length - 4;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Reads the flat palette image from the data provided.
        /// </summary>
        /// <param name="data">The data for the flat palette image.</param>
        /// <param name="resourceNamespace">The resource namespace for this
        /// palette image to be created.</param>
        /// <returns>A palette image, or an empty optional if the data is not a
        /// flat palette image</returns>
        public static Optional<PaletteImage> ReadFlat(byte[] data, ResourceNamespace resourceNamespace)
        {
            int dimension = FlatDimension(data.Length);
            if (dimension == 0)
                return Optional.Empty;

            ushort[] indices = new ushort[dimension * dimension];

            int offset = 0;
            for (int y = 0; y < dimension; y++)
            {
                for (int x = 0; x < dimension; x++)
                {
                    indices[offset] = data[offset];
                    offset++;
                }
            }

            return new PaletteImage(dimension, dimension, indices, new ImageMetadata());
        }

        /// <summary>
        /// Reads the column palette image from the data provided.
        /// </summary>
        /// <param name="data">The data for the column palette image.</param>
        /// <param name="resourceNamespace">The resource namespace for this
        /// palette image to be created.</param>
        /// <returns>A palette image, or an empty optional if the data is not a
        /// column palette image</returns>
        public static Optional<PaletteImage> ReadColumn(byte[] data, ResourceNamespace resourceNamespace)
        {
            // TODO: This could be improved probably dramatically if we:
            //       1) Read it into a column-major image and then rotated
            //       2) Use natively/unsafe code 
            try
            {
                ByteReader reader = new ByteReader(data);

                int width = reader.ReadInt16();
                int height = reader.ReadInt16();
                Vec2i imageOffsets = new Vec2i(reader.ReadInt16(), reader.ReadInt16());

                int[] offsets = new int[width];
                for (int i = 0; i < width; i++)
                    offsets[i] = reader.ReadInt32();

                ushort[] indices = new ushort[width * height];
                indices.Fill(PaletteImage.TRANSPARENT_INDEX);

                for (int col = 0; col < width; col++)
                {
                    reader.Offset(offsets[col]);

                    while (true)
                    {
                        int rowStart = reader.ReadByte();
                        if (rowStart == 0xFF)
                            break;

                        int indicesCount = reader.ReadByte();
                        reader.Advance(1); // Skip dummy.
                        byte[] paletteIndices = reader.ReadBytes(indicesCount);
                        reader.Advance(1); // Skip dummy.

                        int indicesOffset = (rowStart * width) + col;
                        for (int i = 0; i < paletteIndices.Length; i++)
                        {
                            indices[indicesOffset] = paletteIndices[i];
                            indicesOffset += width;
                        }
                    }
                }

                ImageMetadata metadata = new ImageMetadata(imageOffsets, resourceNamespace);
                return new PaletteImage(width, height, indices, metadata);
            }
            catch
            {
                return Optional.Empty;
            }
        }
    }
}
