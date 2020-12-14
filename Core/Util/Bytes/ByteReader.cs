using System;
using System.IO;
using System.Text;
using static Helion.Util.Assertion.Assert;

namespace Helion.Util.Bytes
{
    public class ByteReader
    {
        /// <summary>
        /// How many bytes are in the underlying wrapped byte buffer/stream.
        /// </summary>
        public readonly int Length;

        /// <summary>
        /// The position in the stream. Can either get the current offset, or
        /// set it to a new value relative to the beginning.
        /// </summary>
        /// <remarks>
        /// See https://stackoverflow.com/questions/19134172/is-it-safe-to-use-stream-seek-when-a-binaryreader-is-open
        /// for more information on moving.
        /// </remarks>
        public int Position
        {
            get => (int)m_reader.BaseStream.Position;
            set => m_reader.BaseStream.Seek(value, SeekOrigin.Begin);
        }

        private readonly BinaryReader m_reader;

        /// <summary>
        /// Creates a reader that wraps around the data provided.
        /// </summary>
        /// <param name="data">The data.</param>
        public ByteReader(byte[] data)
        {
            Precondition(BitConverter.IsLittleEndian, "We only support little endian systems");

            Length = data.Length;
            m_reader = new BinaryReader(new MemoryStream(data));
        }

        /// <summary>
        /// Creates a reader that wraps around the data provided.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        public ByteReader(BinaryReader reader)
        {
            Precondition(BitConverter.IsLittleEndian, "We only support little endian systems");

            Length = 0;
            m_reader = reader;
        }

        /// <summary>
        /// Gets this primitive value.
        /// </summary>
        /// <returns>The value.</returns>
        public byte Byte()
        {
            return m_reader.ReadByte();
        }

        /// <summary>
        /// Reads the primitive at the offset provided. Does not mutate the
        /// current offset.
        /// </summary>
        /// <param name="offset">The offset to read at.</param>
        /// <returns>The value at the offset provided.</returns>
        public byte Byte(int offset)
        {
            var currentPosition = Position;
            Position = offset;
            var value = Byte();
            Position = currentPosition;
            return value;
        }

        /// <summary>
        /// Reads `length` values at the current position, and advances the
        /// stream pointer.
        /// </summary>
        /// <param name="length">The number of items to read.</param>
        /// <returns>The values.</returns>
        public byte[] Bytes(int length)
        {
            return m_reader.ReadBytes(length);
        }

        /// <summary>
        /// Gets `length` values at the offset, does not advance the stream
        /// pointer.
        /// </summary>
        /// <param name="length">The number of items to get.</param>
        /// <param name="offset">The offset to start reading at.</param>
        /// <returns>The items.</returns>
        public byte[] Bytes(int length, int offset)
        {
            var currentPosition = Position;
            Position = offset;
            var value = Bytes(length);
            Position = currentPosition;
            return value;
        }

        /// <summary>
        /// Gets this primitive value.
        /// </summary>
        /// <returns>The value.</returns>
        public short Short()
        {
            return m_reader.ReadInt16();
        }

        /// <summary>
        /// Reads the primitive at the offset provided. Does not mutate the
        /// current offset.
        /// </summary>
        /// <param name="offset">The offset to read at.</param>
        /// <returns>The value at the offset provided.</returns>
        public short Short(int offset)
        {
            var currentPosition = Position;
            Position = offset;
            var value = Short();
            Position = currentPosition;
            return value;
        }

        /// <summary>
        /// Reads `amount` values at the current position, and advances the
        /// stream pointer.
        /// </summary>
        /// <param name="amount">The number of items to read.</param>
        /// <returns>The values.</returns>
        public short[] Shorts(int amount)
        {
            var values = new short[amount];
            for (int i = 0; i < amount; i++)
                values[i] = Short();
            return values;
        }

        /// <summary>
        /// Gets `amount` values at the offset, does not advance the stream
        /// pointer.
        /// </summary>
        /// <param name="amount">The number of items to get.</param>
        /// <param name="offset">The offset to start reading at.</param>
        /// <returns>The items.</returns>
        public short[] Shorts(int amount, int offset)
        {
            var currentPosition = Position;
            Position = offset;
            var value = Shorts(amount);
            Position = currentPosition;
            return value;
        }

