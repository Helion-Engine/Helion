using Helion.Bsp.Geometry;
using Helion.Geometry.Segments;
using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;
using Helion.Models;
using Helion.World.Entities;
using Helion.World.Geometry.Sides;
using Helion.World.Special;
using Helion.Geometry.Vectors;
using System;
using Helion.World.Geometry.Sectors;
using System.Collections.Generic;
using Helion.World.Special.Switches;
using Helion.Resources;
using Helion.World.Bsp;
using System.Linq;
using Helion.World.Geometry.Islands;
using Helion.Maps.Components.GL;

namespace Helion.World.Geometry.Lines;

public class Line : IBspUsableLine
{
    public const int NoLineId = 0;

    public int Id { get; }
    public readonly int MapId;
    public readonly Seg2D Segment;
    public readonly Side Front;
    public readonly Side? Back;
    public readonly List<BspSubsectorSeg> SubsectorSegs = new();
    public Island Island = null!;
    public int LineId { get; set; }
    public SpecialArgs Args;
    public LineFlags Flags { get; set; }
    public LineSpecial Special { get; private set; }
    public bool Activated { get; private set; }
    public LineDataTypes DataChanges { get; set; }
    public float Alpha { get; private set; }
    public bool DataChanged => DataChanges > 0;
    // Rendering hax...
    public bool Sky;
    public int BlockmapCount;
    public int BlockLinkCount;
    public int CheckBlockersCount;
    private double? m_length;

    public Vec2D StartPosition => Segment.Start;
    public Vec2D EndPosition => Segment.End;
    public bool OneSided => Back == null;
    public bool TwoSided => !OneSided;
    public bool HasSpecial => Special.LineSpecialType != ZDoomLineSpecialType.None;
    public bool HasSectorTag => SectorTag > 0;
    public int SectorTag => Args.Arg0;
    public int TagArg => Args.Arg0;
    public int SpeedArg => Args.Arg1;
    public int DelayArg => Args.Arg2;
    public int AmountArg => Args.Arg2;
    public bool SeenForAutomap => DataChanges.HasFlag(LineDataTypes.Automap);
    public IEnumerable<Sector> Sectors => Sides.Select(s => s.Sector);
    public IEnumerable<Side> Sides => GetSides();
    public IEnumerable<Vec2D> Vertices => GetVertices();

    public Line(int id, int mapId, Seg2D segment, Side front, Side? back, LineFlags flags, LineSpecial lineSpecial,
        SpecialArgs args)
    {
        Id = id;
        MapId = mapId;
        Segment = segment;
        Front = front;
        Back = back;
        Flags = flags;
        Special = lineSpecial;
        Args = args;
        Alpha = 1;

        front.Line = this;
        front.Sector.Lines.Add(this);

        if (back != null)
        {
            back.Line = this;
            back.Sector.Lines.Add(this);
        }
    }

    // Same as Segment.Length, but caches the value.
    public double GetLength()
    {
        if (m_length.HasValue)
            return m_length.Value;

        m_length = Segment.Length;
        return m_length.Value;
    }

    public LineModel ToLineModel(IWorld world)
    {
        LineModel lineModel = new()
        {
            Id = Id,
            DataChanges = (int)DataChanges,
        };

        if (DataChanges.HasFlag(LineDataTypes.Activated))
            lineModel.Activated = Activated;

        if (DataChanges.HasFlag(LineDataTypes.Texture))
        {
            if (Front.DataChanged)
                lineModel.Front = ToSideModel(world, Front);
            if (Back != null && Back.DataChanged)
                lineModel.Back = ToSideModel(world, Back);
        }

        if (DataChanges.HasFlag(LineDataTypes.Args))
            lineModel.Args = Args;

        if (DataChanges.HasFlag(LineDataTypes.Alpha))
            lineModel.Alpha = Alpha;

        return lineModel;
    }

