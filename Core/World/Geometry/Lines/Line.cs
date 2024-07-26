using Helion.Geometry.Segments;
using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;
using Helion.Models;
using Helion.World.Entities;
using Helion.World.Geometry.Sides;
using Helion.World.Special;
using Helion.Geometry.Vectors;
using Helion.Resources;
using Helion.World.Geometry.Walls;

namespace Helion.World.Geometry.Lines;

public sealed class Line
{
    public const int NoLineId = 0;

    public int Id;
    public Seg2D Segment;
    public Side Front;
    public Side? Back;
    public int LineId;
    public SpecialArgs Args;
    public LineFlags Flags;
    public LineSpecial Special;
    public bool Activated;
    public LineDataTypes DataChanges;
    public float Alpha;
    public bool DataChanged => DataChanges > 0;
    public int BlockmapCount;
    public int PhysicsCount;
    private double? m_length;

    public Vec2D StartPosition => Segment.Start;
    public Vec2D EndPosition => Segment.End;
    public bool HasSpecial => Special.LineSpecialType != ZDoomLineSpecialType.None;
    public bool HasSectorTag => SectorTag > 0;
    public int SectorTag => Args.Arg0;
    public int TagArg => Args.Arg0;
    public int SpeedArg => Args.Arg1;
    public int DelayArg => Args.Arg2;
    public int AmountArg => Args.Arg2;
    public bool SeenForAutomap => (DataChanges & LineDataTypes.Automap) != 0;

    public Line(int id, Seg2D segment, Side front, Side? back, LineFlags flags, LineSpecial lineSpecial,
        SpecialArgs args)
    {
        Id = id;
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

    public void Reset()
    {
        Alpha = 1;
        Activated = default;
        DataChanges = default;
        BlockmapCount = default;
        PhysicsCount = default;
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

        if ((DataChanges & LineDataTypes.Activated) != 0)
            lineModel.Activated = Activated;

        if ((DataChanges & LineDataTypes.Texture) != 0)
        {
            if (Front.DataChanged)
                lineModel.Front = ToSideModel(world, Front);
            if (Back != null && Back.DataChanged)
                lineModel.Back = ToSideModel(world, Back);
        }

        if ((DataChanges & LineDataTypes.Args) != 0)
            lineModel.Args = Args;

        if ((DataChanges & LineDataTypes.Alpha) != 0)
            lineModel.Alpha = Alpha;

        return lineModel;
    }

    public void ApplyLineModel(IWorld world, LineModel lineModel)
    {
        DataChanges = (LineDataTypes)lineModel.DataChanges;
        if ((DataChanges & LineDataTypes.Activated) != 0 && lineModel.Activated.HasValue)
            Activated = lineModel.Activated.Value;

        if ((DataChanges & LineDataTypes.Texture) != 0)
        {
            if (lineModel.Front != null && lineModel.Front.DataChanges > 0)
                ApplySideModel(world, Front, lineModel.Front);
            if (Back != null && lineModel.Back != null && lineModel.Back.DataChanges > 0)
                ApplySideModel(world, Back, lineModel.Back);
        }

        if ((DataChanges & LineDataTypes.Args) != 0 && lineModel.Args.HasValue)
            Args = lineModel.Args.Value;

        if ((DataChanges & LineDataTypes.Alpha) != 0 && lineModel.Alpha.HasValue)
            Alpha = lineModel.Alpha.Value;
    }

    private static void ApplySideModel(IWorld world, Side side, SideModel sideModel)
    {
        var tx = world.TextureManager;
        side.DataChanges = (SideDataTypes)sideModel.DataChanges;
        if ((side.DataChanges & SideDataTypes.UpperTexture) != 0)
        {
            if (sideModel.UpperTex != null)
                side.SetWallTexture(tx.GetTexture(sideModel.UpperTex, ResourceNamespace.Global).Index, WallLocation.Upper);
            else if (sideModel.UpperTexture.HasValue)
                side.SetWallTexture(sideModel.UpperTexture.Value, WallLocation.Upper);
        }

        if ((side.DataChanges & SideDataTypes.MiddleTexture) != 0)
        {
            if(sideModel.MiddelTex != null)
                side.SetWallTexture(tx.GetTexture(sideModel.MiddelTex, ResourceNamespace.Global).Index, WallLocation.Middle);
            else if (sideModel.MiddleTexture.HasValue)
                side.SetWallTexture(sideModel.MiddleTexture.Value, WallLocation.Middle);
        }

        if ((side.DataChanges & SideDataTypes.LowerTexture) != 0)
        {
            if(sideModel.LowerTex != null)
                side.SetWallTexture(tx.GetTexture(sideModel.LowerTex, ResourceNamespace.Global).Index, WallLocation.Lower);
            else if (sideModel.LowerTexture.HasValue)
                side.SetWallTexture(sideModel.LowerTexture.Value, WallLocation.Lower);
        }    
    }

    private static SideModel ToSideModel(IWorld world, Side side)
    {
        SideModel sideModel = new SideModel() { DataChanges = (int)side.DataChanges };
        if ((side.DataChanges & SideDataTypes.UpperTexture) != 0)
            sideModel.UpperTex = world.TextureManager.GetTexture(side.Upper.TextureHandle).Name;
        if ((side.DataChanges & SideDataTypes.MiddleTexture) != 0)
            sideModel.MiddelTex = world.TextureManager.GetTexture(side.Middle.TextureHandle).Name;
        if ((side.DataChanges & SideDataTypes.LowerTexture) != 0)
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

    public static bool BlocksEntity(Entity entity, bool oneSided, in LineFlags flags, bool mbf21)
    {
        if (oneSided)
            return true;

        if (!entity.IsPlayer && !entity.Flags.Missile &&
            (flags.Blocking.Monsters || (mbf21 && flags.Blocking.LandMonstersMbf21 && !entity.Flags.Float)))
            return true;

        if (entity.IsPlayer && (flags.Blocking.Players || (mbf21 && flags.Blocking.PlayersMbf21)))
            return true;

        return false;
    }

    public override string ToString()
    {
        return $"Id={Id} [{StartPosition}] [{EndPosition}]";
    }
}