        /// <summary>
        /// Gets this primitive value.
        /// </summary>
        /// <returns>The value.</returns>
        public ushort UShort()
        {
            return m_reader.ReadUInt16();
        }

        /// <summary>
        /// Reads the primitive at the offset provided. Does not mutate the
        /// current offset.
        /// </summary>
        /// <param name="offset">The offset to read at.</param>
        /// <returns>The value at the offset provided.</returns>
        public ushort UShort(int offset)
        {
            var currentPosition = Position;
            Position = offset;
            var value = UShort();
            Position = currentPosition;
            return value;
        }

        /// <summary>
        /// Reads `amount` values at the current position, and advances the
        /// stream pointer.
        /// </summary>
        /// <param name="amount">The number of items to read.</param>
        /// <returns>The values.</returns>
        public ushort[] UShorts(int amount)
        {
            var values = new ushort[amount];
            for (int i = 0; i < amount; i++)
                values[i] = UShort();
            return values;
        }

        /// <summary>
        /// Gets `amount` values at the offset, does not advance the stream
        /// pointer.
        /// </summary>
        /// <param name="amount">The number of items to get.</param>
        /// <param name="offset">The offset to start reading at.</param>
        /// <returns>The items.</returns>
        public ushort[] UShorts(int amount, int offset)
        {
            var currentPosition = Position;
            Position = offset;
            var value = UShorts(amount);
            Position = currentPosition;
            return value;
        }

        /// <summary>
        /// Gets this primitive value.
        /// </summary>
        /// <returns>The value.</returns>
        public int Int()
        {
            return m_reader.ReadInt32();
        }

        /// <summary>
        /// Reads the primitive at the offset provided. Does not mutate the
        /// current offset.
        /// </summary>
        /// <param name="offset">The offset to read at.</param>
        /// <returns>The value at the offset provided.</returns>
        public int Int(int offset)
        {
            var currentPosition = Position;
            Position = offset;
            var value = Int();
            Position = currentPosition;
            return value;
        }

        /// <summary>
        /// Reads `amount` values at the current position, and advances the
        /// stream pointer.
        /// </summary>
        /// <param name="amount">The number of items to read.</param>
        /// <returns>The values.</returns>
        public int[] Ints(int amount)
        {
            var values = new int[amount];
            for (int i = 0; i < amount; i++)
                values[i] = Int();
            return values;
        }

        /// <summary>
        /// Gets `amount` values at the offset, does not advance the stream
        /// pointer.
        /// </summary>
        /// <param name="amount">The number of items to get.</param>
        /// <param name="offset">The offset to start reading at.</param>
        /// <returns>The items.</returns>
        public int[] Ints(int amount, int offset)
        {
            var currentPosition = Position;
            Position = offset;
            var value = Ints(amount);
            Position = currentPosition;
            return value;
        }

        /// <summary>
        /// Gets this primitive value.
        /// </summary>
        /// <returns>The value.</returns>
        public uint UInt()
        {
            return m_reader.ReadUInt32();
        }

        /// <summary>
        /// Reads the primitive at the offset provided. Does not mutate the
        /// current offset.
        /// </summary>
        /// <param name="offset">The offset to read at.</param>
        /// <returns>The value at the offset provided.</returns>
        public uint UInt(int offset)
        {
            var currentPosition = Position;
            Position = offset;
            var value = UInt();
            Position = currentPosition;
            return value;
        }

        /// <summary>
        /// Reads `amount` values at the current position, and advances the
        /// stream pointer.
        /// </summary>
        /// <param name="amount">The number of items to read.</param>
        /// <returns>The values.</returns>
        public uint[] UInts(int amount)
        {
            var values = new uint[amount];
            for (int i = 0; i < amount; i++)
                values[i] = UInt();
            return values;
        }

