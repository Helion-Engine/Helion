using System;

namespace Helion.Resources.Definitions.SoundInfo;

public class SoundInfo
{
    public readonly string Name;
    public readonly string EntryName;
    public readonly int PitchShiftRange;
    public readonly bool PlayerEntry;
    public int Limit { get; set; } = 0;
    public int PitchShift { get; set; } = 0;

    public SoundInfo(string name, string entry, int pitchShiftRange, bool playerEntry = false)
    {
        Name = name;
        EntryName = entry;
        PitchShiftRange = pitchShiftRange;
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
