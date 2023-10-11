using System;
using Helion.Geometry.Vectors;
using Helion.Util;
using Helion.Util.Assertion;
using Helion.World.Physics;

namespace Helion.World.Entities;

// The enemy movement code is more or less line for line from idtech1
public partial class Entity
{
    public enum MoveDir
    {
        East,
        NorthEast,
        North,
        NorthWest,
        West,
        SouthWest,
        South,
        SouthEast,
        None
    }

    private static readonly double[] MoveAngles = new[]
    {
        0.0,
        MathHelper.QuarterPi,
        MathHelper.HalfPi,
        MathHelper.HalfPi + MathHelper.QuarterPi,
        MathHelper.Pi,
        MathHelper.Pi + MathHelper.QuarterPi,
        MathHelper.Pi + MathHelper.HalfPi,
        MathHelper.Pi + MathHelper.HalfPi + MathHelper.QuarterPi,
        0.0
    };

    private static readonly MoveDir[] OppositeDirections = new[]
    {
        MoveDir.West, MoveDir.SouthWest, MoveDir.South, MoveDir.SouthEast,
        MoveDir.East, MoveDir.NorthEast, MoveDir.North, MoveDir.NorthWest, MoveDir.None
    };

    private static readonly MoveDir[] Diagnals = new[] { MoveDir.NorthWest, MoveDir.NorthEast, MoveDir.SouthWest, MoveDir.SouthEast };

    private static readonly double[] SpeedX = new[] { 1.0, Speed, 0, -Speed, -1.0, -Speed, 0, Speed };
    private static readonly double[] SpeedY = new[] { 0, Speed, 1.0, Speed, 0, -Speed, -1.0, -Speed };

    private static ushort ClosetChaseCount;
    private static ushort ClosetLookCount;
    private static ushort ChaseLoop;

    private MoveDir m_direction = MoveDir.None;

    public bool BlockFloating;
    public bool IsClosetLook => FrameState.Frame.MasterFrameIndex == World.ArchiveCollection.EntityFrameTable.ClosetLookFrameIndex;
    public bool IsClosetChase => FrameState.Frame.MasterFrameIndex == World.ArchiveCollection.EntityFrameTable.ClosetChaseFrameIndex;

    public void SetEnemyDirection(MoveDir direction) =>
        m_direction = direction;

    public bool ValidEnemyTarget(Entity? entity) => entity != null &&
        !entity.IsDead && (!IsFriend(entity) || Target.Entity == null);

    public bool SetNewTarget(bool allaround)
    {
        if (IsFrozen)
            return false;

        Entity? newTarget = null;
        if (Sector.SoundTarget.Entity != null && ValidEnemyTarget(Sector.SoundTarget.Entity))
        {
            if (Flags.Ambush)
            {
                // Ambush enemies will set target based on SoundTarget reguardless of FOV.
                if (EntityManager.World.CheckLineOfSight(this, Sector.SoundTarget.Entity))
                    newTarget = Sector.SoundTarget.Entity;
            }
            else
            {
                newTarget = Sector.SoundTarget.Entity;
            }
        }
        else if (ValidEnemyTarget(Target.Entity))
        {
            newTarget = Target.Entity;
        }
        else
        {
            newTarget = GetNewTarget(allaround);
        }

        if (newTarget != null)
        {
            SetTarget(newTarget);
            if (!allaround)
            {
                SetSeeState();
                PlaySeeSound();
            }

            return true;
        }

        return false;
    }

    public void SetClosetLook()
    {
        FrameState.SetFrameIndex(World.ArchiveCollection.EntityFrameTable.ClosetLookFrameIndex);
        AddFrameTicks(ClosetLookCount);
        ClosetLookCount++;
    }

    public void ClearClosetLook()
    {
        SetSpawnState();
    }

    public void SetClosetChase()
    {
        FrameState.SetFrameIndex(World.ArchiveCollection.EntityFrameTable.ClosetChaseFrameIndex);
        AddFrameTicks(ClosetChaseCount);
        ClosetChaseCount++;
    }

    private void AddFrameTicks(int ticks)
    {
        // Distribute the calls across game ticks
        int frameTicks = FrameState.Frame.Ticks;
        if (frameTicks == 0)
            return;
        FrameState.SetTics((frameTicks + ticks) % frameTicks);
    }

    public void Teleported()
    {
        InMonsterCloset = false;
        if (IsClosetChase)
            SetSeeState();
        if (IsClosetLook)
            SetSpawnState();
    }

    private Entity? GetNewTarget(bool allaround)
    {
        Entity? newTarget;
        if (Flags.Friendly)
        {
            newTarget = EntityManager.World.GetLineOfSightEnemy(this, allaround);
            if (newTarget == null)
                newTarget = EntityManager.World.GetLineOfSightPlayer(this, allaround);
        }
        else
        {
            newTarget = EntityManager.World.GetLineOfSightPlayer(this, allaround);
        }

        return newTarget;
    }

