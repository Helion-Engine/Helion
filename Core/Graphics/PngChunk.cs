using Helion.Geometry.Vectors;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Helion.Graphics;

public class PngChunk(string name, byte[] data, int crc)
{
    public readonly string Name = name;
    public readonly byte[] Data = data;
    public readonly int Crc = crc;

    public static List<PngChunk> ReadAll(BinaryReader reader)
    {
        var chunks = new List<PngChunk>();

        try
        {
            reader.BaseStream.Seek(8, SeekOrigin.Begin);
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                var length = BinaryPrimitives.ReverseEndianness(reader.ReadInt32());
                var name = new string(reader.ReadChars(4));
                var data = reader.ReadBytes(length);
                int crc = BinaryPrimitives.ReverseEndianness(reader.ReadInt32());
                var chunk = new PngChunk(name, data, crc);
                if (chunk.Name == "IDAT")
                    break;

                chunks.Add(chunk);
            }
        }
        catch
        {
            return chunks;
        }

        return chunks;
    }

    public static bool Read(BinaryReader reader, string name, [NotNullWhen(true)] out PngChunk? chunk)
    {
        chunk = null;
        try
        {
            reader.BaseStream.Seek(8, SeekOrigin.Begin);
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                var length = BinaryPrimitives.ReverseEndianness(reader.ReadInt32());
                var readName = new string(reader.ReadChars(4));
                var data = reader.ReadBytes(length);
                int crc = BinaryPrimitives.ReverseEndianness(reader.ReadInt32());

                if (readName == name)
                {
                    chunk = new PngChunk(name, data, crc);
                    return true;
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    public static Vec2I GetPngOffset(BinaryReader reader)
    {
        if (!Read(reader, "grAb", out var grAb))
            return default;

        var x = BinaryPrimitives.ReverseEndianness(BitConverter.ToInt32(grAb.Data.AsSpan(0, 4)));
        var y = BinaryPrimitives.ReverseEndianness(BitConverter.ToInt32(grAb.Data.AsSpan(4, 4)));

        return new(x, y);
    }
}
