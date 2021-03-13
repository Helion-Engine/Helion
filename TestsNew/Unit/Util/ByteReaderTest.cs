using System.IO;
using FluentAssertions;
using Helion.Util;
using Xunit;

namespace Helion.TestsNew.Unit.Util
{
    public class ByteReaderTest
    {
        [Fact(DisplayName = "Reads a byte array")]
        public void ReadByteArray()
        {
            byte[] data = { 0, 1, 2, 4 };
            ByteReader reader = new(data);
            
            reader.ReadByte().Should().Be(0);
            reader.ReadByte().Should().Be(1);
            reader.ReadInt16().Should().Be(0x0402);
        }
        
        [Fact(DisplayName = "Reads from a binary reader")]
        public void ReadBinaryReader()
        {
            byte[] data = { 0, 1, 2, 4 };
            BinaryReader binaryReader = new(new MemoryStream(data));
            ByteReader reader = new(binaryReader);
            
            reader.ReadByte().Should().Be(0);
            reader.ReadByte().Should().Be(1);
            reader.ReadInt16().Should().Be(0x0402);
        }
        
        [Fact(DisplayName = "Gets the number of bytes from the reader")]
        public void GetReaderByteLength()
        {
            byte[] data = { 0, 1, 2, 4 };
            ByteReader reader = new(data);
            
            reader.Length.Should().Be(data.Length);
        }
        
        [Fact(DisplayName = "Read null terminated string")]
        public void ReadNullTerminatedString()
        {
            byte[] data = { (byte)'h', (byte)'i', 0, (byte)'!', 0 };
            ByteReader reader = new(data);
            
            reader.ReadNullTerminatedString().Should().Be("hi");
            reader.ReadNullTerminatedString().Should().Be("!");
        }
        
        [Fact(DisplayName = "Read eight char doom string")]
        public void ReadEightCharDoomString()
        {
            byte[] data = { (byte)'h', (byte)'i', 0, 0, 0, 0, 0, 0, 
                            (byte)'@', 0, 0, 0, 0, 0, 0, 0,
                            0, 0, 0, 0, 0, 0, 0, 0, 
                            (byte)'a', (byte)'a', (byte)'a', (byte)'a', (byte)'a', (byte)'a', (byte)'a', (byte)'a',  
                            1, 2, 3 };
            ByteReader reader = new(data);
            
            reader.ReadEightByteString().Should().Be("hi");
            reader.ReadEightByteString().Should().Be("@");
            reader.ReadEightByteString().Should().Be("");
            reader.ReadEightByteString().Should().Be("aaaaaaaa");
        }
        
        [Fact(DisplayName = "Reads a string with a specific length")]
        public void ReadStringOfSpecificLength()
        {
            byte[] data = { (byte)'h', (byte)'i', (byte)'!', 0 };
            ByteReader reader = new(data);
            
            reader.ReadStringLength(0).Should().Be("");
            reader.ReadStringLength(2).Should().Be("hi");
            reader.ReadStringLength(1).Should().Be("!");
        }
        
        [Fact(DisplayName = "Reads a 32-bit big endian integer")]
        public void ReadBE32Int()
        {
            byte[] data = { 4, 2, 1, 9 };
            ByteReader reader = new(data);
            
            reader.ReadInt32BE().Should().Be(0x04020109);
        }
        
        [Fact(DisplayName = "Correctly returns number of bytes remaining")]
        public void ChecksByteRemaining()
        {
            byte[] data = { 4, 2, 1, 9 };
            ByteReader reader = new(data);
            
            reader.HasBytesRemaining(5).Should().BeFalse();
            reader.HasBytesRemaining(4).Should().BeTrue();
            reader.HasBytesRemaining(1).Should().BeTrue();

            reader.ReadInt16();
            reader.HasBytesRemaining(2).Should().BeTrue();
            
            reader.ReadInt16();
            reader.HasBytesRemaining(0).Should().BeTrue();
        }
        
        [Fact(DisplayName = "Advances the stream correctly")]
        public void AdvanceStream()
        {
            byte[] data = { 4, 2, 1, 9 };
            ByteReader reader = new(data);
            
            reader.HasBytesRemaining(4).Should().BeTrue();
            
            reader.Advance(3);
            reader.HasBytesRemaining(1).Should().BeTrue();
        }
        
        [Fact(DisplayName = "Mutates the stream offset")]
        public void ChangeOffset()
        {
            byte[] data = { 4, 2, 1, 9 };
            ByteReader reader = new(data);
            
            reader.Offset(2);
            reader.ReadByte().Should().Be(1);
            reader.ReadByte().Should().Be(9);
            
            reader.Offset(0);
            reader.ReadByte().Should().Be(4);
        }
    }
}
