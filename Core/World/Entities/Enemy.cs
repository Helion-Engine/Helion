using System;
using Helion.Geometry.Vectors;
using Helion.Resources.Archives.Entries;
using Helion.Util;
using Helion.Util.Assertion;
using Helion.World.Entities.Definition.States;
using Helion.World.Geometry.Lines;
using Helion.World.Physics;
using Helion.World.Special;

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

    private static int ClosetChaseCount;
    private static int ClosetLookCount;

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

    public void ClosetLook()
    {
        if (Sector.SoundTarget.Entity != null && ValidEnemyTarget(Sector.SoundTarget.Entity))
        {
            SetTarget(Sector.SoundTarget.Entity);
            SetClosetChase();
        }
    }

    public void ClosetChase()
    {
        if (Target.Entity.IsDead)
            return;

        SetNewChaseDirection();
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

        if (EntityManager.World.Random.NextByte() > 200 || Math.Abs(dy) > Math.Abs(dx))
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

        // randomly determine direction of search
        if ((EntityManager.World.Random.NextByte() & 1) != 0)
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

        if (oppositeDirection != MoveDir.None)
        {
            m_direction = oppositeDirection;
            if (TryWalk())
                return;
        }

        m_direction = MoveDir.None;
    }

    public Vec2D GetNextEnemyPos()
    {
        if (m_direction == MoveDir.None || (!Flags.Float && !OnGround))
            return Position.XY;

        double speed = IsClosetChase ? 64 : Properties.MonsterMovementSpeed;
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
            Vec3D pos = (Position.X, Position.Y, Position.Z + (Position.Z < tryMove.HighestFloorZ ? FloatSpeed : -FloatSpeed));
            SetPosition(pos);
            return true;
        }
        else
        {
            BlockFloating = false;
        }

        if (tryMove.Success && !Flags.Float && isMoving)
            SetZ(tryMove.HighestFloorZ);

        return tryMove.Success;
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

        distance = Math.Min(distance, Definition.Properties.MinMissileChance);
        return World.Random.NextByte() >= distance;
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

        MoveCount = EntityManager.World.Random.NextByte() & 15;
        return true;
    }
}
