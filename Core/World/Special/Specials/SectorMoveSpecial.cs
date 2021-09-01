using System;
using Helion.Maps.Specials.ZDoom;
using Helion.Models;
using Helion.Util;
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
        public SectorMoveStatus MoveStatus { get; private set; }

        protected readonly IWorld m_world;
        protected double DestZ;
        protected int DelayTics;

        private readonly double m_startZ;
        private readonly double m_minZ;
        private readonly double m_maxZ;
        private MoveDirection m_direction;
        private double m_speed;
        private readonly double[] m_speeds = new double[2];
        private bool m_crushing;
        private bool m_playedReturnSound;
        private bool m_playedStartSound;

        public SectorMoveSpecial(IWorld world, Sector sector, double start, double dest,
            SectorMoveData specialData)
            : this(world, sector, start, dest, specialData, new SectorSoundData())
        {
        }

        public SectorMoveSpecial(IWorld world, Sector sector, double start, double dest,
            SectorMoveData specialData, SectorSoundData soundData)
        {
            Sector = sector;
            m_world = world;
            MoveData = specialData;
            SoundData = soundData;
            SectorPlane = MoveData.SectorMoveType == SectorPlaneFace.Floor ? sector.Floor : sector.Ceiling;
            m_startZ = start;
            DestZ = dest;

            m_direction = MoveData.StartDirection;
            InitSpeeds();
            m_speed = m_speeds[0];

            m_minZ = Math.Min(m_startZ, DestZ);
            m_maxZ = Math.Max(m_startZ, DestZ);

            Sector.SetActiveMoveSpecial(MoveData.SectorMoveType, this);
        }

        public SectorMoveSpecial(IWorld world, Sector sector, SectorMoveSpecialModel model)
        {
            Sector = sector;
            m_world = world;
            MoveData = new SectorMoveData((SectorPlaneFace)model.MoveType, (MoveDirection)model.StartDirection, 
                (MoveRepetition)model.Repetion, model.Speed, model.Delay, 
                crush: FromCrushDataModel(model.Crush), 
                floorChangeTextureHandle: model.FloorChange, 
                ceilingChangeTextureHandle: model.CeilingChange, 
                damageSpecial: model.DamageSpecial?.ToWorldSpecial(world),
                returnSpeed: model.ReturnSpeed,
                compatibilityBlockMovement: model.CompatibilityBlockMovement);
            SoundData = new SectorSoundData(model.StartSound, model.ReturnSound, model.StopSound, model.MovementSound);
            SectorPlane = MoveData.SectorMoveType == SectorPlaneFace.Floor ? sector.Floor : sector.Ceiling;
            m_startZ = model.StartZ;
            DestZ = model.DestZ;
            m_minZ = model.MinZ;
            m_maxZ = model.MaxZ;
            m_speed = model.CurrentSpeed;
            DelayTics = model.DelayTics;
            m_direction = (MoveDirection)model.Direction;
            m_crushing = model.Crushing;
            m_playedStartSound = model.PlayedStartSound;
            m_playedReturnSound = model.PlayedReturnSound;
            IsPaused = model.Paused;

            Sector.SetActiveMoveSpecial(MoveData.SectorMoveType, this);
            InitSpeeds();
            if (SoundData.MovementSound != null)
                CreateSound(SoundData.MovementSound, true);
        }

        private void InitSpeeds()
        {
            m_speeds[0] = MoveData.StartDirection == MoveDirection.Down ? -MoveData.Speed : MoveData.Speed;
            m_speeds[1] = MoveData.StartDirection == MoveDirection.Up ? -MoveData.ReturnSpeed : MoveData.ReturnSpeed;

            if (MoveData.Crush != null)
                m_speeds[1] *= MoveData.Crush.ReturnFactor;
        }

        public virtual ISpecialModel ToSpecialModel()
        {
            return new SectorMoveSpecialModel()
            {
                SectorId = Sector.Id,
                MoveType = (int)MoveData.SectorMoveType,
                Repetion = (int)MoveData.MoveRepetition,
                Speed = MoveData.Speed,
                ReturnSpeed = MoveData.ReturnSpeed,
                Delay = MoveData.Delay,
                FloorChange = MoveData.FloorChangeTextureHandle,
                StartDirection = (int)MoveData.StartDirection,
                CompatibilityBlockMovement = MoveData.CompatibilityBlockMovement,
                StartSound = SoundData.StartSound,
                ReturnSound = SoundData.ReturnSound,
                StopSound = SoundData.StopSound,
                MovementSound = SoundData.MovementSound,
                CurrentSpeed = m_speed,
                DestZ = DestZ,
                StartZ = m_startZ,
                MinZ = m_minZ,
                MaxZ = m_maxZ,
                DelayTics = DelayTics,
                Direction = (int)m_direction,
                Crushing = m_crushing,
                PlayedReturnSound = m_playedReturnSound,
                PlayedStartSound = m_playedStartSound,
                Paused = IsPaused,
                DamageSpecial = CreateSectorDamageSpecialModel(),
                Crush = CreateCrushDataModel(),
            };
        }

        public void SetDelayTics(int delayTics) => DelayTics = delayTics;

        private static CrushData? FromCrushDataModel(CrushDataModel? model)
        {
            if (model == null)
                return null;

            return new CrushData(model);
        }

        private CrushDataModel? CreateCrushDataModel()
        {
            if (MoveData.Crush == null)
                return null;

            return MoveData.Crush.ToCrushDataModel();
        }

        private SectorDamageSpecialModel? CreateSectorDamageSpecialModel()
        {
            if (MoveData.DamageSpecial == null)
                return null;

            return MoveData.DamageSpecial.ToSectorDamageSpecialModel();
        }

        public virtual SpecialTickStatus Tick()
        {
            if (IsPaused)
            {
                SectorPlane.PrevZ = SectorPlane.Z;
                return SpecialTickStatus.Continue;
            }

            if (DelayTics > 0)
            {
                SectorPlane.PrevZ = SectorPlane.Z;
                DelayTics--;
                return SpecialTickStatus.Continue;
            }

            CheckPlaySound();

            double destZ = CalculateDestination();
            PerformAndHandleMoveZ(destZ);

            if (MoveStatus == SectorMoveStatus.BlockedAndStop)
                DestZ = SectorPlane.Z;

            CheckPlaySound();

            if ((SectorPlane.Z == DestZ && IsNonRepeat) || MoveStatus == SectorMoveStatus.BlockedAndStop)
            {
                if (MoveData.FloorChangeTextureHandle != null)
                    Sector.Floor.SetTexture(MoveData.FloorChangeTextureHandle.Value);

                if (MoveData.CeilingChangeTextureHandle != null)
                    Sector.Ceiling.SetTexture(MoveData.CeilingChangeTextureHandle.Value);

                if (MoveData.DamageSpecial != null)
                    Sector.SectorDamageSpecial = MoveData.DamageSpecial;

                Sector.ClearActiveMoveSpecial(MoveData.SectorMoveType);
                return SpecialTickStatus.Destroy;
            }

            if (IsDelayReturn && SectorPlane.Z == m_startZ)
            {
                Sector.ClearActiveMoveSpecial(MoveData.SectorMoveType);
                return SpecialTickStatus.Destroy;
            }

            if (SectorPlane.Z == DestZ)
                FlipMovementDirection(false);

            return SpecialTickStatus.Continue;
        }

        public virtual void ResetInterpolation()
        {
            SectorPlane.PrevZ = SectorPlane.Z;
        }

        private void CheckPlaySound()
        {
            if (SectorPlane.Z == DestZ)
            {
                if (SoundData.StopSound != null)
                    CreateSound(SoundData.StopSound);
                return;
            }

            if (!m_playedStartSound)
            {
                m_playedStartSound = true;
                if (SoundData.StartSound != null)
                    CreateSound(SoundData.StartSound);
                if (SoundData.MovementSound != null)
                    CreateSound(SoundData.MovementSound, true);
            }

            if (m_direction != MoveData.StartDirection && !m_playedReturnSound)
            {
                m_playedReturnSound = true;
                if (SoundData.ReturnSound != null)
                    CreateSound(SoundData.ReturnSound);
            }
        }

        private void CreateSound(string sound, bool loop = false)
        {
            m_world.SoundManager.CreateSoundOn(SectorPlane, sound, SoundChannelType.Auto,
                DataCache.Instance.GetSoundParams(SectorPlane, loop));
        }

        private void StopSound(string sound)
        {
            m_world.SoundManager.StopSoundBySource(SectorPlane, SoundChannelType.Auto, sound);
        }

        public virtual void FinalizeDestroy()
        {
            SectorPlane.PrevZ = SectorPlane.Z;
            if (SoundData.MovementSound != null)
                StopSound(SoundData.MovementSound);
        }

        public virtual bool Use(Entity entity)
        {
            return false;
        }

        public void Pause()
        {
            IsPaused = true;
            SectorPlane.PrevZ = SectorPlane.Z;
            if (SoundData.MovementSound != null)
                StopSound(SoundData.MovementSound);
        }

        public void Resume()
        {
            IsPaused = false;
            if (SoundData.MovementSound != null)
                CreateSound(SoundData.MovementSound, true);
        }

        public virtual SectorBaseSpecialType SectorBaseSpecialType => SectorBaseSpecialType.Move;

        protected void FlipMovementDirection(bool blocked)
        {
            if (!blocked && (MoveData.MoveRepetition == MoveRepetition.Perpetual || (IsDelayReturn && m_direction == MoveData.StartDirection)))
                DelayTics = MoveData.Delay;

            m_playedReturnSound = false;

            m_direction = m_direction == MoveDirection.Up ? MoveDirection.Down : MoveDirection.Up;
            DestZ = m_direction == MoveDirection.Up ? m_maxZ : m_minZ;
            int speedIndex = m_direction == MoveData.StartDirection ? 0 : 1;

            if (m_direction == MoveData.StartDirection && SoundData.StartSound != null)
                m_playedStartSound = false;

            if (m_crushing)
                m_crushing = false;

            m_speed = m_speeds[speedIndex];

            if (MoveData.MoveRepetition == MoveRepetition.PerpetualPause)
                IsPaused = true;
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
            MoveStatus = m_world.MoveSectorZ(Sector, SectorPlane, MoveData.SectorMoveType,
                m_speed, destZ, MoveData.Crush, MoveData.CompatibilityBlockMovement);

            switch (MoveStatus)
            {
                case SectorMoveStatus.Blocked:
                    if (MoveData.MoveRepetition != MoveRepetition.None)
                        FlipMovementDirection(true);
                    break;

                case SectorMoveStatus.Crush when IsInitCrush:
                    SetSectorDataChange();
                    m_crushing = true;
                    if (MoveData.Crush != null && MoveData.Crush.CrushMode == ZDoomCrushMode.DoomWithSlowDown)
                        m_speed = m_speed < 0 ? -0.1 : 0.1;
                    break;

                case SectorMoveStatus.Success:
                    SetSectorDataChange();
                    break;
            }

            if (m_crushing && MoveStatus == SectorMoveStatus.Success)
                m_crushing = false;
        }

        private void SetSectorDataChange()
        {
            SectorPlane.SetRenderingChanged();
            if (MoveData.SectorMoveType == SectorPlaneFace.Floor)
                Sector.DataChanges |= SectorDataTypes.FloorZ;
            else
                Sector.DataChanges |= SectorDataTypes.CeilingZ;
        }

        private bool IsNonRepeat => MoveData.MoveRepetition == MoveRepetition.None || MoveData.MoveRepetition == MoveRepetition.ReturnOnBlock;
        private bool IsDelayReturn => MoveData.MoveRepetition == MoveRepetition.DelayReturn;
        private bool IsInitCrush => MoveData.Crush != null && m_direction == MoveData.StartDirection && !m_crushing;
    }
}