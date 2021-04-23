using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentAssertions;
using Helion.Util;
using Helion.Util.Bytes;
using Xunit;

namespace Helion.Tests.Unit.Util.Bytes
{
    public class ByteWriterTest
    {
        private static byte[] IntArrayToByte(int[] array) => array.Select(i => (byte) i).ToArray();
        
        [Fact(DisplayName = "An empty writer returns an empty array")]
        public void EmptyWriterCreatesEmptyArray()
        {
            ByteWriter writer = new();

            writer.GetData().Should().BeEmpty();
        }
        
        [Theory(DisplayName = "Writes byte by byte")]
        [InlineData]
        [InlineData(1)]
        [InlineData(3, 4, 5, 8)]
        public void WriteSingleByte(params int[] data)
        {
            byte[] bytes = IntArrayToByte(data);
            ByteWriter writer = new();
            
            writer.Byte(bytes);
            
            writer.GetData().Should().Equal(bytes);
        }
        
        [Theory(DisplayName = "Writes byte span")]
        [InlineData]
        [InlineData(1)]
        [InlineData(3, 4, 5, 8)]
        public void WriteSpanByte(params int[] data)
        {
            byte[] bytes = data.Select(i => (byte)i).ToArray();
            Span<byte> span = bytes;
            ByteWriter writer = new();
            
            writer.Bytes(span);
            
            writer.GetData().Should().Equal(bytes);
        }
        
        [Theory(DisplayName = "Writes byte arrays")]
        [InlineData]
        [InlineData(new[] { 1 } )]
        [InlineData(new[] { 1 }, new[] { 2, 3 }, new[] { 4, 5, 8 })]
        public void WriteByteArray(params int[][] data)
        {
            byte[][] byteArray = data.Select(IntArrayToByte).ToArray();
            ByteWriter writer = new();
            
            writer.ByteArrays(byteArray);
            
            IEnumerable<byte> flattened = byteArray.SelectMany(e => e);
            writer.GetData().Should().Equal(flattened);
        }
        
        [Theory(DisplayName = "Writes shorts")]
        [InlineData]
        [InlineData(1)]
        [InlineData(3, 4, 5, 8)]
        public void WriteShorts(params int[] data)
        {
            short[] shorts = data.Select(i => (short)i).ToArray();
            ByteWriter writer = new();
            
            writer.Short(shorts);

            byte[] expected = new byte[shorts.Length * sizeof(short)];
            Buffer.BlockCopy(shorts, 0, expected, 0, expected.Length);
            writer.GetData().Should().Equal(expected);
        }
        
        [Theory(DisplayName = "Writes unsigned shorts")]
        [InlineData]
        [InlineData(1)]
        [InlineData(3, 4, 5, 8)]
        public void WriteUShorts(params int[] data)
        {
            ushort[] ushorts = data.Select(i => (ushort)i).ToArray();
            ByteWriter writer = new();
            
            writer.UShort(ushorts);

            byte[] expected = new byte[ushorts.Length * sizeof(ushort)];
            Buffer.BlockCopy(ushorts, 0, expected, 0, expected.Length);
            writer.GetData().Should().Equal(expected);
        }
        
        [Theory(DisplayName = "Writes ints")]
        [InlineData]
        [InlineData(1)]
        [InlineData(3, 4, 5, 8)]
        public void WriteInts(params int[] data)
        {
            ByteWriter writer = new();
            
            writer.Int(data);

            byte[] expected = new byte[data.Length * sizeof(int)];
            Buffer.BlockCopy(data, 0, expected, 0, expected.Length);
            writer.GetData().Should().Equal(expected);
        }
        
        [Theory(DisplayName = "Writes unsigned ints")]
        [InlineData]
        [InlineData(1)]
        [InlineData(3, 4, 5, 8)]
        public void WriteUInts(params int[] data)
        {
            uint[] uints = data.Select(i => (uint)i).ToArray();
            ByteWriter writer = new();
            
            writer.UInt(uints);

            byte[] expected = new byte[uints.Length * sizeof(uint)];
            Buffer.BlockCopy(uints, 0, expected, 0, expected.Length);
            writer.GetData().Should().Equal(expected);
        }
        
