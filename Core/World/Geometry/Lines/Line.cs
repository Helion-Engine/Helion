using Helion.Bsp.Geometry;
using Helion.Geometry.Segments;
using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;
using Helion.Models;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Sides;
using Helion.World.Special;
using Helion.Geometry.Vectors;

namespace Helion.World.Geometry.Lines;

public class Line : IBspUsableLine
{
    public const int NoLineId = 0;

    public int Id { get; }
    public readonly int MapId;
    public readonly Seg2D Segment;
    public readonly Side Front;
    public readonly Side? Back;
    public readonly Side[] Sides;
    public int LineId { get; set; }
    public SpecialArgs Args;
    public LineFlags Flags { get; set; }
    public LineSpecial Special { get; set; }
    public bool Activated { get; private set; }
    public LineDataTypes DataChanges { get; set; }
    public float Alpha { get; private set; }
    public bool DataChanged => DataChanges > 0;
    // Rendering hax...
    public bool Sky;

    public Vec2D StartPosition => Segment.Start;
    public Vec2D EndPosition => Segment.End;

    public bool OneSided => Back == null;
    public bool TwoSided => !OneSided;
    public bool HasSpecial => Special.LineSpecialType != ZDoomLineSpecialType.None;
    public bool HasSectorTag => SectorTag > 0;

    // TODO: Any way we can encapsulate this somehow?
    public int SectorTag => Args.Arg0;
    public int TagArg => Args.Arg0;
    public int SpeedArg => Args.Arg1;
    public int DelayArg => Args.Arg2;
    public int AmountArg => Args.Arg2;
    public bool SeenForAutomap => DataChanges.HasFlag(LineDataTypes.Automap);

    public Line(int id, int mapId, Seg2D segment, Side front, Side? back, LineFlags flags, LineSpecial lineSpecial,
        SpecialArgs args)
    {
        Id = id;
        MapId = mapId;
        Segment = segment;
        Front = front;
        Back = back;
        Sides = (back == null ? new[] { front } : new[] { front, back });
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

    public LineModel ToLineModel()
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
                lineModel.Front = ToSideModel(Front);
            if (Back != null && Back.DataChanged)
                lineModel.Back = ToSideModel(Back);
        }

        if (DataChanges.HasFlag(LineDataTypes.Args))
            lineModel.Args = Args;

        if (DataChanges.HasFlag(LineDataTypes.Alpha))
            lineModel.Alpha = Alpha;

        return lineModel;
    }

    public void ApplyLineModel(LineModel lineModel)
    {
        DataChanges = (LineDataTypes)lineModel.DataChanges;
        if (DataChanges.HasFlag(LineDataTypes.Activated) && lineModel.Activated.HasValue)
            Activated = lineModel.Activated.Value;

        if (DataChanges.HasFlag(LineDataTypes.Texture))
        {
            if (lineModel.Front != null && lineModel.Front.DataChanges > 0)
                ApplySideModel(Front, lineModel.Front);
            if (Back != null && lineModel.Back != null && lineModel.Back.DataChanges > 0)
                ApplySideModel(Back, lineModel.Back);
        }

        if (DataChanges.HasFlag(LineDataTypes.Args) && lineModel.Args.HasValue)
            Args = lineModel.Args.Value;

        if (DataChanges.HasFlag(LineDataTypes.Alpha) && lineModel.Alpha.HasValue)
            Alpha = lineModel.Alpha.Value;
    }

    private static void ApplySideModel(Side side, SideModel sideModel)
    {
        side.DataChanges = (SideDataTypes)sideModel.DataChanges;

        if (side.DataChanges.HasFlag(SideDataTypes.UpperTexture) && sideModel.UpperTexture.HasValue)
            side.Upper.SetTexture(sideModel.UpperTexture.Value, SideDataTypes.UpperTexture);
        if (side.DataChanges.HasFlag(SideDataTypes.MiddleTexture) && sideModel.MiddleTexture.HasValue)
            side.Middle.SetTexture(sideModel.MiddleTexture.Value, SideDataTypes.MiddleTexture);
        if (side.DataChanges.HasFlag(SideDataTypes.LowerTexture) && sideModel.LowerTexture.HasValue)
            side.Lower.SetTexture(sideModel.LowerTexture.Value, SideDataTypes.LowerTexture);
    }

    private static SideModel ToSideModel(Side side)
    {
        SideModel sideModel = new SideModel() { DataChanges = (int)side.DataChanges };
        if (side.DataChanges.HasFlag(SideDataTypes.UpperTexture))
            sideModel.UpperTexture = side.Upper.TextureHandle;
        if (side.DataChanges.HasFlag(SideDataTypes.MiddleTexture))
            sideModel.MiddleTexture = side.Middle.TextureHandle;
        if (side.DataChanges.HasFlag(SideDataTypes.LowerTexture))
            sideModel.LowerTexture = side.Lower.TextureHandle;

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

        return (!entity.IsPlayer && Flags.Blocking.Monsters && !entity.Flags.Missile) || (entity.IsPlayer && Flags.Blocking.Players);
    }

    public void MarkSeenOnAutomap()
    {
        DataChanges |= LineDataTypes.Automap;
    }

    public override string ToString()
    {
        return $"Id={Id} [{StartPosition}] [{EndPosition}]";
    }
}

