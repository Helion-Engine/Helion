using Helion.Geometry.Vectors;
using Helion.Resources;
using Helion.Util.Bytes;
using Helion.Util.Extensions;
using System;

namespace Helion.Graphics.Palettes;

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
        return data.Length switch
        {
            64 * 64 => true,
            128 * 128 => true,
            256 * 256 => true,
            _ => false
        };
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

        using ByteReader reader = new ByteReader(data);

        int width = reader.ReadInt16();
        int height = reader.ReadInt16();
        int offsetX = reader.ReadInt16();
        int offsetY = reader.ReadInt16();

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
    public static Image? ReadFlat(byte[] data, ResourceNamespace resourceNamespace)
    {
        int dim = FlatDimension(data.Length);
        if (dim == 0)
            return null;

        uint[] indices = new uint[dim * dim];

        int offset = 0;
        for (int y = 0; y < dim; y++)
        {
            for (int x = 0; x < dim; x++)
            {
                indices[offset] = data[offset];
                offset++;
            }
        }

        return Image.FromPaletteIndices((dim, dim), indices, (0, 0), resourceNamespace);
    }

    /// <summary>
    /// Reads the column palette image from the data provided.
    /// </summary>
    /// <param name="data">The data for the column palette image.</param>
    /// <param name="resourceNamespace">The resource namespace for this
    /// palette image to be created.</param>
    /// <returns>A palette image, or an empty optional if the data is not a
    /// column palette image.</returns>
    public static Image? ReadColumn(byte[] data, ResourceNamespace resourceNamespace)
    {
        try
        {
            using ByteReader reader = new(data);

            int width = reader.ReadInt16();
            int height = reader.ReadInt16();
            Vec2I imageOffsets = (reader.ReadInt16(), reader.ReadInt16());

            int[] offsets = new int[width];
            for (int i = 0; i < width; i++)
                offsets[i] = reader.ReadInt32();

            uint[] indices = new uint[width * height];
            indices.Fill(Image.TransparentIndex);

            for (int col = 0; col < width; col++)
            {
                reader.Offset(offsets[col]);
                int offset = 0;

                while (true)
                {
                    int rowStart = reader.ReadByte();
                    if (rowStart == 0xFF)
                        break;

                    int indicesCount = reader.ReadByte();
                    reader.Advance(1); // Skip dummy.
                    int position = (int)reader.BaseStream.Position;
                    var paletteIndices = new Span<byte>(data, position, indicesCount);
                    reader.Advance(indicesCount + 1); // Skip dummy.

                    // Tall patch support, since we are writing up the column we expect rowStart to be greater than the last
                    // If it's smaller or equal then add to the offset to support images greater than 254 in height
                    if (rowStart <= offset)
                        offset += rowStart;
                    else
                        offset = rowStart;

                    int indicesOffset = (offset * width) + col;
                    for (int i = 0; i < paletteIndices.Length; i++)
                    {
                        if (indicesOffset >= indices.Length)
                            break;

                        indices[indicesOffset] = paletteIndices[i];
                        indicesOffset += width;
                    }
                }
            }

            return Image.FromPaletteIndices((width, height), indices, imageOffsets, resourceNamespace);
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
        return length switch
        {
            64 * 64 => 64,
            128 * 128 => 128,
            256 * 256 => 256,
            _ => 0
        };
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

        int offset = reader.ReadInt32();
        if (offset < 0 || offset >= reader.Length)
            return false;
        reader.Offset(offset);

        // Note: We could actually evaluate the column here as well,
        // however this seems to be doing the job thus far. Nothing
        // wrong with more checks though!
        return reader.HasBytesRemaining(1);
    }
}
