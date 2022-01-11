using System;
using Helion.Geometry.Vectors;
using Helion.Util;
using Helion.Util.Assertion;
using Helion.World.Physics;

namespace Helion.World.Entities;

// The enemy movement code is more or less line for line from idtech1
public partial class Entity
{
    private enum MoveDir
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

    private MoveDir m_direction = MoveDir.None;

    public bool BlockFloating;

    public bool ValidEnemyTarget(Entity? entity) => entity != null &&
        !entity.IsDead && (!IsFriend(entity) || Target == null);

    public bool SetNewTarget(bool allaround)
    {
        Entity? newTarget = null;
        if (Sector.SoundTarget != null && ValidEnemyTarget(Sector.SoundTarget))
        {
            if (Flags.Ambush)
            {
                // Ambush enemies will set target based on SoundTarget reguardless of FOV.
                if (EntityManager.World.CheckLineOfSight(this, Sector.SoundTarget))
                    newTarget = Sector.SoundTarget;
            }
            else
            {
                newTarget = Sector.SoundTarget;
            }
        }
        else if (ValidEnemyTarget(Target))
        {
            newTarget = Target;
        }
        else
        {
            newTarget = GetNewTarget(allaround);
        }

        if (newTarget != null)
        {
            Target = newTarget;
            if (!allaround)
            {
                SetSeeState();
                PlaySeeSound();
            }

            return true;
        }

        return false;
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
        // Dehacked can modify things into enemies that can move but this flag doesn't exist in the originalg game.
        // Set this flag for anything that tries to move, otherwise they can clip ito other things and get stuck, especialliy with float.
        Flags.CanPass = true;
        Assert.Precondition(Target != null, "Target is null");

        MoveDir[] dir = new MoveDir[2];
        MoveDir oldDirection = m_direction;
        MoveDir oppositeDirection = OppositeDirections[(int)m_direction];
        MoveDir tdir;

        double dx = Target!.Position.X - Position.X;
        double dy = Target!.Position.Y - Position.Y;

        if (dx > 10)
            dir[0] = MoveDir.East;
        else if (dx < -10)
            dir[0] = MoveDir.West;
        else
            dir[0] = MoveDir.None;

        if (dy < -10)
            dir[1] = MoveDir.South;
        else if (dy > 10)
            dir[1] = MoveDir.North;
        else
            dir[1] = MoveDir.None;

        if (dir[0] != MoveDir.None && dir[1] != MoveDir.None)
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
            tdir = dir[0];
            dir[0] = dir[1];
            dir[1] = tdir;
        }

        if (dir[0] == oppositeDirection)
            dir[0] = MoveDir.None;
        if (dir[1] == oppositeDirection)
            dir[1] = MoveDir.None;

        if (dir[0] != MoveDir.None)
        {
            m_direction = dir[0];
            if (TryWalk())
                return;  // either moved forward or attacked
        }

        if (dir[1] != MoveDir.None)
        {
            m_direction = dir[1];
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

        double speedX = SpeedX[(int)m_direction] * Properties.Speed;
        double speedY = SpeedY[(int)m_direction] * Properties.Speed;

        return (Position.X + speedX, Position.Y + speedY);
    }

    public bool MoveEnemy(out TryMoveData? tryMove)
    {
        if (m_direction == MoveDir.None || (!Flags.Float && !OnGround))
        {
            tryMove = null;
            return false;
        }

        Vec2D nextPos = GetNextEnemyPos();
        tryMove = World.TryMoveXY(this, nextPos);

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

        if (tryMove.Success)
        {
            if (!Flags.Float)
            {
                Vec3D newPos = (Position.X, Position.Y, tryMove.HighestFloorZ);
                SetPosition(newPos);
            }

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

        return tryMove.Success;
    }

    public double GetEnemyFloatMove()
    {
        if (IsPlayer || IsDead || Target == null || !Flags.Float || Flags.Skullfly || BlockFloating || OnGround)
            return 0.0;

        double distance = Position.ApproximateDistance2D(Target.Position);
        double dz = (Target.Position.Z - Position.Z + (Height / 2)) * 3;

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
        if (Target == null || IsFriend(Target) || !EntityManager.World.CheckLineOfSight(this, Target))
            return false;

        if (Flags.JustHit)
        {
            Flags.JustHit = false;
            return true;
        }

        if (ReactionTime > 0)
            return false;

        double distance = Position.ApproximateDistance2D(Target.Position);

        if (!HasMeleeState())
            distance -= 128;

        if (HasMeleeState() && distance < Definition.Properties.MeleeThreshold)
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
            if (tryMove != null && tryMove.ImpactSpecialLines.Count > 0)
            {
                for (int i = 0; i < tryMove.ImpactSpecialLines.Count; i++)
                    World.ActivateSpecialLine(this, tryMove.ImpactSpecialLines[i], ActivationContext.UseLine);
            }

            return false;
        }

        MoveCount = EntityManager.World.Random.NextByte() & 15;
        return true;
    }
}
