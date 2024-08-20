using Helion.Audio;
using Helion.Geometry.Planes;
using Helion.Geometry.Vectors;
using Helion.Maps.Specials;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;
using Helion.Resources.Definitions.SoundInfo;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Sound;
using Helion.World.Static;
using System;

namespace Helion.World.Geometry.Sectors;

public sealed class SectorPlane : ISoundSource
{
    public SectorPlaneFace Facing;
    public PlaneD Plane;
    public Sector Sector;
    public double Z;
    public double PrevZ;
    public int TextureHandle;
    public short LightLevel;
    public int LastRenderChangeGametick;
    public int LastRenderGametick;

    public RenderOffsets RenderOffsets;
    public SectorDynamic Dynamic;
    public StaticGeometryData Static;

    public bool MidTextureHack;
    public bool NoRender;
    public StaticSkyGeometryData? SkyGeometry;

    private IAudioSource? m_audio;
    private SoundInfo? m_soundInfo;

    private readonly double m_initialZ;
    private readonly int m_initialTextureHandle;

    public SectorPlane(SectorPlaneFace facing, double z, int textureHandle, short lightLevel)
    {
        Facing = facing;
        Z = z;
        PrevZ = z;
        TextureHandle = textureHandle;
        LightLevel = lightLevel;
        Plane = new PlaneD(0, 0, 1.0, -z);
        m_initialZ = z;
        m_initialTextureHandle = textureHandle;
        Sector = null!;
    }

    public void Reset(short lightLevel)
    {
        SetZ(m_initialZ);
        PrevZ = m_initialZ;
        m_audio = default;
        m_soundInfo = default;
        TextureHandle = m_initialTextureHandle;
        LightLevel = lightLevel;
        LastRenderChangeGametick = default;
        LastRenderGametick = default;
        Dynamic = default;
        Static = default;
        RenderOffsets = default;
        MidTextureHack = default;
        NoRender = default;
        SkyGeometry = default;
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

        if (RenderOffsets.Gametick != 0)
            return true;

        return false;
    }

    public void SetTexture(int texture, int gametick)
    {
        TextureHandle = texture;
        Sector.PlaneTextureChange(this);
        LastRenderChangeGametick = gametick;
    }

    public Vec3D GetSoundSource(Entity listener, SectorPlaneFace type)
    {
        if (WorldStatic.VanillaSectorSound)
        {
            var box = Sector.GetBoundingBox();
            return new Vec3D(box.Min.X + ((box.Max.X - box.Min.X) /2), box.Min.Y + ((box.Max.Y - box.Min.Y) / 2), listener.Position.Z);
        }

        Vec2D pos2D = listener.Position.XY;
        // Do not count being in the sector if this is a bad self-referencing subsector. E.g. hr2final map01 sector 160
        if (ReferenceEquals(listener.Sector, Sector)&&
            !WorldStatic.World.Geometry.IslandGeometry.BadSubsectors.Contains(listener.Subsector.Id))
        {
            return pos2D.To3D(type == SectorPlaneFace.Floor ? Sector.ToFloorZ(pos2D) : Sector.ToCeilingZ(pos2D));
        }

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

    public Vec3D? GetSoundVelocity() => default;

    public bool CanMakeSound() => true;

    public float GetSoundRadius() => 32;
}
