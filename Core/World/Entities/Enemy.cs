using System;
using Helion.Util.Assertion;
using Helion.Util.Geometry.Vectors;

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

        private static readonly MoveDir[] OppositeDirections = new[]
        {
            MoveDir.West, MoveDir.SoutWest, MoveDir.South, MoveDir.SouthEast,
            MoveDir.East, MoveDir.NorthEast, MoveDir.North, MoveDir.NorthWest, MoveDir.None
        };

        private static readonly MoveDir[] Diagnals = new[] { MoveDir.NorthWest, MoveDir.NorthEast, MoveDir.SoutWest, MoveDir.SouthEast };

        private static readonly double[] SpeedX = new[] { 1.0, Speed, 0, -Speed, -1.0, -Speed, 0, Speed };
        private static readonly double[] SpeedY = new[] { 0, Speed, 1.0, Speed, 0, -Speed, -1.0, -Speed };

        private MoveDir m_direction = MoveDir.None;

        public bool BlockFloating;
        public bool IsEnemyMove;

        public bool SetNewTarget(bool allaround)
        {
            Entity? newTarget;
            if (Target != null && !Target.IsDead)
                newTarget = Target;
            else
                newTarget = EntityManager.World.GetLineOfSightPlayer(this, allaround);

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

        public bool MoveEnemy()
        {
            if (m_direction == MoveDir.None || (!Flags.Float && !OnGround))
                return false;

            Vec3D saveVelocity = Velocity;
            // TODO get speed from definition, currently it is always zero
            double speedX = SpeedX[(int)m_direction] * 8;
            double speedY = SpeedY[(int)m_direction] * 8;
            Velocity.X = speedX;
            Velocity.Y = speedY;

            Vec3D oldPos = Position;
            Vec3D nextPos = new Vec3D(Position.X + speedX, Position.Y + speedY, 0);
            AngleRadians = Position.Angle(nextPos);

            // TODO would like to refactor out IsEnemyMove
            IsEnemyMove = true;
            bool success = EntityManager.World.PhysicsManager.TryMoveXY(this);
            IsEnemyMove = false;
            Velocity = saveVelocity;

            if (!success)
            {
                if (Flags.Float && DropOffZ != NoDropOff)
                {
                    BlockFloating = true;
                    Vec3D pos = new Vec3D(Position.X, Position.Y, Position.Z + (Position.Z < DropOffZ ? FloatSpeed : -FloatSpeed));

                    if (pos.Z < HighestFloorZ || pos.Z > LowestCeilingZ)
                        return false;

                    SetPosition(pos);
                    return true;
                }
            }
            else
            {
                BlockFloating = false;
            }

            // TODO may be able to clean this up, but after TryMoveXY the z may cause the dropoff to get the enemy stuck so we need to verify again
            if (success && !Flags.Float)
            {
                Vec3D newPos = new Vec3D(Position.X, Position.Y, HighestFloorZ);

                IsEnemyMove = true;
                success = EntityManager.World.PhysicsManager.IsPositionValid(this, newPos.To2D());
                IsEnemyMove = false;
                if (success)
                    SetPosition(newPos);
            }

            // TODO refactor TryMoveXY to not actually move the entity
            if (!success)
                SetPosition(oldPos);

            return success;
        }

        public double GetEnemyFloatMove()
        {
            if (Target == null || !Flags.IsMonster || !Flags.Float || Flags.Skullfly || BlockFloating)
                return 0.0;

            double distance = Position.To2D().Distance(Target.Position.To2D());
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

            double distance = Position.To2D().Distance(entity.Position.To2D());

            if (distance >= Properties.MeleeRange + Radius)
                return false;

            if (!Flags.NoVerticalMeleeRange && (entity.Position.Z > Position.Z + Height || entity.Position.Z + entity.Height < Position.Z))
                return false;

            if (!EntityManager.World.PhysicsManager.CheckLineOfSight(this, entity))
                return false;

            return true;
        }

        public bool CheckMissileRange()
        {
            if (Target == null || !EntityManager.World.PhysicsManager.CheckLineOfSight(this, Target))
                return false;

            return true;
        }

        private bool TryWalk()
        {
            if (!MoveEnemy())
                return false;

            MoveCount = EntityManager.World.Random.NextByte() & 15;
            return true;
        }
    }
}
