using Helion.Audio;
using Helion.Geometry.Vectors;
using Helion.Resources.Definitions.SoundInfo;
using Helion.Util.Extensions;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Sound;

namespace Helion.World.Geometry.Sectors;

public abstract class SectorSoundSource : ISoundSource
{
    private IAudioSource? m_audio;
    private SoundInfo? m_soundInfo;

    public abstract Sector SoundSector { get; }

    public void ResetSound()
    {
        m_audio = default;
        m_soundInfo = default;
    }

    public Vec3D GetSoundSource(Entity listener, SectorPlaneFace type)
    {
        if (WorldStatic.VanillaSectorSound)
        {
            var box = SoundSector.GetBoundingBox();
            return new Vec3D(box.Min.X + ((box.Max.X - box.Min.X) / 2), box.Min.Y + ((box.Max.Y - box.Min.Y) / 2), listener.Position.Z);
        }

        Vec2D pos2D = listener.Position.XY;
        // Do not count being in the sector if this is a bad self-referencing subsector. E.g. hr2final map01 sector 160
        if (ReferenceEquals(listener.Sector, SoundSector) &&
            !WorldStatic.World.Geometry.IslandGeometry.BadSubsectors.Contains(listener.SubsectorId))
        {
            return pos2D.To3D(type == SectorPlaneFace.Floor ? SoundSector.ToFloorZ(pos2D) : SoundSector.ToCeilingZ(pos2D));
        }

        double z = listener.Position.Z;
        pos2D = GetClosestPointFrom(pos2D);

        // Check if the player z is in line with the lower/upper of the moving sector
        // This is set to the player z so the sound doesn't attenuate on z axis
        if (type == SectorPlaneFace.Floor)
        {
            double floorZ = SoundSector.ToFloorZ(pos2D);
            if (floorZ < z)
                z = floorZ;
        }
        else
        {
            double ceilingZ = SoundSector.ToCeilingZ(pos2D);
            if (ceilingZ > z)
                z = ceilingZ;
        }

        return new Vec3D(pos2D.X, pos2D.Y, z);
    }

    public Vec2D GetClosestPointFrom(in Vec2D point)
    {
        double minDist = double.MaxValue;
        Line? minLine = null;

        for (int i = 0; i < SoundSector.Lines.Length; i++)
        {
            var line = SoundSector.Lines[i];
            if (line.Back != null && line.Front.Sector == line.Back.Sector)
                continue;
            double dist = line.Segment.ClosestPoint(point).Distance(point);
            if (dist < minDist)
            {
                minDist = dist;
                minLine = line;
            }
        }

        if (minLine != null)
            return minLine.Segment.ClosestPoint(point);

        return Vec2D.Zero;
    }

    public void SoundCreated(SoundInfo soundInfo, IAudioSource? audioSource, SoundChannel channel)
    {
        m_audio = audioSource;
        m_soundInfo = soundInfo;
    }

    public bool TryClearSound(string sound, SoundChannel channel, out IAudioSource? clearedSound)
    {
        if (m_soundInfo != null && m_soundInfo.Name.EqualsIgnoreCase(sound))
        {
            clearedSound = m_audio;
            m_audio = null;
            m_soundInfo = null;
            return true;
        }

        clearedSound = null;
        return false;
    }

    public bool HasSound(string sound, SoundChannel channel)
    {
        return m_soundInfo != null && m_soundInfo.Name.EqualsIgnoreCase(sound);
    }

    public void ClearSound(IAudioSource audioSource, SoundChannel channel)
    {
        m_audio = null;
        m_soundInfo = null;
    }

    // Use the sector's LastActivePlaneMove. The move special itself may have been destroyed,
    // but the distance needs to be calculated for stop sounds long after the movement has completed.
    public double GetDistanceSquaredFrom(Entity listenerEntity) =>
        GetSoundSource(listenerEntity, SoundSector.LastActivePlaneMove).DistanceSquared(listenerEntity.Position);

    public Vec3D? GetSoundPosition(Entity listenerEntity) =>
        GetSoundSource(listenerEntity, SoundSector.LastActivePlaneMove);

    public Vec3D? GetSoundVelocity() => default;

    public bool CanMakeSound() => true;

    public float GetSoundRadius() => 32;
}
