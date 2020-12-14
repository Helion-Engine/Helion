﻿using Helion.Resources;
using Helion.Util.Bytes;
using Helion.Util.Extensions;
using Helion.Util.Geometry.Vectors;

namespace Helion.Graphics.Palette
{
    /// <summary>
    /// A collection of palette image reader helper methods.
    /// </summary>
    public static class PaletteReaders
    {
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
            if (data.Length < 16)
                return false;

            ByteReader reader = new(data);

            int width = reader.Short();
            int height = reader.Short();
            int offsetX = reader.Short();
            int offsetY = reader.Short();

            if (InvalidColumnImageDimensions(data, width, height, offsetX, offsetY))
                return false;

            return LastColumnValid(reader, width);
        }

        /// <summary>
        /// Reads the flat palette image from the data provided.
        /// </summary>
        /// <param name="data">The data for the flat palette image.</param>
        /// <param name="resourceNamespace">The resource namespace for this
        /// palette image to be created.</param>
        /// <returns>A palette image, or an empty optional if the data is not a
        /// flat palette image.</returns>
        public static PaletteImage? ReadFlat(byte[] data, ResourceNamespace resourceNamespace)
        {
            int dimension = FlatDimension(data.Length);
            if (dimension == 0)
                return null;

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

            ImageMetadata metadata = new ImageMetadata(resourceNamespace);
            return new PaletteImage(dimension, dimension, indices, metadata);
        }

        /// <summary>
        /// Reads the column palette image from the data provided.
        /// </summary>
        /// <param name="data">The data for the column palette image.</param>
        /// <param name="resourceNamespace">The resource namespace for this
        /// palette image to be created.</param>
        /// <returns>A palette image, or an empty optional if the data is not a
        /// column palette image.</returns>
        public static PaletteImage? ReadColumn(byte[] data, ResourceNamespace resourceNamespace)
        {
            // TODO: This could be improved probably dramatically if we:
            //       1) Read it into a column-major image and then rotated
            //       2) Use native/unsafe code
            try
            {
                ByteReader reader = new ByteReader(data);

                int width = reader.Short();
                int height = reader.Short();
                Vec2I imageOffsets = new Vec2I(width - (reader.Short() * 2), reader.Short() - height);

                int[] offsets = new int[width];
                for (int i = 0; i < width; i++)
                    offsets[i] = reader.Int();

                ushort[] indices = new ushort[width * height];
                indices.Fill(PaletteImage.TransparentIndex);

                for (int col = 0; col < width; col++)
                {
                    reader.Position = offsets[col];

                    while (true)
                    {
                        int rowStart = reader.Byte();
                        if (rowStart == 0xFF)
                            break;

                        int indicesCount = reader.Byte();
                        reader.Advance(1); // Skip dummy.
                        byte[] paletteIndices = reader.Bytes(indicesCount);
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
                return null;
            }
        }

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
        /// Checks if the data is larger than what is possible. This helps us
        /// quickly determine if it's corrupt data.
        /// </summary>
        /// <param name="data">The data to use for its length.</param>
        /// <param name="width">The image width.</param>
        /// <param name="height">The image height.</param>
        /// <returns>True if the data is larger than it could be, which means
        /// it is likely corrupt.</returns>
        private static bool LargerThanMaxColumnDataSize(byte[] data, int width, int height)
        {
            // This is an upper bound on the worst case for a column. Suppose
            // a column has a constant pixel/no-pixel alternating sequence.

            // That means we will get h/2 'posts' (or h/2 + 1 if odd, so we'll
            // go with that since it covers all cases).
            int maxPosts = (height / 2) + 1;

            // Each post is made up of a 'header' + 'length' + 2 dummy bytes +
            // the length of bytes. Since each length would be 1 'index pixel',
            // then the largest size it can be is 5 bytes. This means we have
            // 5 * max posts. We add 1 to the end because the last byte has to
            // be the 0xFF magic number to end the column.
            int maxBytesPerColumn = (5 * maxPosts) + 1;

            int headerSize = 8 - (width * 4);
            return data.Length - headerSize > width * maxBytesPerColumn;
        }

        private static bool InvalidColumnImageDimensions(byte[] data, int width, int height, int offsetX, int offsetY)
        {
            return width <= 0 || width >= 4096 ||
                   height <= 0 || height >= 4096 ||
                   offsetX < -2048 || offsetX > 2048 ||
                   offsetY < -2048 || offsetY > 2048 ||
                   LargerThanMaxColumnDataSize(data, width, height);
        }

        private static bool LastColumnValid(ByteReader reader, int width)
        {
            if (!reader.HasBytesRemaining(width * 4))
                return false;

            reader.Advance((width - 1) * 4);

            int offset = reader.Int();
            if (offset < 0 || offset >= reader.Length)
                return false;
            reader.Position = offset;

            // Note: We could actually evaluate the column here as well,
            // however this seems to be doing the job thus far. Nothing
            // wrong with more checks though!
            return reader.HasBytesRemaining(1);
        }
    }
}