        [Theory(DisplayName = "Writes longs")]
        [InlineData]
        [InlineData(1)]
        [InlineData(3, 4, 5, 8)]
        public void WriteLongs(params int[] data)
        {
            long[] longs = data.Select(i => (long)i).ToArray();
            ByteWriter writer = new();
            
            writer.Long(longs);

            byte[] expected = new byte[longs.Length * sizeof(long)];
            Buffer.BlockCopy(longs, 0, expected, 0, expected.Length);
            writer.GetData().Should().Equal(expected);
        }
        
        [Theory(DisplayName = "Writes unsigned longs")]
        [InlineData]
        [InlineData(1)]
        [InlineData(3, 4, 5, 8)]
        public void WriteULongs(params int[] data)
        {
            ulong[] ulongs = data.Select(i => (ulong)i).ToArray();
            ByteWriter writer = new();
            
            writer.ULong(ulongs);

            byte[] expected = new byte[ulongs.Length * sizeof(ulong)];
            Buffer.BlockCopy(ulongs, 0, expected, 0, expected.Length);
            writer.GetData().Should().Equal(expected);
        }
        
        [Theory(DisplayName = "Writes floats")]
        [InlineData]
        [InlineData(1.0f)]
        [InlineData(3.1f, 4.2f, 5.3f, 8.4f)]
        public void WriteFloats(params float[] data)
        {
            ByteWriter writer = new();
            
            writer.Float(data);

            byte[] expected = new byte[data.Length * sizeof(float)];
            Buffer.BlockCopy(data, 0, expected, 0, expected.Length);
            writer.GetData().Should().Equal(expected);
        }
        
        [Theory(DisplayName = "Writes doubles")]
        [InlineData]
        [InlineData(1.0)]
        [InlineData(3.1, 4.2, 5.3, 8.4)]
        public void WriteDoubles(params double[] data)
        {
            ByteWriter writer = new();
            
            writer.Double(data);

            byte[] expected = new byte[data.Length * sizeof(double)];
            Buffer.BlockCopy(data, 0, expected, 0, expected.Length);
            writer.GetData().Should().Equal(expected);
        }
        
        [Theory(DisplayName = "Writes strings")]
        [InlineData]
        [InlineData("hi")]
        [InlineData("123", "", ":)", "This Is A String")]
        public void WriteStrings(params string[] data)
        {
            ByteWriter writer = new();
            
            writer.String(data);

            IEnumerable<byte> expected = data.SelectMany(Encoding.UTF8.GetBytes);
            writer.GetData().Should().Equal(expected);
        }        
        
        [Theory(DisplayName = "Writes strings with a null terminator")]
        [InlineData]
        [InlineData("hi")]
        [InlineData("123", "", ":)", "This Is A String")]
        public void WriteNullTerminatedStrings(params string[] data)
        {
            ByteWriter writer = new();
            
            writer.StringNullTerminated(data);

            IEnumerable<byte> expected = data.Select(s => s + "\0").SelectMany(Encoding.UTF8.GetBytes);
            writer.GetData().Should().Equal(expected);
        }
        
        [Theory(DisplayName = "Writes strings to the eight-char null padded format")]
        [InlineData]
        [InlineData("hi")]
        [InlineData("123", "", ":)", "This Is A String")]
        public void WriteEightCharStrings(params string[] data)
        {
            ByteWriter writer = new();
            
            writer.EightCharString(data);

            IEnumerable<byte> expected = data.Select(s => s.Length > 8 ? s[..8] : s.PadRight(8, '\0'))
                                             .SelectMany(Encoding.UTF8.GetBytes);
            writer.GetData().Should().Equal(expected);
        }
    }
}