    public void SetNewChaseDirection()
    {
        // All monsters normally have CanPass set.
        // Dehacked can modify things into enemies that can move but this flag doesn't exist in the original game.
        // Set this flag for anything that tries to move, otherwise they can clip ito other things and get stuck, especialliy with float.
        Flags.CanPass = true;
        Assert.Precondition(Target.Entity != null, "Target is null");

        MoveDir dir0;
        MoveDir dir1;
        MoveDir oldDirection = m_direction;
        MoveDir oppositeDirection = OppositeDirections[(int)m_direction];
        MoveDir tdir;

        double dx = Target.Entity!.Position.X - Position.X;
        double dy = Target.Entity!.Position.Y - Position.Y;

        if (dx > 10)
            dir0 = MoveDir.East;
        else if (dx < -10)
            dir0 = MoveDir.West;
        else
            dir0 = MoveDir.None;

        if (dy < -10)
            dir1 = MoveDir.South;
        else if (dy > 10)
            dir1 = MoveDir.North;
        else
            dir1 = MoveDir.None;

        if (dir0 != MoveDir.None && dir1 != MoveDir.None)
        {
            int index = 0;
            if (dy < 0)
                index += 2;
            if (dx > 0)
                index++;
            m_direction = Diagnals[index];
            if (m_direction != oppositeDirection && TryWalk())
                return;
        }

        if (EntityStatic.Random.NextByte() > 200 || Math.Abs(dy) > Math.Abs(dx))
        {
            tdir = dir0;
            dir0 = dir1;
            dir1 = tdir;
        }

        if (dir0 == oppositeDirection)
            dir0 = MoveDir.None;
        if (dir1 == oppositeDirection)
            dir1 = MoveDir.None;

        if (dir0 != MoveDir.None)
        {
            m_direction = dir0;
            if (TryWalk())
                return;  // either moved forward or attacked
        }

        if (dir1 != MoveDir.None)
        {
            m_direction = dir1;
            if (TryWalk())
                return;
        }

        // there is no direct path to the player, so pick another direction.
        if (oldDirection != MoveDir.None)
        {
            m_direction = oldDirection;
            if (TryWalk())
                return;
        }

        if (SlowTickMultiplier <= 1)
        {
            // randomly determine direction of search
            if ((EntityStatic.Random.NextByte() & 1) != 0)
            {
                for (tdir = MoveDir.East; tdir <= MoveDir.SouthEast; tdir++)
                {
                    if (tdir != oppositeDirection)
                    {
                        m_direction = tdir;
                        if (TryWalk())
                            return;
                    }
                }
            }
            else
            {
                for (tdir = MoveDir.SouthEast; tdir != (MoveDir.East - 1); tdir--)
                {
                    if (tdir != oppositeDirection)
                    {
                        m_direction = tdir;
                        if (TryWalk())
                            return;
                    }
                }
            }
        }
        else if ((ChaseLoop++ % 4) > 0)
        {
            // Do not run this nearly as often, randomize start search directions and limit to 3
            int random = EntityStatic.Random.NextByte();
            int addDir = -1;
            tdir = MoveDir.SouthEast - (random % 4);

            if ((random & 1) != 0)
            {
                tdir = MoveDir.East + (random % 4);
                addDir = 1;
            }

            if (tdir != oppositeDirection && tdir >= MoveDir.East && tdir <= MoveDir.SouthEast)
            {
                m_direction = tdir;
                if (TryWalk())
                    return;
            }

            tdir += addDir;
            if (tdir != oppositeDirection && tdir >= MoveDir.East && tdir <= MoveDir.SouthEast)
            {
                m_direction = tdir;
                if (TryWalk())
                    return;
            }

            tdir += addDir;
            if (tdir != oppositeDirection && tdir >= MoveDir.East && tdir <= MoveDir.SouthEast)
            {
                m_direction = tdir;
                if (TryWalk())
                    return;
            }
        }

        if (oppositeDirection != MoveDir.None)
        {
            m_direction = oppositeDirection;
            if (TryWalk())
                return;
        }

        if (MoveCount < 0 && SlowTickMultiplier > 1)
            MoveCount = EntityStatic.Random.NextByte() & 15;

        m_direction = MoveDir.None;
    }

    public Vec2D GetNextEnemyPos()
    {
        if (m_direction == MoveDir.None || (!Flags.Float && !OnGround))
            return Position.XY;

        double speed = IsClosetChase ? 64 : Math.Clamp(Properties.MonsterMovementSpeed * SlowTickMultiplier, -128, 128);
        double speedX = SpeedX[(int)m_direction] * speed;
        double speedY = SpeedY[(int)m_direction] * speed;

        return (Position.X + speedX, Position.Y + speedY);
    }

