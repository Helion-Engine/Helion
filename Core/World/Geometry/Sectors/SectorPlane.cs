using Helion.Audio;
using Helion.Geometry.Planes;
using Helion.Geometry.Vectors;
using Helion.Maps.Specials;
using Helion.Resources.Definitions.SoundInfo;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Sound;
using Helion.World.Static;
using System;

namespace Helion.World.Geometry.Sectors;

public class SectorPlane : ISoundSource
{
    public readonly int Id;
    public readonly SectorPlaneFace Facing;
    public PlaneD Plane;
    public Sector Sector { get; internal set; }
    public double Z;
    public double PrevZ;
    public int TextureHandle { get; set; }
    public short LightLevel { get; set; }
    public int LastRenderChangeGametick;
    public int LastRenderGametick;
    public short RenderLightLevel => Facing == SectorPlaneFace.Floor ? Sector.FloorRenderLightLevel : Sector.CeilingRenderLightLevel;

    public SectorScrollData? SectorScrollData;
    public SectorDynamic Dynamic;
    public StaticGeometryData Static;

    private IAudioSource? m_audio;
    private SoundInfo? m_soundInfo;

    public SectorPlane(int id, SectorPlaneFace facing, double z, int textureHandle, short lightLevel)
    {
        Id = id;
        Facing = facing;
        Z = z;
        PrevZ = z;
        TextureHandle = textureHandle;
        LightLevel = lightLevel;
        Plane = new PlaneD(0, 0, 1.0, -z);

        // We are okay with things blowing up violently if someone forgets
        // to assign it, because that is such a critical error on the part
        // of the developer if this ever happens that it's deserved. Fixing
        // this would lead to some very messy logic, and when this is added
        // to a parent object, it will add itself for us. If this can be
        // fixed in the future with non-messy code, go for it.
        Sector = null !;
    }

    public void SetZ(double z)
    {
        Plane.MoveZ(z - Z);
        Z = z;
    }

    public void SetSectorMoveChanged(int gametick) => LastRenderChangeGametick = gametick;

    public bool CheckRenderingChanged()
    {
        if (LastRenderChangeGametick >= LastRenderGametick - 1)
            return true;

        if (PrevZ != Z)
            return true;

        if (SectorScrollData != null)
            return true;

        return false;
    }

    public void CreateScrollData()
    {
        SectorScrollData = new();
    }

    public void SetTexture(int texture, int gametick)
    {
        TextureHandle = texture;
        Sector.PlaneTextureChange(this);
        LastRenderChangeGametick = gametick;
    }

    public Vec3D GetSoundSource(Entity listener, SectorPlaneFace type)
    {
        Vec2D pos2D = listener.Position.XY;
        if (listener.Sector.Equals(Sector))
            return pos2D.To3D(type == SectorPlaneFace.Floor ? Sector.ToFloorZ(pos2D) : Sector.ToCeilingZ(pos2D));

        double z = listener.Position.Z;
        pos2D = GetClosestPointFrom(pos2D);

        // Check if the player z is in line with the lower/upper of the moving sector
        // This is set to the player z so the sound doesn't attenuate on z axis
        if (type == SectorPlaneFace.Floor)
        {
            double floorZ = Sector.ToFloorZ(pos2D);
            if (floorZ < z)
                z = floorZ;
        }
        else
        {
            double ceilingZ = Sector.ToCeilingZ(pos2D);
            if (ceilingZ > z)
                z = ceilingZ;
        }

        return new Vec3D(pos2D.X, pos2D.Y, z);
    }

    public Vec2D GetClosestPointFrom(in Vec2D point)
    {
        double minDist = double.MaxValue;
        Line? minLine = null;

        for (int i = 0; i < Sector.Lines.Count; i++)
        {
            var line = Sector.Lines[i];
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

    public Line? GetClosestLineCenterFrom(in Vec2D point)
    {
        double minDist = double.MaxValue;
        Line? minLine = null;

        for (int i = 0; i < Sector.Lines.Count; i++)
        {
            var line = Sector.Lines[i];
            double dist = line.Segment.FromTime(0.5).Distance(point);
            if (dist < minDist)
            {
                minDist = dist;
                minLine = line;
            }
        }

        if (minLine != null)
            return minLine;

        return null;
    }

    public void SoundCreated(SoundInfo soundInfo, IAudioSource? audioSource, SoundChannel channel)
    {
        m_audio = audioSource;
        m_soundInfo = soundInfo;
    }

    public bool TryClearSound(string sound, SoundChannel channel, out IAudioSource? clearedSound)
    {
        if (m_soundInfo != null && m_soundInfo.Name.Equals(sound, StringComparison.OrdinalIgnoreCase))
        {
            clearedSound = m_audio;
            m_audio = null;
            m_soundInfo = null;
            return true;
        }

        clearedSound = null;
        return false;
    }

    public void ClearSound(IAudioSource audioSource, SoundChannel channel)
    {
        m_audio = null;
        m_soundInfo = null;
    }

    // Use the sector's LastActivePlaneMove. The move special itself may have been destroyed,
    // but the distance needs to be calculated for stop sounds long after the movement has completed.
    public double GetDistanceFrom(Entity listenerEntity) =>
        GetSoundSource(listenerEntity, Sector.LastActivePlaneMove).Distance(listenerEntity.Position);

    public Vec3D? GetSoundPosition(Entity listenerEntity) =>
        GetSoundSource(listenerEntity, Sector.LastActivePlaneMove);

    public Vec3D? GetSoundVelocity() => Vec3D.Zero;

    public bool CanMakeSound() => true;
}
