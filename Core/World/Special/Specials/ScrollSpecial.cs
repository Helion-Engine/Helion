using Helion.Geometry.Vectors;
using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;
using Helion.Models;
using Helion.Util;
using Helion.Util.Container;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using System;

namespace Helion.World.Special.Specials;

public enum ScrollType
{
    Scroll,
    Carry
}

[Flags]
enum ScrollSides
{
    Front = 1,
    Back = 2
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
    private readonly ScrollSides m_scrollSides;
    private readonly SideScrollData? m_frontScroll;
    private readonly SideScrollData? m_backScroll;

    public ScrollSpecial(Line line, in Vec2D speed, ZDoomLineScroll scroll, Sector? accelSector = null,
        ZDoomScroll scrollFlags = ZDoomScroll.None)
    {
        m_type = ScrollType.Scroll;
        Speed = speed;
        Line = line;

        m_scrollSides = ScrollSides.Front;
        if ((scroll & ZDoomLineScroll.BothSides) != 0)
            m_scrollSides |= ScrollSides.Back;

        if ((int)scroll > (int)ZDoomLineScroll.LowerTexture)
            m_lineScroll = ZDoomLineScroll.All;
        else
            m_lineScroll = scroll;

        Line.Front.ScrollData ??= new();
        m_frontScroll = Line.Front.ScrollData;
        
        if ((m_scrollSides & ScrollSides.Back) != 0 && Line.Back != null)
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

    public ScrollSpecial(Line line, Sector? accelSector, in ScrollSpecialModel model)
        : this(line, new Vec2D(model.SpeedX, model.SpeedY), (ZDoomLineScroll)model.Type, accelSector, (ZDoomScroll)model.ScrollFlags)
    {
        if ((model.OffsetFrontX != null && model.OffsetFrontY != null) || model.FrontOffset != null)
        {
            line.Front.ScrollData ??= new();
            m_frontScroll = line.Front.ScrollData;
            m_scrollSides |= ScrollSides.Front;

            if (model.FrontOffset != null)
                ApplyScrollOffset(m_frontScroll, model.FrontOffset.Value);
        }

        if (line.Back != null && ((model.OffsetBackX != null && model.OffsetBackY != null) || model.BackOffset != null))
        {
            line.Back.ScrollData ??= new();
            m_backScroll = line.Back.ScrollData;
            m_scrollSides |= ScrollSides.Back;

            if (model.BackOffset != null)
                ApplyScrollOffset(m_backScroll, model.BackOffset.Value);
        }

        // OffsetFrontX and OffsetFrontY are deprecated. Kept for backwards compatibility.
        if (line.Front.ScrollData != null && model.OffsetFrontX != null)
            ApplyScrollOffset(line.Front.ScrollData, model.OffsetFrontX, model.OffsetFrontY);

        if (line.Back != null && line.Back.ScrollData != null && model.OffsetBackX != null)
            ApplyScrollOffset(line.Back.ScrollData, model.OffsetBackX, model.OffsetBackY);
    }

    private static void ApplyScrollOffset(SideScrollData scroll, double[]? offsetX, double[]? offsetY)
    {
        if (offsetX == null || offsetY == null)
            return;

        if (offsetX.Length != 3 || offsetY.Length != 3)
            return;

        scroll.OffsetUpper.X = scroll.LastOffsetUpper.X = offsetX[0];
        scroll.OffsetMiddle.X = scroll.LastOffsetMiddle.X = offsetX[1];
        scroll.OffsetLower.X = scroll.LastOffsetLower.X = offsetX[2];

        scroll.OffsetUpper.Y = scroll.LastOffsetUpper.Y = offsetY[0];
        scroll.OffsetMiddle.Y = scroll.LastOffsetMiddle.Y = offsetY[1];
        scroll.OffsetLower.Y = scroll.LastOffsetLower.Y = offsetY[2];
    }

    private static void ApplyScrollOffset(SideScrollData scroll, in ScrollSideOffsets offsets)
    {
        scroll.OffsetUpper.X = offsets.Up.X;
        scroll.OffsetUpper.Y = offsets.Up.Y;
        scroll.OffsetMiddle.X = offsets.Mid.X;
        scroll.OffsetMiddle.Y = offsets.Mid.Y;
        scroll.OffsetLower.X = offsets.Low.X;
        scroll.OffsetLower.Y = offsets.Low.Y;
    }

    public ScrollSpecial(SectorPlane sectorPlane, Sector? accelSector, in ScrollSpecialModel model)
        : this ((ScrollType)model.Type, sectorPlane, new Vec2D(model.SpeedX, model.SpeedY), accelSector, (ZDoomScroll)model.ScrollFlags)
    {
        if (m_accelScrollSpeed != null && model.AccelSpeedX.HasValue && model.AccelSpeedY.HasValue && model.AccelLastZ.HasValue)
        {
            m_accelScrollSpeed.AccelSpeed.X = model.AccelSpeedX.Value;
            m_accelScrollSpeed.AccelSpeed.Y = model.AccelSpeedY.Value;
            m_accelScrollSpeed.LastHeight = model.AccelLastZ.Value;
        }

        if (model.OffsetX != null)
            sectorPlane.RenderOffsets.Offset.X = model.OffsetX.Value;
        if (model.OffsetY != null)
            sectorPlane.RenderOffsets.Offset.Y = model.OffsetY.Value;
    }

    public ScrollSpecialModel ToSpecialModel()
    {
        if (Line != null)
        {
            var model = new ScrollSpecialModel()
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

            if (Line.Front.ScrollData != null)
                model.FrontOffset = SetScrollSideOffsets(Line.Front.ScrollData);

            if (Line.Back?.ScrollData != null)
                model.BackOffset = SetScrollSideOffsets(Line.Back.ScrollData);

            return model;
        }
        else if (SectorPlane != null)
        {
            return new()
            {
                SectorId = SectorPlane.Sector.Id,
                PlaneType = SectorPlane == SectorPlane.Sector.Floor ? (int)SectorPlaneFace.Floor : (int)SectorPlaneFace.Ceiling,
                Type = (int)m_type,
                SpeedX = Speed.X,
                SpeedY = Speed.Y,
                OffsetX = SectorPlane.RenderOffsets.Offset.X,
                OffsetY = SectorPlane.RenderOffsets.Offset.Y,
                AccelSectorId = m_accelScrollSpeed?.Sector.Id,
                AccelSpeedX = m_accelScrollSpeed?.AccelSpeed.X,
                AccelSpeedY = m_accelScrollSpeed?.AccelSpeed.Y,
                AccelLastZ = m_accelScrollSpeed?.LastHeight,
                ScrollFlags = GetModelScrollFlags()
            };
        }

        throw new HelionException("Scroll special has neither line or sector plane set.");
    }

    private static ScrollSideOffsets SetScrollSideOffsets(SideScrollData sideScroll)
    {
        return new()
        {
            Up = new(sideScroll.OffsetUpper.X, sideScroll.OffsetUpper.Y),
            Mid = new(sideScroll.OffsetMiddle.X, sideScroll.OffsetMiddle.Y),
            Low = new(sideScroll.OffsetLower.X, sideScroll.OffsetLower.Y),
        };
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
        var speed = m_accelScrollSpeed == null ? Speed : m_accelScrollSpeed.AccelSpeed;

        if (Line != null)
            ScrollLine(speed.X, speed.Y);
        else if (SectorPlane != null)
            ScrollPlane(SectorPlane, speed.X, speed.Y);

        return SpecialTickStatus.Continue;
    }

    private void ScrollLine(double x, double y)
    {
        if (m_frontScroll != null)
            Scroll(m_frontScroll, x, y);
        if (m_backScroll != null)
            Scroll(m_backScroll, -x, y);
    }

    private void Scroll(SideScrollData scrollData, double x, double y)
    {
        bool updateInterpolation = WorldStatic.World.Gametick != scrollData.Gametick; 
        if (m_lineScroll == ZDoomLineScroll.All || (m_lineScroll & ZDoomLineScroll.UpperTexture) != 0)
        {
            if (updateInterpolation)
                scrollData.LastOffsetUpper = scrollData.OffsetUpper;
            scrollData.OffsetUpper.X += x;
            scrollData.OffsetUpper.Y += y;
        }

        if (m_lineScroll == ZDoomLineScroll.All || (m_lineScroll & ZDoomLineScroll.MiddleTexture) != 0)
        {
            if (updateInterpolation)
                scrollData.LastOffsetMiddle = scrollData.OffsetMiddle;
            scrollData.OffsetMiddle.X += x;
            scrollData.OffsetMiddle.Y += y;
        }

        if (m_lineScroll == ZDoomLineScroll.All || (m_lineScroll & ZDoomLineScroll.LowerTexture) != 0)
        {
            if (updateInterpolation)
                scrollData.LastOffsetLower = scrollData.OffsetLower;
            scrollData.OffsetLower.X += x;
            scrollData.OffsetLower.Y += y;
        }

        scrollData.Gametick = WorldStatic.World.Gametick;
    }

    private void ScrollPlane(SectorPlane sectorPlane, double x, double y)
    {
        ref var scroll = ref sectorPlane.RenderOffsets;
        if (m_type == ScrollType.Scroll)
        {
            if (x == 0 && y == 0)
            {
                scroll.LastOffset = scroll.Offset; 
                return;
            }

            if (scroll.Gametick != WorldStatic.World.Gametick)
            {
                scroll.Gametick = WorldStatic.World.Gametick;
                scroll.LastOffset = scroll.Offset;
            }
            scroll.Offset.X += x;
            scroll.Offset.Y += y;
            sectorPlane.Sector.DataChanges |= SectorDataTypes.Offset;
        }
        else if (m_type == ScrollType.Carry && sectorPlane == sectorPlane.Sector.Floor)
        {
            var node = sectorPlane.Sector.Entities.Head;
            while (node != null)
            {
                var entity = node.Value;
                node = node.Next;

                if (entity.Flags.NoClip || entity.Flags.NoBlockmap)
                    continue;

                // Boom would carry anything that was considered 'underwater'
                var waterHeight = double.MinValue;
                var transfer = sectorPlane.Sector.TransferHeights;
                if (transfer != null)
                    waterHeight = transfer.ControlSector.Floor.Z > sectorPlane.Sector.Floor.Z ?
                        transfer.ControlSector.Floor.Z : double.MinValue;
                
                if (entity.Position.Z >= waterHeight && (entity.Flags.NoGravity || !entity.OnGround || !entity.OnSectorFloorZ(sectorPlane.Sector)))
                    continue;

                entity.Velocity.X += x;
                entity.Velocity.Y += y;
                entity.Flags.IgnoreDropOff = true;
            }
        }
    }

    public void ResetInterpolation()
    {
        if (SectorPlane != null && m_type == ScrollType.Scroll)
            SectorPlane.RenderOffsets.LastOffset = SectorPlane.RenderOffsets.Offset;
        else if (Line != null)
            ScrollLine(0, 0);
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
            scroll.m_scrollSides == m_scrollSides &&
            scroll.Speed == Speed;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
