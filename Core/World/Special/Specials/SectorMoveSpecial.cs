﻿using System;
using Helion.Audio;
using Helion.Maps.Specials.ZDoom;
using Helion.World.Entities;
using Helion.World.Geometry.Sectors;
using Helion.World.Physics;
using Helion.World.Sound;
using Helion.World.Special.SectorMovement;

namespace Helion.World.Special.Specials
{
    public class SectorMoveSpecial : ISectorSpecial
    {
        public Sector Sector { get; protected set; }
        public SectorMoveData MoveData { get; protected set; }
        public SectorSoundData SoundData { get; protected set; }
        public SectorPlane SectorPlane { get; protected set; }
        public bool IsPaused { get; private set; }

        protected readonly WorldBase m_world;
        protected double DestZ;
        protected int DelayTics;

        private readonly double m_startZ;
        private readonly double m_minZ;
        private readonly double m_maxZ;
        private MoveDirection m_direction;
        private double m_speed;
        private bool m_crushing;
        private bool m_playedReturnSound;
        private bool m_playedStartSound;

        public SectorMoveSpecial(WorldBase world, Sector sector, double start, double dest,
            SectorMoveData specialData)
            : this(world, sector, start, dest, specialData, new SectorSoundData())
        {
        }

        public SectorMoveSpecial(WorldBase world, Sector sector, double start, double dest,
            SectorMoveData specialData, SectorSoundData soundData)
        {
            Sector = sector;
            m_world = world;
            MoveData = specialData;
            SoundData = soundData;
            SectorPlane = MoveData.SectorMoveType == SectorPlaneType.Floor ? sector.Floor : sector.Ceiling;
            m_startZ = start;
            DestZ = dest;

            m_direction = MoveData.StartDirection;
            m_speed = MoveData.StartDirection == MoveDirection.Down ? -MoveData.Speed : MoveData.Speed;

            m_minZ = Math.Min(m_startZ, DestZ);
            m_maxZ = Math.Max(m_startZ, DestZ);

            Sector.ActiveMoveSpecial = this;
        }

        public virtual SpecialTickStatus Tick()
        {
            if (IsPaused)
                return SpecialTickStatus.Continue;

            if (DelayTics > 0)
            {
                SectorPlane.PrevZ = SectorPlane.Z;
                DelayTics--;
                return SpecialTickStatus.Continue;
            }

            double destZ = CalculateDestination();
            PerformAndHandleMoveZ(destZ);

            PlaySound();

            if (SectorPlane.Z == DestZ && IsNonRepeat)
            {
                if (MoveData.FloorChangeTextureHandle != null)
                {
                    Sector.SectorDamageSpecial = null;
                    Sector.Floor.TextureHandle = MoveData.FloorChangeTextureHandle.Value;
                }

                if (MoveData.DamageSpecial != null)
                    Sector.SectorDamageSpecial = MoveData.DamageSpecial;

                Sector.ActiveMoveSpecial = null;
                return SpecialTickStatus.Destroy;
            }

            if (IsDelayReturn && SectorPlane.Z == m_startZ)
            {
                Sector.ActiveMoveSpecial = null;
                return SpecialTickStatus.Destroy;
            }

            if (SectorPlane.Z == DestZ)
                FlipMovementDirection(false);

            return SpecialTickStatus.Continue;
        }

        public void ResetInterpolation()
        {
            SectorPlane.PrevZ = SectorPlane.Z;
        }

        private void PlaySound()
        {
            if (SectorPlane.Z == DestZ)
            {
                if (SoundData.StopSound != null)
                    m_world.SoundManager.CreateSoundOn(Sector, SoundData.StopSound, SoundChannelType.Auto, new SoundParams(Sector));
                return;
            }

            if (!m_playedStartSound)
            {
                m_playedStartSound = true;
                if (SoundData.StartSound != null)
                    m_world.SoundManager.CreateSoundOn(Sector, SoundData.StartSound, SoundChannelType.Auto, new SoundParams(Sector));
                if (SoundData.MovementSound != null)
                    m_world.SoundManager.CreateSoundOn(Sector, SoundData.MovementSound, SoundChannelType.Auto, new SoundParams(Sector, true));
            }

            if (m_direction != MoveData.StartDirection && !m_playedReturnSound)
            {
                m_playedReturnSound = true;
                if (SoundData.ReturnSound != null)
                    m_world.SoundManager.CreateSoundOn(Sector, SoundData.ReturnSound, SoundChannelType.Auto, new SoundParams(Sector));
            }
        }