        /// <summary>
        /// Gets `amount` values at the offset, does not advance the stream
        /// pointer.
        /// </summary>
        /// <param name="amount">The number of items to get.</param>
        /// <param name="offset">The offset to start reading at.</param>
        /// <returns>The items.</returns>
        public uint[] UInts(int amount, int offset)
        {
            var currentPosition = Position;
            Position = offset;
            var value = UInts(amount);
            Position = currentPosition;
            return value;
        }

        /// <summary>
        /// Gets this primitive value.
        /// </summary>
        /// <returns>The value.</returns>
        public long Long()
        {
            return m_reader.ReadInt64();
        }

        /// <summary>
        /// Reads the primitive at the offset provided. Does not mutate the
        /// current offset.
        /// </summary>
        /// <param name="offset">The offset to read at.</param>
        /// <returns>The value at the offset provided.</returns>
        public long Long(int offset)
        {
            var currentPosition = Position;
            Position = offset;
            var value = Long();
            Position = currentPosition;
            return value;
        }

        /// <summary>
        /// Reads `amount` values at the current position, and advances the
        /// stream pointer.
        /// </summary>
        /// <param name="amount">The number of items to read.</param>
        /// <returns>The values.</returns>
        public long[] Longs(int amount)
        {
            var values = new long[amount];
            for (int i = 0; i < amount; i++)
                values[i] = Long();
            return values;
        }

        /// <summary>
        /// Gets `amount` values at the offset, does not advance the stream
        /// pointer.
        /// </summary>
        /// <param name="amount">The number of items to get.</param>
        /// <param name="offset">The offset to start reading at.</param>
        /// <returns>The items.</returns>
        public long[] Longs(int amount, int offset)
        {
            var currentPosition = Position;
            Position = offset;
            var value = Longs(amount);
            Position = currentPosition;
            return value;
        }

        /// <summary>
        /// Gets this primitive value.
        /// </summary>
        /// <returns>The value.</returns>
        public ulong ULong()
        {
            return m_reader.ReadUInt64();
        }

        /// <summary>
        /// Reads the primitive at the offset provided. Does not mutate the
        /// current offset.
        /// </summary>
        /// <param name="offset">The offset to read at.</param>
        /// <returns>The value at the offset provided.</returns>
        public ulong ULong(int offset)
        {
            var currentPosition = Position;
            Position = offset;
            var value = ULong();
            Position = currentPosition;
            return value;
        }

        /// <summary>
        /// Reads `amount` values at the current position, and advances the
        /// stream pointer.
        /// </summary>
        /// <param name="amount">The number of items to read.</param>
        /// <returns>The values.</returns>
        public ulong[] ULongs(int amount)
        {
            var values = new ulong[amount];
            for (int i = 0; i < amount; i++)
                values[i] = ULong();
            return values;
        }

        /// <summary>
        /// Gets `amount` values at the offset, does not advance the stream
        /// pointer.
        /// </summary>
        /// <param name="amount">The number of items to get.</param>
        /// <param name="offset">The offset to start reading at.</param>
        /// <returns>The items.</returns>
        public ulong[] ULongs(int amount, int offset)
        {
            var currentPosition = Position;
            Position = offset;
            var value = ULongs(amount);
            Position = currentPosition;
            return value;
        }

        /// <summary>
        /// Gets this primitive value.
        /// </summary>
        /// <returns>The value.</returns>
        public float Float()
        {
            return m_reader.ReadSingle();
        }

        /// <summary>
        /// Reads the primitive at the offset provided. Does not mutate the
        /// current offset.
        /// </summary>
        /// <param name="offset">The offset to read at.</param>
        /// <returns>The value at the offset provided.</returns>
        public float Float(int offset)
        {
            var currentPosition = Position;
            Position = offset;
            var value = Float();
            Position = currentPosition;
            return value;
        }

        /// <summary>
        /// Reads `amount` values at the current position, and advances the
        /// stream pointer.
        /// </summary>
        /// <param name="amount">The number of items to read.</param>
        /// <returns>The values.</returns>
        public float[] Floats(int amount)
        {
            var values = new float[amount];
            for (int i = 0; i < amount; i++)
                values[i] = Float();
            return values;
        }

