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
    public ObjectHealth ObjectHealth = ObjectHealth.Default;
    public bool Activated => (DataChanges & LineDataTypes.Activated) != 0;
    public LineDataTypes DataChanges;
    public float Alpha;
    public ZDoomKeyType LockNumber;
    public bool DataChanged => DataChanges > 0;
    public int BlockmapCount;
    public int PhysicsCount;
    public string? MusicChangeFront;
    public string? MusicChangeBack;
    private double m_length;
    private double m_angle;

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

        m_length = -1;
        m_angle = double.MinValue;
    }

    public void Reset()
    {
        Alpha = 1;
        DataChanges = default;
        BlockmapCount = default;
        PhysicsCount = default;

        if (ObjectHealth != ObjectHealth.Default)
            ObjectHealth.Health = ObjectHealth.OriginalHealth;
    }

    // Same as Segment.Length, but caches the value.
    public double GetLength()
    {
        if (m_length != -1)
            return m_length;

        m_length = Segment.Length();
        return m_length;
    }

    // Same as Segment.Start.Angle(Segment.End), but caches the value.
    public double GetAngle()
    {
        if (m_angle != double.MinValue)
            return m_angle;

        m_angle = Segment.Start.Angle(Segment.End);
        return m_angle;
    }

    public LineModel ToLineModel(IWorld world)
    {
        LineModel lineModel = new()
        {
            Id = Id,
            DataChanges = (int)DataChanges,
        };

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

    public void ApplyLineModel(IWorld world, in LineModel lineModel)
    {
        DataChanges = (LineDataTypes)lineModel.DataChanges;

        if ((DataChanges & LineDataTypes.Texture) != 0)
        {
            if (lineModel.Front != null && lineModel.Front.Value.DataChanges > 0)
                ApplySideModel(world, Front, lineModel.Front.Value);
            if (Back != null && lineModel.Back != null && lineModel.Back.Value.DataChanges > 0)
                ApplySideModel(world, Back, lineModel.Back.Value);
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
                side.SetWallTexture(tx.GetTexture(sideModel.UpperTex, ResourceNamespace.Global, ResourceNamespace.Textures).Index, WallLocation.Upper);
            else if (sideModel.UpperTexture.HasValue)
                side.SetWallTexture(sideModel.UpperTexture.Value, WallLocation.Upper);
        }

        if ((side.DataChanges & SideDataTypes.MiddleTexture) != 0)
        {
            if(sideModel.MiddelTex != null)
                side.SetWallTexture(tx.GetTexture(sideModel.MiddelTex, ResourceNamespace.Global, ResourceNamespace.Textures).Index, WallLocation.Middle);
            else if (sideModel.MiddleTexture.HasValue)
                side.SetWallTexture(sideModel.MiddleTexture.Value, WallLocation.Middle);
        }

        if ((side.DataChanges & SideDataTypes.LowerTexture) != 0)
        {
            if(sideModel.LowerTex != null)
                side.SetWallTexture(tx.GetTexture(sideModel.LowerTex, ResourceNamespace.Global, ResourceNamespace.Textures).Index, WallLocation.Lower);
            else if (sideModel.LowerTexture.HasValue)
                side.SetWallTexture(sideModel.LowerTexture.Value, WallLocation.Lower);
        }    
    }

    private static SideModel ToSideModel(IWorld world, Side side)
    {
        var sideModel = new SideModel() { DataChanges = (int)side.DataChanges };
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
        if (set)
            DataChanges |= LineDataTypes.Activated;
        else
            DataChanges &= ~LineDataTypes.Activated;
    }

    public void SetAlpha(float alpha)
    {
        Alpha = alpha;
        DataChanges |= LineDataTypes.Alpha;
    }

    public static bool CanMoveOutOf(Entity entity, double x, double y, in Seg2D seg, bool oneSided)
    {
        if (entity.PlayerObj == null || entity.PlayerObj.IsVooDooDoll)
            return false;

        // Boom appears to check if the player was previously clipped with the line
        // If the player is moving out of the line then do not count the line as blocking. Boom things...
        if (!seg.Intersects(entity.Position.X - entity.Radius, entity.Position.Y - entity.Radius,
            entity.Position.X + entity.Radius, entity.Position.Y + entity.Radius))
        {
            return false;
        }

        if (!oneSided)
            return true;

        var newPos = new Vec2D(x, y);
        if (!seg.OnRight(newPos))
            return false;

        var oldPos = entity.Position.XY;
        var linePoint = seg.FromTime(seg.ToTime(oldPos));
        return linePoint.DistanceSquared(oldPos) <= linePoint.DistanceSquared(newPos);        
    }

    public static bool BlocksEntity(Entity entity, double x, double y, in Seg2D seg, bool oneSided, in LineBlockFlags blockFlags, bool mbf21)
    {
        if (oneSided || blockFlags.Everything)
            return !CanMoveOutOf(entity, x, y, seg, oneSided);

        if (entity.Flags.Missile && blockFlags.Projectiles)
            return true;

        bool isPlayerOrFriendly = entity.IsPlayer || entity.Flags.Friendly;
        if (!isPlayerOrFriendly && !entity.Flags.Missile &&
            (blockFlags.Monsters || (mbf21 && blockFlags.LandMonstersMbf21 && !entity.Flags.Float) || (blockFlags.LandMonsters && !entity.Flags.Float) || (blockFlags.FloatMonsters && entity.Flags.Float)))
            return true;

        if (entity.IsPlayer && (blockFlags.Players || (mbf21 && blockFlags.PlayersMbf21)))
            return true;

        return false;
    }

    public override string ToString()
    {
        return $"Id={Id} [{StartPosition}] [{EndPosition}]";
    }
}
