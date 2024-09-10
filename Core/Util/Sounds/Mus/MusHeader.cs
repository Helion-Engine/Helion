using Helion.Util.Bytes;

namespace Helion.Util.Sounds.Mus;

public record MusHeader
{
    public readonly string Header;
    public readonly short ScoreLength;
    public readonly short ScoreStart;
    public readonly short PrimaryChannels;
    public readonly short SecondaryChannels;
    public readonly short InstrumentCount;

    public MusHeader(ByteReader reader)
    {
        Header = reader.ReadStringLength(4);
        ScoreLength = reader.ReadInt16();
        ScoreStart = reader.ReadInt16();
        PrimaryChannels = reader.ReadInt16();
        SecondaryChannels = reader.ReadInt16();
        InstrumentCount = reader.ReadInt16();
    }
}