        /// <summary>
        /// Gets `amount` values at the offset, does not advance the stream
        /// pointer.
        /// </summary>
        /// <param name="amount">The number of items to get.</param>
        /// <param name="offset">The offset to start reading at.</param>
        /// <returns>The items.</returns>
        public float[] Floats(int amount, int offset)
        {
            var currentPosition = Position;
            Position = offset;
            var value = Floats(amount);
            Position = currentPosition;
            return value;
        }

        /// <summary>
        /// Gets this primitive value.
        /// </summary>
        /// <returns>The value.</returns>
        public double Double()
        {
            return m_reader.ReadDouble();
        }

        /// <summary>
        /// Reads the primitive at the offset provided. Does not mutate the
        /// current offset.
        /// </summary>
        /// <param name="offset">The offset to read at.</param>
        /// <returns>The value at the offset provided.</returns>
        public double Double(int offset)
        {
            var currentPosition = Position;
            Position = offset;
            var value = Double();
            Position = currentPosition;
            return value;
        }

        /// <summary>
        /// Reads `amount` values at the current position, and advances the
        /// stream pointer.
        /// </summary>
        /// <param name="amount">The number of items to read.</param>
        /// <returns>The values.</returns>
        public double[] Doubles(int amount)
        {
            var values = new double[amount];
            for (int i = 0; i < amount; i++)
                values[i] = Double();
            return values;
        }

        /// <summary>
        /// Gets `amount` values at the offset, does not advance the stream
        /// pointer.
        /// </summary>
        /// <param name="amount">The number of items to get.</param>
        /// <param name="offset">The offset to start reading at.</param>
        /// <returns>The items.</returns>
        public double[] Doubles(int amount, int offset)
        {
            var currentPosition = Position;
            Position = offset;
            var value = Doubles(amount);
            Position = currentPosition;
            return value;
        }

        /// <summary>
        /// Gets a string of some length.
        /// </summary>
        /// <returns>The string.</returns>
        public string String(int length)
        {
            return Encoding.UTF8.GetString(Bytes(length));
        }

        /// <summary>
        /// Reads the primitive at the offset provided. Does not mutate the
        /// current offset.
        /// </summary>
        /// <param name="length">The length of the string.</param>
        /// <param name="offset">The offset to read at.</param>
        /// <returns>The value at the offset provided.</returns>
        public string String(int length, int offset)
        {
            var currentPosition = Position;
            Position = offset;
            var value = String(length);
            Position = currentPosition;
            return value;
        }

        /// <summary>
        /// Reads `amount` values at the current position, and advances the
        /// stream pointer.
        /// </summary>
        /// <param name="length">The string length (for all strings).</param>
        /// <param name="amount">The number of items to read.</param>
        /// <returns>The values.</returns>
        public string[] Strings(int length, int amount)
        {
            var values = new string[amount];
            for (int i = 0; i < amount; i++)
                values[i] = String(length);
            return values;
        }

        /// <summary>
        /// Gets `amount` values at the offset, does not advance the stream
        /// pointer.
        /// </summary>
        /// <param name="length">The string length (for all strings).</param>
        /// <param name="amount">The number of items to get.</param>
        /// <param name="offset">The offset to start reading at.</param>
        /// <returns>The items.</returns>
        public string[] Strings(int length, int amount, int offset)
        {
            var currentPosition = Position;
            Position = offset;
            var value = Strings(length, amount);
            Position = currentPosition;
            return value;
        }

        /// <summary>
        /// Gets a string that is null terminated.
        /// </summary>
        /// <returns>The string.</returns>
        public string StringNullTerminated()
        {
            MemoryStream memoryStream = new();

            while (true)
            {
                byte b = m_reader.ReadByte();
                if (b == '\0')
                    return Encoding.UTF8.GetString(memoryStream.ToArray());
                memoryStream.WriteByte(b);
            }
        }

