using Helion.Geometry.Vectors;
using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;
using Helion.Models;
using Helion.Util;
using Helion.Util.Container;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Static;
using System.Linq;

namespace Helion.World.Special.Specials;

public enum ScrollType
{
    Scroll,
    Carry
}

public class ScrollSpecial : ISpecial
{
    public readonly SectorPlane? SectorPlane;
    public readonly Line? Line;

    public Vec2D Speed => m_speed;

    public bool OverrideEquals => true;

    private readonly IWorld m_world;
    private readonly ScrollType m_type;
    private readonly AccelScrollSpeed? m_accelScrollSpeed;
    private readonly ZDoomLineScroll m_lineScroll;
    private readonly SideTexture m_sideTextures;
    private readonly bool m_scrollLineFront;
    private Vec2D m_speed;

    public ScrollSpecial(IWorld world, Line line, in Vec2D speed, ZDoomLineScroll scroll, bool front = true, Sector? accelSector = null,
        ZDoomScroll scrollFlags = ZDoomScroll.None)
    {
        m_world = world;
        m_type = ScrollType.Scroll;
        m_speed = speed;
        Line = line;
        if ((int)scroll > (int)ZDoomLineScroll.LowerTexture)
            m_lineScroll = ZDoomLineScroll.All;
        else
            m_lineScroll = scroll;

        if (m_lineScroll == ZDoomLineScroll.All)
        {
            m_sideTextures = SideTexture.Upper | SideTexture.Lower | SideTexture.Middle;
        }
        else
        {
            if ((m_lineScroll & ZDoomLineScroll.UpperTexture) != 0)
                m_sideTextures |= SideTexture.Upper;
            if ((m_lineScroll & ZDoomLineScroll.LowerTexture) != 0)
                m_sideTextures |= SideTexture.Lower;
            if ((m_lineScroll & ZDoomLineScroll.MiddleTexture) != 0)
                m_sideTextures |= SideTexture.Middle;
        }

        m_scrollLineFront = front;
        if (m_scrollLineFront)
            Line.Front.ScrollData = new();
        else if (Line.Back != null)
            Line.Back.ScrollData = new();

        if (accelSector != null)
            m_accelScrollSpeed = new AccelScrollSpeed(accelSector, speed, scrollFlags);
    }

    public ScrollSpecial(IWorld world, ScrollType type, SectorPlane sectorPlane, in Vec2D speed, Sector? accelSector = null,
        ZDoomScroll scrollFlags = ZDoomScroll.None)
    {
        m_world = world;
        m_type = type;
        SectorPlane = sectorPlane;
        m_speed = speed;
        if (accelSector != null)
            m_accelScrollSpeed = new AccelScrollSpeed(accelSector, speed, scrollFlags);

        // Only create if visually scrolling
        if (type == ScrollType.Scroll)
            SectorPlane.CreateScrollData();
    }

    public ScrollSpecial(IWorld world, Line line, Sector? accelSector, ScrollSpecialModel model)
        : this(world, line, new Vec2D(model.SpeedX, model.SpeedY), (ZDoomLineScroll)model.Type, model.Front, accelSector, (ZDoomScroll)model.ScrollFlags)
    {

    }

    public ScrollSpecial(IWorld world, SectorPlane sectorPlane, Sector? accelSector, ScrollSpecialModel model)
        : this (world, (ScrollType)model.Type, sectorPlane, new Vec2D(model.SpeedX, model.SpeedY), accelSector, (ZDoomScroll)model.ScrollFlags)
    {
        if (m_accelScrollSpeed != null && model.AccelSpeedX.HasValue && model.AccelSpeedY.HasValue && model.AccelLastZ.HasValue)
        {
            m_accelScrollSpeed.AccelSpeed.X = model.AccelSpeedX.Value;
            m_accelScrollSpeed.AccelSpeed.Y = model.AccelSpeedY.Value;
            m_accelScrollSpeed.LastHeight = model.AccelLastZ.Value;
        }
    }

    public ISpecialModel ToSpecialModel()
    {
        if (Line != null)
        {
            ScrollSpecialModel model = new ScrollSpecialModel()
            {
                LineId = Line.Id,
                Type = (int)m_lineScroll,
                SpeedX = m_speed.X,
                SpeedY = m_speed.Y,
                Front = m_scrollLineFront,
                AccelSectorId = m_accelScrollSpeed?.Sector.Id,
                AccelSpeedX = m_accelScrollSpeed?.AccelSpeed.X,
                AccelSpeedY = m_accelScrollSpeed?.AccelSpeed.Y,
                AccelLastZ = m_accelScrollSpeed?.LastHeight,
                ScrollFlags = GetModelScrollFlags()
            };

            if (m_scrollLineFront && Line.Front.ScrollData != null)
            {
                model.OffsetFrontX = Line.Front.ScrollData.Offset.Select(v => v.X).ToArray();
                model.OffsetFrontY = Line.Front.ScrollData.Offset.Select(v => v.Y).ToArray();
            }

            if (!m_scrollLineFront && Line.Back?.ScrollData != null)
            {
                model.OffsetBackX = Line.Back.ScrollData.Offset.Select(v => v.X).ToArray();
                model.OffsetBackY = Line.Back.ScrollData.Offset.Select(v => v.Y).ToArray();
            }

            return model;
        }
        else if (SectorPlane != null)
        {
            return new ScrollSpecialModel()
            {
                SectorId = SectorPlane.Sector.Id,
                PlaneType = SectorPlane == SectorPlane.Sector.Floor ? (int)SectorPlaneFace.Floor : (int)SectorPlaneFace.Ceiling,
                Type = (int)m_type,
                SpeedX = m_speed.X,
                SpeedY = m_speed.Y,
                AccelSectorId = m_accelScrollSpeed?.Sector.Id,
                AccelSpeedX = m_accelScrollSpeed?.AccelSpeed.X,
                AccelSpeedY = m_accelScrollSpeed?.AccelSpeed.Y,
                AccelLastZ = m_accelScrollSpeed?.LastHeight,
                ScrollFlags = GetModelScrollFlags()
            };
        }

        throw new HelionException("Scroll special has neither line or sector plane set.");
    }