        public virtual void FinalizeDestroy()
        {
            SectorPlane.PrevZ = SectorPlane.Z;
            if (SoundData.MovementSound != null)
                m_world.SoundManager.StopSoundBySource(Sector, SoundChannelType.Auto, SoundData.MovementSound);
        }

        public virtual void Use(Entity entity)
        {
        }

        public void Pause()
        {
            IsPaused = true;
            SectorPlane.PrevZ = SectorPlane.Z;
            if (SoundData.MovementSound != null)
                m_world.SoundManager.StopSoundBySource(Sector, SoundChannelType.Auto, SoundData.MovementSound);
        }

        public void Resume()
        {
            IsPaused = false;
            if (SoundData.MovementSound != null)
                m_world.SoundManager.CreateSoundOn(Sector, SoundData.MovementSound, SoundChannelType.Auto, new SoundParams(Sector, true));
        }

        public virtual SectorBaseSpecialType SectorBaseSpecialType => SectorBaseSpecialType.Move;

        protected void FlipMovementDirection(bool blocked)
        {
            if (!blocked && (MoveData.MoveRepetition == MoveRepetition.Perpetual || (IsDelayReturn && m_direction == MoveData.StartDirection)))
                DelayTics = MoveData.Delay;

            m_playedReturnSound = false;

            m_direction = m_direction == MoveDirection.Up ? MoveDirection.Down : MoveDirection.Up;
            DestZ = m_direction == MoveDirection.Up ? m_maxZ : m_minZ;

            if (m_direction == MoveData.StartDirection && SoundData.StartSound != null)
                m_playedStartSound = false;

            if (MoveData.Crush != null)
            {
                if (m_direction == MoveDirection.Up)
                    m_speed = -MoveData.Speed * MoveData.Crush.ReturnFactor;
                else
                    m_speed = MoveData.Speed;
            }

            if (m_crushing)
            {
                m_speed = MoveData.Speed;
                m_crushing = false;
            }
            else
            {
                m_speed = -m_speed;
            }
        }

        private double CalculateDestination()
        {
            double destZ = SectorPlane.Z + m_speed;

            if (m_direction == MoveDirection.Down && destZ < DestZ)
                destZ = DestZ;
            else if (m_direction == MoveDirection.Up && destZ > DestZ)
                destZ = DestZ;

            return destZ;
        }

        private void PerformAndHandleMoveZ(double destZ)
        {
            SectorMoveStatus status = m_world.MoveSectorZ(Sector, SectorPlane, MoveData.SectorMoveType,
                m_direction, m_speed, destZ, MoveData.Crush);

            switch (status)
            {
            case SectorMoveStatus.Blocked:
                if (MoveData.MoveRepetition != MoveRepetition.None)
                    FlipMovementDirection(true);
                break;
            case SectorMoveStatus.Crush when IsInitCrush:
                Sector.DataChanged = true;
                // TODO: Can we maybe make this into its own class to avoid the null issue?
                if (MoveData.Crush == null)
                    throw new NullReferenceException("Should never have a null crush component when having a crushing sector");
                m_crushing = true;
                if (MoveData.Crush.CrushMode == ZDoomCrushMode.DoomWithSlowDown)
                    m_speed = m_speed < 0 ? -0.1 : 0.1;
                break;

            case SectorMoveStatus.Success:
                Sector.DataChanged = true;
                break;
            }

            if (m_crushing && status == SectorMoveStatus.Success)
                m_crushing = false;
        }

        private bool IsNonRepeat => MoveData.MoveRepetition == MoveRepetition.None || MoveData.MoveRepetition == MoveRepetition.ReturnOnBlock;
        private bool IsDelayReturn => MoveData.MoveRepetition == MoveRepetition.DelayReturn;
        private bool IsInitCrush => MoveData.Crush != null && m_direction == MoveData.StartDirection && !m_crushing;
    }
}