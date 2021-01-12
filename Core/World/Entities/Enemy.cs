using System;
using Helion.Util;
using Helion.Util.Assertion;
using Helion.Util.Geometry.Vectors;
using Helion.World.Physics;

namespace Helion.World.Entities
{
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
            SoutWest,
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
            MoveDir.West, MoveDir.SoutWest, MoveDir.South, MoveDir.SouthEast,
            MoveDir.East, MoveDir.NorthEast, MoveDir.North, MoveDir.NorthWest, MoveDir.None
        };

        private static readonly MoveDir[] Diagnals = new[] { MoveDir.NorthWest, MoveDir.NorthEast, MoveDir.SoutWest, MoveDir.SouthEast };

        private static readonly double[] SpeedX = new[] { 1.0, Speed, 0, -Speed, -1.0, -Speed, 0, Speed };
        private static readonly double[] SpeedY = new[] { 0, Speed, 1.0, Speed, 0, -Speed, -1.0, -Speed };

        private MoveDir m_direction = MoveDir.None;
        private bool m_enemyMove;

        public bool BlockFloating;

        public bool ValidEnemyTarget(Entity? entity) => entity != null && !entity.IsDead;

        public bool SetNewTarget(bool allaround)
        {
            Entity? newTarget = null;
            if (ValidEnemyTarget(Target))
            {
                newTarget = Target;
            }
            else
            {
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
                else
                {
                    newTarget = EntityManager.World.GetLineOfSightPlayer(this, allaround);
                }
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

        public void SetNewChaseDirection()
        {
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
                return Position.To2D();

            double speedX = SpeedX[(int)m_direction] * Definition.Properties.Speed;
            double speedY = SpeedY[(int)m_direction] * Definition.Properties.Speed;

            return new Vec2D(Position.X + speedX, Position.Y + speedY);
        }

        public bool MoveEnemy(out TryMoveData? tryMove)
        {
            if (m_direction == MoveDir.None || (!Flags.Float && !OnGround))
            {
                tryMove = null;
                return false;
            }

            Vec2D nextPos = GetNextEnemyPos();

            m_enemyMove = true;
            tryMove = World.TryMoveXY(this, nextPos, false);
            m_enemyMove = false;

            if (!tryMove.Success && Flags.Float && tryMove.CanFloat)
            {
                BlockFloating = true;
                Vec3D pos = new Vec3D(Position.X, Position.Y, Position.Z + (Position.Z < tryMove.HighestFloorZ ? FloatSpeed : -FloatSpeed));
                SetPosition(pos);
                return true;
            }
            else
            {
                BlockFloating = false;
            }

            if (tryMove.Success && !Flags.Float)
            {
                Vec3D newPos = new Vec3D(Position.X, Position.Y, tryMove.HighestFloorZ);
                SetPosition(newPos);
            }
            
            if (tryMove.Success)
            {
                // TODO Doom has 'turn towards movement direction if not there yet'
                AngleRadians = MoveAngles[(int)m_direction];
            }

            return tryMove.Success;
        }

        public double GetEnemyFloatMove()
        {
            if (IsDead || Target == null || !Flags.IsMonster || !Flags.Float || Flags.Skullfly || BlockFloating)
                return 0.0;

            double distance = Position.ApproximateDistance2D(Target.Position);
            double dz = (Target.Position.Z - Position.Z + (Height / 2)) * 3;

            if (dz < 0 && distance < -dz)
                return -FloatSpeed;
            else if (dz > 0 && distance < dz)
                return FloatSpeed;

            return 0.0;
        }

        public bool InMeleeRange(Entity entity)
        {
            if (entity == null || Properties.MeleeRange == 0)
                return false;

            double distance = Position.ApproximateDistance2D(entity.Position);

            if (distance >= Properties.MeleeRange + entity.Radius)
                return false;

            if (!Flags.NoVerticalMeleeRange && (entity.Position.Z > Position.Z + Height || entity.Position.Z + entity.Height < Position.Z))
                return false;

            if (!EntityManager.World.CheckLineOfSight(this, entity))
                return false;

            return true;
        }

        public bool CheckMissileRange()
        {
            if (Target == null || !EntityManager.World.CheckLineOfSight(this, Target))
                return false;

            if (Flags.JustHit)
            {
                Flags.JustHit = false;
                return true;
            }

            if (Properties.ReactionTime > 0)
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

            // TODO use game skill when implemented, changes this chance
            distance = Math.Min(distance, Definition.Properties.MinMissileChance);
            return World.Random.NextByte() >= distance;
        }

        private bool TryWalk()
        {
            if (!MoveEnemy(out TryMoveData? tryMove))
            {
                if (tryMove != null && tryMove.IntersectSpecialLines.Count > 0)
                {
                    for (int i = 0; i < tryMove.IntersectSpecialLines.Count; i++)
                        World.ActivateSpecialLine(this, tryMove.IntersectSpecialLines[i], ActivationContext.UseLine);
                }

                return false;
            }

            MoveCount = EntityManager.World.Random.NextByte() & 15;
            return true;
        }
    }
}
