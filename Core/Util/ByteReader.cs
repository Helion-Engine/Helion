using System;
using System.IO;
using System.Text;
using static Helion.Util.Assertion.Assert;

namespace Helion.Util
{
    /// <summary>
    /// A convenience class for reading from bytes. It also has support for
    /// reading big endian if the computer is in little endian format.
    /// </summary>
    public class ByteReader : BinaryReader
    {
        /// <summary>
        /// How many bytes are in the underlying wrapped byte buffer/stream.
        /// </summary>
        public readonly int Length;

        /// <summary>
        /// Initializes a new instance of the <see cref="ByteReader"/> class.
        /// </summary>
        /// <remarks>
        /// This allocates a new memory stream and does not dispose it, because
        /// the docs say memory streams do not need to be disposed.
        /// </remarks>
        /// <param name="bytes">The bytes to wrap the reader around and read
        /// from.</param>
        public ByteReader(byte[] bytes) :
            base(new MemoryStream(bytes))
        {
            Precondition(BitConverter.IsLittleEndian, "We only support little endian systems");
            Length = bytes.Length;
        }

        /// <summary>
        /// Creates a byte reader around an existing binary reader stream.
        /// </summary>
        /// <param name="stream">The stream to wrap around.</param>
        public ByteReader(BinaryReader stream) :
            base(stream.BaseStream)
        {
            Precondition(BitConverter.IsLittleEndian, "We only support little endian systems");
            Length = (int)stream.BaseStream.Length;
        }

        /// <summary>
        /// Reads the passed in stream of bytes as a string until a null
        /// terminator is reached. This function assumes that there is a
        /// null terminator in the bytes one is reading.
        /// </summary>
        /// <remarks>
        /// This leaves the ByteReader at the position of the null terminator.
        /// No null terminator is present in the returned string.
        /// </remarks>
        /// <returns>A string representation of the byte stream passed in.
        /// </returns>
        public string ReadNullTerminatedString()
        {
            StringBuilder stringBuilder = new();

            char c = (char)ReadByte();
            while (c != '\0')
            {
                stringBuilder.Append(c);
                c = (char)ReadByte();
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Reads the eight bytes of a string, unless it hits a null terminator
        /// to which it returns early. This is intended for raw lump strings
        /// that are always eight characters in length.
        /// </summary>
        /// <remarks>
        /// This will throw an exception like any other reading.
        /// </remarks>
        /// <returns>The string.</returns>
        public string ReadEightByteString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < 8; i++)
            {
                char c = (char)ReadByte();
                if (c == 0)
                {
                    // We need to always consume eight characters. Since we
                    // have not incremented the loop iteration yet, we are
                    // off by one and use 7 instead of 8.
                    Advance(7 - i);
                    break;
                }
                else
                    stringBuilder.Append(c);
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Reads in the provided characters as a string. This does not do any
        /// bounds checking and will throw if it reads out of bounds.
        /// </summary>
        /// <param name="length">How many characters to read.</param>
        /// <returns>The string that was read.</returns>
        public string ReadStringLength(int length)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < length; i++)
                builder.Append((char)ReadByte());

            return builder.ToString();
        }

        /// <summary>
        /// Reads a 32-bit big endian integer. Throws if any errors result from
        /// reading the data (such as not enough data).
        /// </summary>
        /// <returns>The desired big endian integer.</returns>
        public int ReadInt32BE()
        {
            byte[] data = ReadBytes(4);
            Array.Reverse(data);
            return BitConverter.ToInt32(data, 0);
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
            return BaseStream.Position + amount <= BaseStream.Length;
        }

        /// <summary>
        /// Moves the stream ahead (or backward if negative) by the provided
        /// number of bytes.
        /// </summary>
        /// <param name="amount">The amount of bytes to move.</param>
        public void Advance(int amount)
        {
            BaseStream.Seek(amount, SeekOrigin.Current);
        }

        /// <summary>
        /// Moves the internal pointer to the offset provided, relative to the
        /// beginning.
        /// </summary>
        /// <param name="offset">The offset to go to.</param>
        public void Offset(int offset)
        {
            // Remember that we can Seek() safely on a binary reader:
            // https://stackoverflow.com/questions/19134172/is-it-safe-to-use-stream-seek-when-a-binaryreader-is-open
            BaseStream.Seek(offset, SeekOrigin.Begin);
        }
    }
}