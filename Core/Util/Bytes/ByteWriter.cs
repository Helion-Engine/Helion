using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Helion.Util.Bytes;

/// <summary>
/// A helper class for writing bytes.
/// </summary>
public class ByteWriter : IDisposable
{
    private readonly MemoryStream stream = new();
    private readonly BinaryWriter writer;

    public ByteWriter()
    {
        writer = new BinaryWriter(stream);
    }

    public void Dispose()
    {
        stream.Dispose();
        writer.Dispose();
    }

    /// <summary>
    /// Writes the values into the data stream.
    /// </summary>
    /// <param name="items">The items to write.</param>
    /// <returns>This instance for chaining.</returns>
    public ByteWriter Byte(params byte[] items)
    {
        for (int i = 0; i < items.Length; i++)
            writer.Write(items[i]);
        return this;
    }

    /// <summary>
    /// Writes the values into the data stream.
    /// </summary>
    /// <param name="span">The span to write.</param>
    /// <returns>This instance for chaining.</returns>
    public ByteWriter Bytes(Span<byte> span)
    {
        for (int i = 0; i < span.Length; i++)
            writer.Write(span[i]);
        return this;
    }

    /// <summary>
    /// Writes the values into the data stream.
    /// </summary>
    /// <param name="items">The items to write.</param>
    /// <returns>This instance for chaining.</returns>
    public ByteWriter ByteArrays(params byte[][] items)
    {
        for (int i = 0; i < items.Length; i++)
            Byte(items[i]);
        return this;
    }

    /// <summary>
    /// Writes the values into the data stream.
    /// </summary>
    /// <param name="items">The items to write.</param>
    /// <returns>This instance for chaining.</returns>
    public ByteWriter Short(params short[] items)
    {
        for (int i = 0; i < items.Length; i++)
            writer.Write(items[i]);
        return this;
    }

    /// <summary>
    /// Writes the values into the data stream.
    /// </summary>
    /// <param name="items">The items to write.</param>
    /// <returns>This instance for chaining.</returns>
    public ByteWriter UShort(params ushort[] items)
    {
        for (int i = 0; i < items.Length; i++)
            writer.Write(items[i]);
        return this;
    }

    /// <summary>
    /// Writes the values into the data stream.
    /// </summary>
    /// <param name="items">The items to write.</param>
    /// <returns>This instance for chaining.</returns>
    public ByteWriter Int(params int[] items)
    {
        for (int i = 0; i < items.Length; i++)
            writer.Write(items[i]);
        return this;
    }

    /// <summary>
    /// Writes the values into the data stream.
    /// </summary>
    /// <param name="items">The items to write.</param>
    /// <returns>This instance for chaining.</returns>
    public ByteWriter UInt(params uint[] items)
    {
        for (int i = 0; i < items.Length; i++)
            writer.Write(items[i]);
        return this;
    }

    /// <summary>
    /// Writes the values into the data stream.
    /// </summary>
    /// <param name="items">The items to write.</param>
    /// <returns>This instance for chaining.</returns>
    public ByteWriter Long(params long[] items)
    {
        for (int i = 0; i < items.Length; i++)
            writer.Write(items[i]);
        return this;
    }

    /// <summary>
    /// Writes the values into the data stream.
    /// </summary>
    /// <param name="items">The items to write.</param>
    /// <returns>This instance for chaining.</returns>
    public ByteWriter ULong(params ulong[] items)
    {
        for (int i = 0; i < items.Length; i++)
            writer.Write(items[i]);
        return this;
    }

    /// <summary>
    /// Writes the values into the data stream.
    /// </summary>
    /// <param name="items">The items to write.</param>
    /// <returns>This instance for chaining.</returns>
    public ByteWriter Float(params float[] items)
    {
        for (int i = 0; i < items.Length; i++)
            writer.Write(items[i]);
        return this;
    }

    /// <summary>
    /// Writes the values into the data stream.
    /// </summary>
    /// <param name="items">The items to write.</param>
    /// <returns>This instance for chaining.</returns>
    public ByteWriter Double(params double[] items)
    {
        for (int i = 0; i < items.Length; i++)
            writer.Write(items[i]);
        return this;
    }

    /// <summary>
    /// Writes the values into the data stream.
    /// </summary>
    /// <param name="items">The items to write.</param>
    /// <returns>This instance for chaining.</returns>
    public ByteWriter String(params string[] items)
    {
        for (int i = 0; i < items.Length; i++)
            writer.Write(Encoding.UTF8.GetBytes(items[i]));
        return this;
    }

    /// <summary>
    /// Writes the values into the data stream.
    /// </summary>
    /// <param name="items">The items to write.</param>
    /// <returns>This instance for chaining.</returns>
    public ByteWriter StringNullTerminated(params string[] items)
    {
        for (int i = 0; i < items.Length; i++)
        {
            writer.Write(Encoding.UTF8.GetBytes(items[i]));
            writer.Write((byte)0);
        }

        return this;
    }

    /// <summary>
    /// Writes the values into the data stream.
    /// </summary>
    /// <param name="items">The items to write.</param>
    /// <returns>This instance for chaining.</returns>
    public ByteWriter EightCharString(params string[] items)
    {
        for (int i = 0; i < items.Length; i++)
        {
            string item = items[i];
            int length = Math.Min(item.Length, 8);
            string str = item.Substring(0, length).PadRight(8, '\0');
            writer.Write(Encoding.UTF8.GetBytes(str));
        }

        return this;
    }

    /// <summary>
    /// Compiles the data into an array.
    /// </summary>
    /// <returns>The written data.</returns>
    public byte[] GetData() => stream.ToArray();
}
