using System;
using Helion.Geometry.Vectors;
using Helion.Resources.Archives.Entries;
using Helion.Util;
using Helion.Util.Assertion;
using Helion.World.Entities.Definition.Flags;
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

    private static readonly double[] Speeds = { 1.0, Speed, 0, -Speed, -1.0, -Speed, 0, Speed, 
        0, Speed, 1.0, Speed, 0, -Speed, -1.0, -Speed };

    public static ushort ClosetChaseCount;
    public static ushort ClosetLookCount;
    public static ushort ChaseLoop;
    public static ushort ChaseFailureCount;

    private MoveDir m_direction = MoveDir.None;

    public double MonsterMovementSpeed;

    public void SetEnemyDirection(MoveDir direction) =>
        m_direction = direction;

    public bool ValidEnemyTarget(Entity? entity) => entity != null &&
        !entity.IsDead && (!IsFriend(entity) || Target() == null);

    public void SetMoveDirection(MoveDir dir) => m_direction = dir;

    public bool SetNewTarget(bool allAround)
    {
        if (IsFrozen || Flags.Dormant)
            return false;

        Entity? newTarget = null;
        var soundTarget = Sector.SoundTarget.Get();
        if (soundTarget != null && ValidEnemyTarget(soundTarget))
        {
            if (Flags.Ambush)
            {
                // Ambush enemies will set target based on SoundTarget reguardless of FOV.
                if (WorldStatic.World.CheckLineOfSight(this, soundTarget))
                    newTarget = soundTarget;
            }
            else
            {
                newTarget = soundTarget;
            }
        }
        else
        {
            newTarget = GetNewTarget(allAround);
        }

        if (newTarget != null)
        {
            if (Flags.Friendly)
            {
                var previousTarget = Target();
                SetTarget(newTarget);
                if (newTarget != null && newTarget.IsPlayer && newTarget != previousTarget && Definition.MissileState.HasValue)
                {
                    SetSeeState();
                    PlaySeeSound();
                    Flags.JustHit = false;
                    return true;
                }
            }
            else
            {
                SetTarget(newTarget);
            }

            if (!allAround)
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
        FrameState.SetFrameIndex(this, WorldStatic.World.ArchiveCollection.EntityFrameTable.ClosetLookFrameIndex);
        AddFrameTicks(ClosetLookCount);
        ClosetLookCount++;
    }

    public void ClearClosetLook()
    {
        SetSpawnState();
    }

    public void SetClosetChase()
    {
        ClosetFlags = ClosetFlags & ~ClosetFlags.ClosetLook;
        FrameState.SetFrameIndex(this, WorldStatic.World.ArchiveCollection.EntityFrameTable.ClosetChaseFrameIndex);
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

    public void ClearMonsterCloset()
    {
        if (ClosetFlags == ClosetFlags.None)
            return;

        var flags = ClosetFlags;
        ClosetFlags = ClosetFlags.None;

        if ((flags & ClosetFlags.ClosetLook) != 0)
            SetSpawnState();
        if ((flags & ClosetFlags.ClosetChase) != 0)
            SetSeeState();
    }

    private Entity? GetNewTarget(bool allAround)
    {
        Entity? newTarget;
        if (Flags.Friendly)
        {
            newTarget = WorldStatic.World.GetLineOfSightEnemy(this, allAround);
            newTarget ??= WorldStatic.World.GetLineOfSightPlayer(this, allAround);
            newTarget ??= WorldStatic.World.GetFirstAlivePlayer();
        }
        else
        {
            newTarget = WorldStatic.World.GetLineOfSightPlayer(this, allAround);
        }

        return newTarget;
    }

    public void SetNewChaseDirection()
    {
        if (--ChaseFailureSkipCount > 0)
            return;

        ChaseFailureSkipCount = 0;
        // All monsters normally have CanPass set.
        // Dehacked can modify things into enemies that can move but this flag doesn't exist in the original game.
        // Set this flag for anything that tries to move, otherwise they can clip ito other things and get stuck, especialliy with float.
        Flags.CanPass = true;
        Assert.Precondition(Target() != null, "Target is null");

        MoveDir dir0;
        MoveDir dir1;
        MoveDir oldDirection = m_direction;
        MoveDir oppositeDirection = oldDirection;
        MoveDir tdir;

        if (oppositeDirection != MoveDir.None)
            oppositeDirection = (MoveDir)(((int)oppositeDirection) ^ 4);

        var target = Target()!;
        double dx = target.Position.X - Position.X;
        double dy = target.Position.Y - Position.Y;

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
            if (dy < 0)
                m_direction = dx > 0 ? MoveDir.SouthEast : MoveDir.SouthWest;
            else
                m_direction = dx > 0 ? MoveDir.NorthEast : MoveDir.NorthWest;

            if (m_direction != oppositeDirection && TryWalk())
                return;
        }

        if (WorldStatic.Random.NextByte() > 200 || Math.Abs(dy) > Math.Abs(dx))
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
            if ((WorldStatic.Random.NextByte() & 1) != 0)
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
            int random = WorldStatic.Random.NextByte();
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
            MoveCount = WorldStatic.Random.NextByte() & 15;

        if (WorldStatic.SlowTickEnabled)
            ChaseFailureSkipCount = (short)(WorldStatic.SlowTickChaseFailureSkipCount + (ChaseFailureCount++ & 1));

        // Need to try to use the monster's normal movement speed if stuck. Otherwise they may never move or correctly cross teleport lines.
        ClosetChaseSpeed = MonsterMovementSpeed;
        m_direction = MoveDir.None;
    }

    public void GetEnemySpeed(out double speedX, out double speedY)
    {
        if (m_direction == MoveDir.None || ((Flags.Flags1 & EntityFlags.FloatFlag) == 0 && !OnGround))
        {
            speedX = 0;
            speedY = 0;
            return;
        }

        double speed = (ClosetFlags & ClosetFlags.ClosetChase) != 0 ? ClosetChaseSpeed :
            Math.Clamp(MonsterMovementSpeed * SlowTickMultiplier, -128, 128);
        speedX = Speeds[(int)m_direction] * speed;
        speedY = Speeds[(int)m_direction + 8] * speed;
    }

    public bool MoveEnemy(out TryMoveData? tryMove)
    {
        bool floatFlag = (Flags.Flags1 & EntityFlags.FloatFlag) != 0;
        if (m_direction == MoveDir.None || (!floatFlag && !OnGround) || IsFrozen)
        {
            tryMove = null;
            return false;
        }

        GetEnemySpeed(out double speedX, out double speedY);
        bool isMoving = speedX != 0 || speedY != 0;
        Flags.MonsterMove = true;
        tryMove = WorldStatic.World.PhysicsManager.TryMoveXY(this, Position.X + speedX, Position.Y + speedY);
        Flags.MonsterMove = false;
        if (Flags.Teleported)
            return true;

        if (!tryMove.Success && floatFlag && tryMove.CanFloat)
        {
            Flags.InFloat = true;
            Position.Z += Position.Z < tryMove.HighestFloorZ ? FloatSpeed : -FloatSpeed;
            return true;
        }
        else
        {
            Flags.InFloat = false;
        }

        if (tryMove.Success && !floatFlag && isMoving)
            Position.Z = tryMove.HighestFloorZ;

        // With increased speeds using the TickMultiplier TryMove will iterate and can have partial successes.
        // A partial success needs be considered true in this case.
        return Position.X != PrevPosition.X || Position.Y != PrevPosition.Y || tryMove.Success;
    }

    public void TurnTowardsMovementDirection()
    {
        if (m_direction == MoveDir.None)
            return;

        AngleRadians = Math.Round(AngleRadians / MathHelper.QuarterPi) * MathHelper.QuarterPi;
        if (AngleRadians < 0 || AngleRadians > MathHelper.TwoPi)
            AngleRadians = MathHelper.GetPositiveAngle(AngleRadians);
        double delta = AngleRadians - (int)m_direction * MathHelper.QuarterPi;
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
            AngleRadians = (int)m_direction * MathHelper.QuarterPi;
    }

    public double GetEnemyFloatMove()
    {
        if (IsPlayer || IsDead || !Flags.Float || Flags.Skullfly || Flags.InFloat || OnGround)
            return 0.0;

        var target = Target();
        if (target == null)
            return 0.0;

        double distance = Position.ApproximateDistance2D(target.Position);
        double dz = (target.Position.Z - Position.Z + (Height / 2)) * 3;

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

        if (WorldStatic.Udmf && Sector.NoAttack)
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

        return WorldStatic.World.CheckLineOfSight(this, entity);
    }

    public bool CheckMissileRange()
    {
        if (WorldStatic.Udmf && Sector.NoAttack)
            return false;

        var target = Target();
        if (target == null || IsFriend(target) || !WorldStatic.World.CheckLineOfSight(this, target))
            return false;

        if (Flags.JustHit)
        {
            Flags.JustHit = false;
            return true;
        }

        if (ReactionTime > 0)
            return false;

        double distance = Position.ApproximateDistance2D(target.Position);

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
        return WorldStatic.Random.NextByte() >= distance;
    }

    public bool TryWalk()
    {
        if (!MoveEnemy(out TryMoveData? tryMove))
        {
            if (tryMove != null)
            {
                for (int i = 0; i < tryMove.ImpactSpecialLines.Length; i++)
                    WorldStatic.World.ActivateSpecialLine(this, WorldStatic.World.Lines[tryMove.ImpactSpecialLines[i]], ActivationContext.UseLine, Position.X, Position.Y);

                for (int i = 0; i < tryMove.IntersectSpecialLines.Length; i++)
                    WorldStatic.World.ActivateSpecialLine(this, WorldStatic.World.Lines[tryMove.IntersectSpecialLines[i]], ActivationContext.UseLine, Position.X, Position.Y);
            }

            return false;
        }

        ClosetChaseSpeed = DefaultClosetChaseSpeed;
        MoveCount = WorldStatic.Random.NextByte() & 15;
        return true;
    }
}
