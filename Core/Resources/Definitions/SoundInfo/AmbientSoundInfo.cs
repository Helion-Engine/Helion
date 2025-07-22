using Helion.Util;
namespace Helion.Resources.Definitions.SoundInfo;

public enum AmbientSoundType
{
    Point,
    Surround,
    World,
}

public enum AmbientSoundMode
{
    Continuous,
    Random,
    Periodic
}

public class AmbientSoundInfo
{
    public readonly int Index;
    public readonly string LogicalSound;
    public readonly AmbientSoundType Type;
    public readonly AmbientSoundMode Mode;
    public readonly float Volume;
    public readonly float Attenuation;
    public readonly int MinTicks;
    public readonly int MaxTicks;

    public AmbientSoundInfo(int index, string logicalSound, AmbientSoundType type, AmbientSoundMode mode, float volume, float attenuation, float? minSeconds, float? maxSeconds)
    {
        Index = index;
        LogicalSound = logicalSound;
        Type = type;
        Mode = mode;
        Volume = volume;
        Attenuation = attenuation;
        if (minSeconds.HasValue)
            MinTicks = (int)(Constants.TicksPerSecond * minSeconds);
        if (maxSeconds.HasValue)
            MaxTicks = (int)(Constants.TicksPerSecond * maxSeconds);
    }
}