        /// <summary>
        /// Reads the strings at the offset provided. Does not mutate the
        /// current offset.
        /// </summary>
        /// <param name="offset">The offset to read at.</param>
        /// <returns>The value at the offset provided.</returns>
        public string StringNullTerminated(int offset)
        {
            var currentPosition = Position;
            Position = offset;
            var value = StringNullTerminated();
            Position = currentPosition;
            return value;
        }

        /// <summary>
        /// Reads `amount` strings at the current position, and advances the
        /// stream pointer.
        /// </summary>
        /// <param name="amount">The number of items to read.</param>
        /// <returns>The values.</returns>
        public string[] StringsNullTerminated(int amount)
        {
            var values = new string[amount];
            for (int i = 0; i < amount; i++)
                values[i] = StringNullTerminated();
            return values;
        }

        /// <summary>
        /// Gets `amount` strings at the offset, does not advance the stream
        /// pointer.
        /// </summary>
        /// <param name="amount">The number of items to get.</param>
        /// <param name="offset">The offset to start reading at.</param>
        /// <returns>The items.</returns>
        public string[] StringsNullTerminated(int amount, int offset)
        {
            var currentPosition = Position;
            Position = offset;
            var value = StringsNullTerminated(amount);
            Position = currentPosition;
            return value;
        }

        /// <summary>
        /// Reads the eight bytes of a string, unless it hits a null terminator
        /// to which it returns early. This is intended for raw lump strings
        /// that are always eight characters in length. The null terminators
        /// will not be part of the output.
        /// </summary>
        /// <remarks>
        /// This will throw an exception like any other reading.
        /// </remarks>
        /// <returns>The string (with no null terminators).</returns>
        public string EightByteString()
        {
            byte[] data = new byte[8];

            int index = 0;
            for (; index < 8; index++)
            {
                data[index] = Byte();

                if (data[index] == 0)
                {
                    // We need to always consume eight characters. Since we
                    // have not incremented the loop iteration yet, we are
                    // off by one and use 7 instead of 8.
                    Advance(7 - index);
                    break;
                }
            }

            return Encoding.UTF8.GetString(data[..index]);
        }

        /// <summary>
        /// Reads the strings at the offset provided. Does not mutate the
        /// current offset.
        /// </summary>
        /// <param name="offset">The offset to read at.</param>
        /// <returns>The value at the offset provided.</returns>
        public string EightByteString(int offset)
        {
            var currentPosition = Position;
            Position = offset;
            var value = EightByteString();
            Position = currentPosition;
            return value;
        }

        /// <summary>
        /// Reads `amount` strings at the current position, and advances the
        /// stream pointer.
        /// </summary>
        /// <param name="amount">The number of items to read.</param>
        /// <returns>The values.</returns>
        public string[] EightByteStrings(int amount)
        {
            var values = new string[amount];
            for (int i = 0; i < amount; i++)
                values[i] = EightByteString();
            return values;
        }

        /// <summary>
        /// Gets `strings` strings at the offset, does not advance the stream
        /// pointer.
        /// </summary>
        /// <param name="amount">The number of items to get.</param>
        /// <param name="offset">The offset to start reading at.</param>
        /// <returns>The items.</returns>
        public string[] EightByteStrings(int amount, int offset)
        {
            var currentPosition = Position;
            Position = offset;
            var value = EightByteStrings(amount);
            Position = currentPosition;
            return value;
        }

        /// <summary>
        /// Checks if there are `amount` bytes remaining to be read.
        /// </summary>
        /// <remarks>
        /// Not intended for negative offsets. Zero can return false if the
        /// stream position is past the end of the array.
        /// </remarks>
        /// <param name="amount">How many bytes to read.</param>
        /// <returns>True if there are at least the provided bytes left to be
        /// safely read, false if not.</returns>
        public bool HasBytesRemaining(int amount)
        {
            return m_reader.BaseStream.Position + amount <= Length;
        }

        /// <summary>
        /// Moves the stream ahead (or backward if negative) by the provided
        /// number of bytes.
        /// </summary>
        /// <param name="amount">The amount of bytes to move.</param>
        public void Advance(int amount)
        {
            m_reader.BaseStream.Seek(amount, SeekOrigin.Current);
        }
    }
}
