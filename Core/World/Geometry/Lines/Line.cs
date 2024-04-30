using Helion.Geometry.Segments;
using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;
using Helion.Models;
using Helion.World.Entities;
using Helion.World.Geometry.Sides;
using Helion.World.Special;
using Helion.Geometry.Vectors;
using Helion.World.Geometry.Sectors;
using System.Collections.Generic;
using Helion.Resources;
using Helion.World.Bsp;
using System.Linq;
using Helion.Maps.Bsp.Geometry;
using Helion.World.Geometry.Islands;
using Helion.World.Blockmap;

namespace Helion.World.Geometry.Lines;

public class Line : IBspUsableLine
{
    public const int NoLineId = 0;

    public int Id { get; }
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
    // Rendering hax...
    public bool Sky;
    public int BlockmapCount;
    public int PhysicsCount;
    private double? m_length;
    public bool MarkAutomap;

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
    public bool SeenForAutomap => (DataChanges & LineDataTypes.Automap) != 0;
    public IEnumerable<Sector> Sectors => Sides.Select(s => s.Sector);
    public IEnumerable<Side> Sides => GetSides();
    public IEnumerable<Vec2D> Vertices => GetVertices();

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
                side.Upper.SetTexture(tx.GetTexture(sideModel.UpperTex, ResourceNamespace.Global).Index, SideDataTypes.UpperTexture);
            else if (sideModel.UpperTexture.HasValue)
                side.Upper.SetTexture(sideModel.UpperTexture.Value, SideDataTypes.UpperTexture);
        }

        if ((side.DataChanges & SideDataTypes.MiddleTexture) != 0)
        {
            if(sideModel.MiddelTex != null)
                side.Middle.SetTexture(tx.GetTexture(sideModel.MiddelTex, ResourceNamespace.Global).Index, SideDataTypes.MiddleTexture);
            else if (sideModel.MiddleTexture.HasValue)
                side.Middle.SetTexture(sideModel.MiddleTexture.Value, SideDataTypes.MiddleTexture);
        }

        if ((side.DataChanges & SideDataTypes.LowerTexture) != 0)
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
