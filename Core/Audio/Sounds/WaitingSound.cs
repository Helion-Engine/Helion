using Helion.Geometry.Vectors;
using Helion.Resources.Definitions.SoundInfo;
using Helion.World.Sound;

namespace Helion.Audio.Sounds;

public struct WaitingSound
{
    public WaitingSound(ISoundSource source, Vec3D? position, Vec3D? velocity, SoundInfo soundInfo, int priority, SoundParams soundParams)
    {
        SoundSource = source;
        Position = position;
        Velocity = velocity;
        SoundInfo = soundInfo;
        Priority = priority;
        SoundParams = soundParams;
    }

    public ISoundSource SoundSource { get; set; }
    public Vec3D? Position { get; set; }
    public Vec3D? Velocity { get; set; }
    public SoundInfo SoundInfo { get; set; }
    public int Priority { get; set; }
    public SoundParams SoundParams { get; set; }
}