    private int GetModelScrollFlags()
    {
        if (m_accelScrollSpeed != null)
            return (int)m_accelScrollSpeed.ScrollFlags;

        return 0;
    }

    public SpecialTickStatus Tick()
    {
        m_accelScrollSpeed?.Tick();
        Vec2D speed = m_accelScrollSpeed == null ? m_speed : m_accelScrollSpeed.AccelSpeed;

        if (Line != null)
            ScrollLine(Line, speed);
        else if (SectorPlane != null)
            ScrollPlane(SectorPlane, speed);

        return SpecialTickStatus.Continue;
    }

    private void ScrollLine(Line line, in Vec2D speed)
    {
        if (m_scrollLineFront)
        {
            Scroll(line.Front.ScrollData!, speed);
            line.Front.OffsetChanged = true;
            m_world.SetSideScroll(line.Front, m_sideTextures);
        }
        else if (line.Back != null)
        {
            Scroll(line.Back.ScrollData!, speed);
            line.Back.OffsetChanged = true;
            m_world.SetSideScroll(line.Back, m_sideTextures);
        }
    }

    private void Scroll(SideScrollData scrollData, in Vec2D speed)
    {
        if (m_lineScroll == ZDoomLineScroll.All || (m_lineScroll & ZDoomLineScroll.UpperTexture) != 0)
        {
            scrollData.LastOffset[SideScrollData.UpperPosition] = scrollData.Offset[SideScrollData.UpperPosition];
            scrollData.Offset[SideScrollData.UpperPosition] += speed;
        }

        if (m_lineScroll == ZDoomLineScroll.All || (m_lineScroll & ZDoomLineScroll.MiddleTexture) != 0)
        {
            scrollData.LastOffset[SideScrollData.MiddlePosition] = scrollData.Offset[SideScrollData.MiddlePosition];
            scrollData.Offset[SideScrollData.MiddlePosition] += speed;
        }

        if (m_lineScroll == ZDoomLineScroll.All || (m_lineScroll & ZDoomLineScroll.LowerTexture) != 0)
        {
            scrollData.LastOffset[SideScrollData.LowerPosition] = scrollData.Offset[SideScrollData.LowerPosition];
            scrollData.Offset[SideScrollData.LowerPosition] += speed;
        }
    }

    private void ScrollPlane(SectorPlane sectorPlane, in Vec2D speed)
    {
        if (m_type == ScrollType.Scroll)
        {
            if (speed == Vec2D.Zero)
            {
                sectorPlane.SectorScrollData!.LastOffset = sectorPlane.SectorScrollData!.Offset;
                return;
            }

            sectorPlane.SectorScrollData!.LastOffset = sectorPlane.SectorScrollData!.Offset;
            sectorPlane.SectorScrollData!.Offset += speed;
            sectorPlane.Sector.DataChanges |= SectorDataTypes.Offset;
            m_world.SetSectorPlaneScroll(sectorPlane);
        }
        else if (m_type == ScrollType.Carry && sectorPlane == sectorPlane.Sector.Floor)
        {
            LinkableNode<Entity>? node = sectorPlane.Sector.Entities.Head;
            while (node != null)
            {
                Entity entity = node.Value;
                node = node.Next;
                
                // Boom would carry anything that was considered 'underwater'
                double waterHeight = double.MinValue;
                if (sectorPlane.Sector.TransferHeights != null)
                    waterHeight = sectorPlane.Sector.TransferHeights.ControlSector.Floor.Z > sectorPlane.Sector.Floor.Z ?
                        sectorPlane.Sector.TransferHeights.ControlSector.Floor.Z : double.MinValue;

                if (entity.Flags.NoClip)
                    continue;
                
                if (entity.Position.Z >= waterHeight && (entity.Flags.NoGravity || !entity.OnGround || !entity.OnSectorFloorZ(sectorPlane.Sector)))
                    continue;

                entity.Velocity.X += speed.X;
                entity.Velocity.Y += speed.Y;
            }
        }
    }

    public void ResetInterpolation()
    {
        if (SectorPlane != null && m_type == ScrollType.Scroll)
            SectorPlane.SectorScrollData!.LastOffset = SectorPlane.SectorScrollData!.Offset;
        else if (Line != null)
            ScrollLine(Line, Vec2D.Zero);
    }

    public bool Use(Entity entity)
    {
        return false;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not ScrollSpecial scroll)
            return false;

        bool lineEquals;
        bool planeEquals;
        if (scroll.Line == null)
            lineEquals = Line == null;
        else
            lineEquals = Line != null && scroll.Line.Id == Line.Id;

        if (scroll.SectorPlane == null)
            planeEquals = SectorPlane == null;
        else
            planeEquals = SectorPlane != null && scroll.SectorPlane.Facing == SectorPlane.Facing && scroll.SectorPlane.Sector.Id == SectorPlane.Sector.Id;

        return lineEquals && planeEquals &&
            scroll.m_type == m_type &&
            scroll.m_accelScrollSpeed == m_accelScrollSpeed &&
            scroll.m_lineScroll == m_lineScroll &&
            scroll.m_scrollLineFront == m_scrollLineFront &&
            scroll.m_speed == m_speed;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
