using Helion.Geometry.Vectors;
using Helion.Maps.Specials;

namespace Helion.Models;

public class EntityModel
{
    public const int MidTexEntityFlag = 1 << 31;
    public string Name = string.Empty;
    public int Id;
    public int ThingId;
    public double AngleRadians;
    public EntityBoxModel Box;
    public double SpawnPointX;
    public double SpawnPointY;
    public double SpawnPointZ;
    public double VelocityX;
    public double VelocityY;
    public double VelocityZ;
    public int Health;
    public int Armor;
    public string? ArmorDefinition;
    public int FrozenTics;
    public int MoveCount;
    public int Sector;
    public int? Owner;
    public int? Target;
    public int? Tracer;

    public bool Refire;
    public bool MoveLinked;
    public bool Respawn;

    public int MoveDir;
    public bool BlockFloat;
    public bool? IsBlood;

    public FrameStateModel Frame;
    public EntityFlagsModel Flags;
    public int Threshold;
    public int ReactionTime;

    public int? HighSec;
    public int? LowSec;
    public int? HighEntity;
    public int? LowEntity;
    // Previously was not serialized
    public bool? OnGround;
    public double Gravity = 1;
    public float? Alpha;
    public int? RenderStyle;
    public int? MaxTargetRange;
    public int? MinMissileChance;
    public int? MeleeThreshold;
    public SpecialArgs Args;
}