    public void ApplyLineModel(IWorld world, LineModel lineModel)
    {
        DataChanges = (LineDataTypes)lineModel.DataChanges;
        if (DataChanges.HasFlag(LineDataTypes.Activated) && lineModel.Activated.HasValue)
            Activated = lineModel.Activated.Value;

        if (DataChanges.HasFlag(LineDataTypes.Texture))
        {
            if (lineModel.Front != null && lineModel.Front.DataChanges > 0)
                ApplySideModel(world, Front, lineModel.Front);
            if (Back != null && lineModel.Back != null && lineModel.Back.DataChanges > 0)
                ApplySideModel(world, Back, lineModel.Back);
        }

        if (DataChanges.HasFlag(LineDataTypes.Args) && lineModel.Args.HasValue)
            Args = lineModel.Args.Value;

        if (DataChanges.HasFlag(LineDataTypes.Alpha) && lineModel.Alpha.HasValue)
            Alpha = lineModel.Alpha.Value;
    }

    private static void ApplySideModel(IWorld world, Side side, SideModel sideModel)
    {
        var tx = world.TextureManager;
        side.DataChanges = (SideDataTypes)sideModel.DataChanges;
        if (side.DataChanges.HasFlag(SideDataTypes.UpperTexture))
        {
            if (sideModel.UpperTex != null)
                side.Upper.SetTexture(tx.GetTexture(sideModel.UpperTex, ResourceNamespace.Global).Index, SideDataTypes.UpperTexture);
            else if (sideModel.UpperTexture.HasValue)
                side.Upper.SetTexture(sideModel.UpperTexture.Value, SideDataTypes.UpperTexture);
        }

        if (side.DataChanges.HasFlag(SideDataTypes.MiddleTexture))
        {
            if(sideModel.MiddelTex != null)
                side.Middle.SetTexture(tx.GetTexture(sideModel.MiddelTex, ResourceNamespace.Global).Index, SideDataTypes.MiddleTexture);
            else if (sideModel.MiddleTexture.HasValue)
                side.Middle.SetTexture(sideModel.MiddleTexture.Value, SideDataTypes.MiddleTexture);
        }

        if (side.DataChanges.HasFlag(SideDataTypes.LowerTexture))
        {
            if(sideModel.LowerTex != null)
                side.Lower.SetTexture(tx.GetTexture(sideModel.LowerTex, ResourceNamespace.Global).Index, SideDataTypes.LowerTexture);
            else if (sideModel.LowerTexture.HasValue)
                side.Lower.SetTexture(sideModel.LowerTexture.Value, SideDataTypes.LowerTexture);
        }    
    }

    private static SideModel ToSideModel(IWorld world, Side side)
    {
        SideModel sideModel = new SideModel() { DataChanges = (int)side.DataChanges };
        if (side.DataChanges.HasFlag(SideDataTypes.UpperTexture))
            sideModel.UpperTex = world.TextureManager.GetTexture(side.Upper.TextureHandle).Name;
        if (side.DataChanges.HasFlag(SideDataTypes.MiddleTexture))
            sideModel.MiddelTex = world.TextureManager.GetTexture(side.Middle.TextureHandle).Name;
        if (side.DataChanges.HasFlag(SideDataTypes.LowerTexture))
            sideModel.LowerTex = world.TextureManager.GetTexture(side.Lower.TextureHandle).Name;

        return sideModel;
    }

    public void SetActivated(bool set)
    {
        Activated = set;
        DataChanges |= LineDataTypes.Activated;
    }

    public void SetAlpha(float alpha)
    {
        Alpha = alpha;
        DataChanges |= LineDataTypes.Alpha;
    }

    /// <summary>
    /// If the line blocks the given entity. Only checks line properties
    /// and flags. No sector checking.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity is blocked by this line, false
    /// otherwise.</returns>
    public bool BlocksEntity(Entity entity)
    {
        if (OneSided)
            return true;

        if (!entity.IsPlayer && !entity.Flags.Missile &&
            (Flags.Blocking.Monsters || (Flags.Blocking.LandMonsters && !entity.Flags.Float)))
            return true;

        if (entity.IsPlayer && Flags.Blocking.Players)
            return true;

        return false;
    }

    public void MarkSeenOnAutomap()
    {
        DataChanges |= LineDataTypes.Automap;
    }

    private IEnumerable<Vec2D> GetVertices()
    {
        yield return Segment.Start;
        yield return Segment.End;
}

    private IEnumerable<Side> GetSides()
    {
        yield return Front;
        if (Back != null)
            yield return Back;
    }

    public override string ToString()
    {
        return $"Id={Id} [{StartPosition}] [{EndPosition}]";
    }
}
