using System;

namespace Helion.Resources.Definitions.SoundInfo;

public class SoundInfo
{
    public const int NoPitch = -1;

    public readonly string Name;
    public readonly string EntryName;
    public readonly bool PlayerEntry;

    public int PitchShift { get; set; }
    public int Limit { get; set; } = 0;
    public float PitchSet { get; set; }

    public SoundInfo(string name, string entry, int pitchShift, bool playerEntry = false)
    {
        Name = name;
        EntryName = entry;
        PitchShift = pitchShift;
        PlayerEntry = playerEntry;
    }

    public override bool Equals(object? obj)
    {
        if (obj is SoundInfo soundInfo)
            return soundInfo.Name.Equals(Name, StringComparison.OrdinalIgnoreCase);

        return false;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}