    public bool MoveEnemy(out TryMoveData? tryMove)
    {
        if (m_direction == MoveDir.None || (!Flags.Float && !OnGround) || IsFrozen)
        {
            tryMove = null;
            return false;
        }

        Vec2D nextPos = GetNextEnemyPos();
        bool isMoving = Position.XY != nextPos;
        tryMove = World.TryMoveXY(this, nextPos);
        if (Flags.Teleport)
            return true;

        if (!tryMove.Success && Flags.Float && tryMove.CanFloat)
        {
            BlockFloating = true;
            Position = (Position.X, Position.Y, Position.Z + (Position.Z < tryMove.HighestFloorZ ? FloatSpeed : -FloatSpeed));
            return true;
        }
        else
        {
            BlockFloating = false;
        }

        if (tryMove.Success && !Flags.Float && isMoving)
            Position.Z = tryMove.HighestFloorZ;

        // With increased speeds using the TickMultiplier TryMove will iterate and can have partial successes.
        // A partial success needs be considered true in this case.
        return Position.X != PrevPosition.X || Position.Y != PrevPosition.Y || tryMove.Success;
    }

    public void TurnTowardsMovementDirection()
    {
        if (m_direction == MoveDir.None)
            return;

        AngleRadians = MathHelper.GetPositiveAngle(AngleRadians - (AngleRadians % MathHelper.QuarterPi));
        double delta = AngleRadians - MoveAngles[(int)m_direction];
        if (delta != 0)
        {
            if (Math.Abs(delta) > MathHelper.Pi)
                delta = -delta;
            if (delta > 0)
                AngleRadians -= MathHelper.QuarterPi;
            else if (delta < 0)
                AngleRadians += MathHelper.QuarterPi;
        }
    }

    public void SetToMovementDirection()
    {
        if (m_direction != MoveDir.None)
            AngleRadians = MoveAngles[(int)m_direction];
    }

    public double GetEnemyFloatMove()
    {
        if (IsPlayer || IsDead || Target.Entity == null || !Flags.Float || Flags.Skullfly || BlockFloating || OnGround)
            return 0.0;

        double distance = Position.ApproximateDistance2D(Target.Entity.Position);
        double dz = (Target.Entity.Position.Z - Position.Z + (Height / 2)) * 3;

        if (dz < 0 && distance < -dz)
            return -FloatSpeed;
        else if (dz > 0 && distance < dz)
            return FloatSpeed;

        return 0.0;
    }

    public bool InMeleeRange(Entity? entity, double range = -1)
    {
        if (entity == null)
            return false;

        if (range == -1)
            range = Properties.MeleeRange;

        if (range == 0 || IsFriend(entity))
            return false;

        double distance = Position.ApproximateDistance2D(entity.Position);

        if (distance >= range + entity.Radius)
            return false;

        if (!Flags.NoVerticalMeleeRange && (entity.Position.Z > Position.Z + Height || entity.Position.Z + entity.Height < Position.Z))
            return false;

        if (!EntityManager.World.CheckLineOfSight(this, entity))
            return false;

        return true;
    }

    public bool CheckMissileRange()
    {
        if (Target.Entity == null || IsFriend(Target.Entity) || !EntityManager.World.CheckLineOfSight(this, Target.Entity))
            return false;

        if (Flags.JustHit)
        {
            Flags.JustHit = false;
            return true;
        }

        if (ReactionTime > 0)
            return false;

        double distance = Position.ApproximateDistance2D(Target.Entity.Position);

        if (Definition.MissileState == null)
            distance -= 128;

        if (Definition.MeleeState != null && distance < Definition.Properties.MeleeThreshold)
            return false;

        if (Definition.Flags.MissileMore)
            distance /= 2;
        if (Definition.Flags.MissileEvenMore)
            distance /= 8;

        if (Definition.Properties.MaxTargetRange > 0 && distance > Definition.Properties.MaxTargetRange)
            return false;

        if (SlowTickMultiplier > 0)
            distance /= SlowTickMultiplier;

        distance = Math.Min(distance, Definition.Properties.MinMissileChance);
        return EntityStatic.Random.NextByte() >= distance;
    }

    private bool TryWalk()
    {
        if (!MoveEnemy(out TryMoveData? tryMove))
        {
            if (tryMove != null && tryMove.ImpactSpecialLines.Length > 0)
            {
                for (int i = 0; i < tryMove.ImpactSpecialLines.Length; i++)
                    World.ActivateSpecialLine(this, tryMove.ImpactSpecialLines[i], ActivationContext.UseLine);
            }

            return false;
        }

        MoveCount = EntityStatic.Random.NextByte() & 15;
        return true;
    }
}
