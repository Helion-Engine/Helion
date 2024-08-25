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

    public Vec2D Speed;

    public bool OverrideEquals => true;

    private readonly ScrollType m_type;
    private readonly AccelScrollSpeed? m_accelScrollSpeed;
    private readonly ZDoomLineScroll m_lineScroll;
    private readonly SideTexture m_sideTextures;
    private readonly bool m_scrollLineFront;
    private readonly SideScrollData? m_frontScroll;
    private readonly SideScrollData? m_backScroll;

    public ScrollSpecial(Line line, in Vec2D speed, ZDoomLineScroll scroll, Sector? accelSector = null,
        ZDoomScroll scrollFlags = ZDoomScroll.None)
    {
        m_type = ScrollType.Scroll;
        Speed = speed;
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

        Line.Front.ScrollData ??= new();
        m_frontScroll = Line.Front.ScrollData;
        
        if ((scroll & ZDoomLineScroll.BothSides) != 0 && Line.Back != null)
        {
            Line.Back.ScrollData ??= new();
            m_backScroll = Line.Back.ScrollData;
        }

        if (accelSector != null)
            m_accelScrollSpeed = new AccelScrollSpeed(accelSector, speed, scrollFlags);
    }

    public ScrollSpecial(ScrollType type, SectorPlane sectorPlane, in Vec2D speed, Sector? accelSector = null,
        ZDoomScroll scrollFlags = ZDoomScroll.None)
    {
        m_type = type;
        SectorPlane = sectorPlane;
        Speed = speed;
        if (accelSector != null)
            m_accelScrollSpeed = new AccelScrollSpeed(accelSector, speed, scrollFlags);
    }

    public ScrollSpecial(Line line, Sector? accelSector, ScrollSpecialModel model)
        : this(line, new Vec2D(model.SpeedX, model.SpeedY), (ZDoomLineScroll)model.Type, accelSector, (ZDoomScroll)model.ScrollFlags)
    {

    }

    public ScrollSpecial(SectorPlane sectorPlane, Sector? accelSector, ScrollSpecialModel model)
        : this ((ScrollType)model.Type, sectorPlane, new Vec2D(model.SpeedX, model.SpeedY), accelSector, (ZDoomScroll)model.ScrollFlags)
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
                SpeedX = Speed.X,
                SpeedY = Speed.Y,
                AccelSectorId = m_accelScrollSpeed?.Sector.Id,
                AccelSpeedX = m_accelScrollSpeed?.AccelSpeed.X,
                AccelSpeedY = m_accelScrollSpeed?.AccelSpeed.Y,
                AccelLastZ = m_accelScrollSpeed?.LastHeight,
                ScrollFlags = GetModelScrollFlags()
            };

            if (m_scrollLineFront && Line.Front.ScrollData != null)
            {
                model.OffsetFrontX = [Line.Front.ScrollData.OffsetUpper.X, Line.Front.ScrollData.OffsetMiddle.X, Line.Front.ScrollData.OffsetLower.X];
                model.OffsetFrontY = [Line.Front.ScrollData.OffsetUpper.Y, Line.Front.ScrollData.OffsetMiddle.Y, Line.Front.ScrollData.OffsetLower.Y];
            }

            if (!m_scrollLineFront && Line.Back?.ScrollData != null)
            {
                model.OffsetBackX = [Line.Back.ScrollData.OffsetUpper.X, Line.Back.ScrollData.OffsetMiddle.X, Line.Back.ScrollData.OffsetLower.X];
                model.OffsetBackY = [Line.Back.ScrollData.OffsetUpper.Y, Line.Back.ScrollData.OffsetMiddle.Y, Line.Back.ScrollData.OffsetLower.Y];
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
                SpeedX = Speed.X,
                SpeedY = Speed.Y,
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
        Vec2D speed = m_accelScrollSpeed == null ? Speed : m_accelScrollSpeed.AccelSpeed;

        if (Line != null)
            ScrollLine(speed);
        else if (SectorPlane != null)
            ScrollPlane(SectorPlane, speed);

        return SpecialTickStatus.Continue;
    }

    private void ScrollLine(in Vec2D speed)
    {
        if (m_frontScroll != null)
            Scroll(m_frontScroll, speed);
        if (m_backScroll != null)
            Scroll(m_backScroll, -speed);
    }

    private void Scroll(SideScrollData scrollData, in Vec2D speed)
    {
        bool updateInterpolation = WorldStatic.World.Gametick != scrollData.Gametick; 
        if (m_lineScroll == ZDoomLineScroll.All || (m_lineScroll & ZDoomLineScroll.UpperTexture) != 0)
        {
            if (updateInterpolation)
                scrollData.LastOffsetUpper = scrollData.OffsetUpper;
            scrollData.OffsetUpper += speed;
        }

        if (m_lineScroll == ZDoomLineScroll.All || (m_lineScroll & ZDoomLineScroll.MiddleTexture) != 0)
        {
            if (updateInterpolation)
                scrollData.LastOffsetMiddle = scrollData.OffsetMiddle;
            scrollData.OffsetMiddle += speed;
        }

        if (m_lineScroll == ZDoomLineScroll.All || (m_lineScroll & ZDoomLineScroll.LowerTexture) != 0)
        {
            if (updateInterpolation)
                scrollData.LastOffsetLower = scrollData.OffsetLower;
            scrollData.OffsetLower += speed;
        }

        scrollData.Gametick = WorldStatic.World.Gametick;
    }

    private void ScrollPlane(SectorPlane sectorPlane, in Vec2D speed)
    {
        ref RenderOffsets scroll = ref sectorPlane.RenderOffsets;
        if (m_type == ScrollType.Scroll)
        {
            if (speed == Vec2D.Zero)
            {
                scroll.LastOffset = scroll.Offset; 
                return;
            }

            if (scroll.Gametick != WorldStatic.World.Gametick)
            {
                scroll.Gametick = WorldStatic.World.Gametick;
                scroll.LastOffset = scroll.Offset;
            }
            scroll.Offset += speed;
            sectorPlane.Sector.DataChanges |= SectorDataTypes.Offset;
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
                
                if (entity.Position.Z >= waterHeight && (entity.Flags.NoBlockmap || entity.Flags.NoGravity || !entity.OnGround || !entity.OnSectorFloorZ(sectorPlane.Sector)))
                    continue;

                entity.Velocity.X += speed.X;
                entity.Velocity.Y += speed.Y;
                entity.Flags.IgnoreDropOff = true;
            }
        }
    }

    public void ResetInterpolation()
    {
        if (SectorPlane != null && m_type == ScrollType.Scroll)
            SectorPlane.RenderOffsets.LastOffset = SectorPlane.RenderOffsets.Offset;
        else if (Line != null)
            ScrollLine(Vec2D.Zero);
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
            scroll.Speed == Speed;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
