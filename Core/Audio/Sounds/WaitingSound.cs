using Helion.Geometry.Vectors;
using Helion.Resources.Definitions.SoundInfo;
using Helion.World.Sound;

namespace Helion.Audio.Sounds;

public struct WaitingSound(ISoundSource source, Vec3D? position, Vec3D? velocity, SoundInfo soundInfo, int priority, float offsetSeconds, int gameTick, SoundParams soundParams)
{
    public ISoundSource SoundSource = source;
    public Vec3D? Position = position;
    public Vec3D? Velocity = velocity;
    public SoundInfo SoundInfo = soundInfo;
    public int Priority = priority;
    public SoundParams SoundParams = soundParams;
    public float OffsetSeconds = offsetSeconds;
    public int GameTick = gameTick;
    public double DistanceSquared;
}
