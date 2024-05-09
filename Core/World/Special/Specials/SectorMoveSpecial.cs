using System;
using System.Collections.Generic;
using System.Reflection;
using Helion.Audio;
using Helion.Maps.Specials.ZDoom;
using Helion.Models;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Physics;
using Helion.World.Sound;
using Helion.World.Special.SectorMovement;

namespace Helion.World.Special.Specials;

public class SectorMoveSpecial : ISectorSpecial
{
    public Sector Sector { get; set; }
    public SectorPlane SectorPlane { get; set; }
    public SectorMoveData MoveData { get; protected set; }
    public SectorSoundData SoundData { get; protected set; }
    public bool IsPaused { get; private set; }
    public SectorMoveStatus MoveStatus { get; private set; }
    public MoveDirection MoveDirection => m_direction;
    public int DelayTics { get; protected set; }
    public double MoveSpeed => m_speed;
    public bool IsCrushing => m_crushing;
    // If this sector started out with the ceiling clipped through the floor
    public bool StartClipped { get; private set; }
    public virtual bool OverrideEquals => true;
    public bool IsInitialMove { get; protected set; } = true;
    public virtual bool MultiSector => false;
    public virtual void GetSectors(List<(Sector, SectorPlane)> data) { }
    public double DestZ { get; protected set; }
    public bool IsDoor;

    protected IWorld m_world;

    private double m_startZ;
    private double m_minZ;
    private double m_maxZ;
    private MoveDirection m_direction;
    private double m_speed;
    private double m_startSpeed;
    private double m_returnSpeed;
    private bool m_crushing;
    private bool m_playedReturnSound;
    private bool m_playedStartSound;

    public SectorMoveSpecial()
    {

    }

    public SectorMoveSpecial(IWorld world, Sector sector, double start, double dest,
        in SectorMoveData specialData, in SectorSoundData soundData)
    {
        Set(world, sector, start, dest, specialData, soundData);
    }

    public void Set(IWorld world, Sector sector, double start, double dest,
        in SectorMoveData specialData, in SectorSoundData soundData)
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
        InitStartClip();
        m_speed = m_startSpeed;

        m_minZ = Math.Min(m_startZ, DestZ);
        m_maxZ = Math.Max(m_startZ, DestZ);

        Sector.SetActiveMoveSpecial(MoveData.SectorMoveType, this);
    }

    public SectorMoveSpecial(IWorld world, Sector sector, SectorMoveSpecialModel model)
    {
        Set(world, sector, model);
    }

    public void Set(IWorld world, Sector sector, SectorMoveSpecialModel model)
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
            flags: (SectorMoveFlags)model.Flags,
            lightTag: model.LightTag ?? 0);
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
        IsDoor = model.Door;

        Sector.SetActiveMoveSpecial(MoveData.SectorMoveType, this);
        InitSpeeds();
        InitStartClip();
        if (SoundData.MovementSound != null)
            CreateSound(SoundData.MovementSound, true);
    }

    // If setting the plane to destZ would complete this move.
    public bool IsFinalDestination(double destZ)
    {
        if (m_direction == MoveDirection.Down)
            return destZ <= DestZ;

        return destZ >= DestZ;
    }

    private void InitSpeeds()
    {
        m_startSpeed = MoveData.StartDirection == MoveDirection.Down ? -MoveData.Speed : MoveData.Speed;
        m_returnSpeed = MoveData.StartDirection == MoveDirection.Up ? -MoveData.ReturnSpeed : MoveData.ReturnSpeed;

        if (MoveData.Crush != null)
            m_returnSpeed *= MoveData.Crush.Value.ReturnFactor;
    }

    private void InitStartClip()
    {
        // Physics needs to know if this was started with the floor clipped through the ceiling to allow movement.
        StartClipped = Sector.Ceiling.Z < Sector.Floor.Z;
    }

    private void CheckStartClip()
    {
        if (!StartClipped)
            return;

        StartClipped = Sector.Ceiling.Z < Sector.Floor.Z;
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
            Flags = (int)MoveData.Flags,
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
            LightTag = MoveData.LightTag > 0 ? MoveData.LightTag : null,
            Door = IsDoor
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

        return MoveData.Crush.Value.ToCrushDataModel();
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
            if (SectorPlane.PrevZ != SectorPlane.Z)
                SectorPlane.SetSectorMoveChanged(m_world.Gametick);
            SectorPlane.PrevZ = SectorPlane.Z;
            return SpecialTickStatus.Continue;
        }

        if (DelayTics > 0)
        {
            if (SectorPlane.PrevZ != SectorPlane.Z)
                SectorPlane.SetSectorMoveChanged(m_world.Gametick);
            SectorPlane.PrevZ = SectorPlane.Z;
            DelayTics--;
            return SpecialTickStatus.Continue;
        }

        CheckPlaySound();

        double destZ = CalculateDestination();
        PerformAndHandleMoveZ(destZ);
        CheckStartClip();

        IsInitialMove = false;

        if (MoveStatus == SectorMoveStatus.BlockedAndStop)
            DestZ = SectorPlane.Z;

        if (MoveData.LightTag > 0)
            DoorLight.UpdateLight(m_world, MoveData.LightTag, m_maxZ, m_minZ, Sector.Ceiling.Z);

        CheckPlaySound();

        if ((IsNonRepeat && SectorPlane.Z == DestZ) || MoveStatus == SectorMoveStatus.BlockedAndStop)
        {
            if (CheckInstantMove(destZ))
                ResetInterpolation();

            if (MoveData.FloorChangeTextureHandle != null)
                m_world.SetPlaneTexture(Sector.Floor, MoveData.FloorChangeTextureHandle.Value);

            if (MoveData.CeilingChangeTextureHandle != null)
                m_world.SetPlaneTexture(Sector.Ceiling, MoveData.CeilingChangeTextureHandle.Value);

            if (MoveData.DamageSpecial != null)
                Sector.SectorDamageSpecial = MoveData.DamageSpecial.Copy(Sector);

            if ((MoveData.Flags & SectorMoveFlags.ClearDamage) != 0)
                Sector.SectorDamageSpecial = null;

            if (MoveData.SectorEffect != null)
                Sector.SetSectorEffect(MoveData.SectorEffect.Value);

            if (MoveData.KillEffect != null)
                Sector.SetKillEffect(MoveData.KillEffect.Value);

            StopMovementSound();
            Sector.ClearActiveMoveSpecial(MoveData.SectorMoveType);

            return SpecialTickStatus.Destroy;
        }

        if (IsDelayReturn && SectorPlane.Z == m_startZ && m_direction != MoveData.StartDirection)
        {
            StopMovementSound();
            Sector.ClearActiveMoveSpecial(MoveData.SectorMoveType);
            return SpecialTickStatus.Destroy;
        }

        if (SectorPlane.Z == DestZ)
            FlipMovementDirection(false);

        return SpecialTickStatus.Continue;
    }

    private bool CheckInstantMove(double destZ)
    {
        return Math.Abs(destZ - SectorPlane.PrevZ) > Math.Abs(m_speed);
    }

    private void StopMovementSound()
    {
        if (SoundData.MovementSound != null)
            StopSound(SoundData.MovementSound);
    }

    public virtual void ResetInterpolation()
    {
        SectorPlane.PrevZ = SectorPlane.Z;
        SectorPlane.LastRenderChangeGametick = m_world.Gametick;
    }

    private void CheckPlaySound()
    {
        // Doom does not play the stop sound for ceilings
        if (SectorPlane.Z == DestZ)
        {
            if (SoundData.StopSound != null)
                CreateSound(SoundData.StopSound);
            else if (SoundData.MovementSound != null && MoveData.MoveRepetition != MoveRepetition.Perpetual)
                StopSound(SoundData.MovementSound);
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
        m_world.SoundManager.CreateSoundOn(SectorPlane, sound, new SoundParams(SectorPlane, loop));
    }

    private void StopSound(string sound)
    {
        m_world.SoundManager.StopSoundBySource(SectorPlane, SoundChannel.Default, sound);
    }

    public virtual void FinalizeDestroy()
    {
        SectorPlane.SetSectorMoveChanged(m_world.Gametick);
        SectorPlane.PrevZ = SectorPlane.Z;
    }

    public virtual void Free()
    {
        Sector = null!;
        SectorPlane = null!;
        m_world = null!;

        IsPaused = default;
        MoveStatus = default;
        DelayTics = default;
        m_speed = default;
        m_crushing = default;
        StartClipped = default;
        IsInitialMove = true;
        DestZ = default;
        m_crushing = default;
        m_playedReturnSound = default;
        m_playedStartSound = default;
        IsDoor = default;
    }

    public virtual bool Use(Entity entity)
    {
        if (!IsDoor || MoveData.MoveRepetition == MoveRepetition.None || !entity.IsPlayer)
            return false;

        // If the delay is zero then flip the door direction. Otherwise we
        // are in the wait delay and setting the delay back to 0 will
        // immediately bring it back down. Either way we need to set delay
        // to 0, because this effect needs to work immediately.
        if (DelayTics == 0)
            FlipMovementDirection(false);
        DelayTics = 0;
        return true;
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

        if (m_direction == MoveData.StartDirection && SoundData.StartSound != null)
            m_playedStartSound = false;

        if (m_crushing)
            m_crushing = false;

        m_speed = m_direction == MoveData.StartDirection ? m_startSpeed : m_returnSpeed;

        if (MoveData.MoveRepetition == MoveRepetition.PerpetualPause)
            IsPaused = true;
    }

    private double CalculateDestination()
    {
        double destZ = SectorPlane.Z + m_speed;

        if ((MoveData.Flags & SectorMoveFlags.Door) != 0 && Sector.Floor.Z != m_minZ)
            UpdateFloorDest();

        if (m_direction == MoveDirection.Down && destZ < DestZ)
            destZ = m_direction == MoveData.StartDirection ? DestZ : m_startZ;
        else if (m_direction == MoveDirection.Up && destZ > DestZ)
            destZ = m_direction == MoveData.StartDirection ? DestZ : m_startZ;

        return destZ;
    }

    private void UpdateFloorDest()
    {
        m_startZ = Sector.Floor.Z;
        m_minZ = Math.Min(m_startZ, DestZ);
        m_maxZ = Math.Max(m_startZ, DestZ);

        if (m_direction != MoveData.StartDirection)
            DestZ = m_startZ;
    }

    private void PerformAndHandleMoveZ(double destZ)
    {
        MoveStatus = m_world.MoveSectorZ(m_speed, destZ, this);

        switch (MoveStatus)
        {
            case SectorMoveStatus.Blocked:
                if (MoveData.MoveRepetition != MoveRepetition.None)
                    FlipMovementDirection(true);
                break;

            case SectorMoveStatus.Crush when IsInitCrush:
                SetSectorDataChange();
                m_crushing = true;
                if (MoveData.Crush != null && MoveData.Crush.Value.CrushMode == ZDoomCrushMode.DoomWithSlowDown)
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
        SectorPlane.SetSectorMoveChanged(m_world.Gametick);
        if (MoveData.SectorMoveType == SectorPlaneFace.Floor)
            Sector.DataChanges |= SectorDataTypes.FloorZ;
        else
            Sector.DataChanges |= SectorDataTypes.CeilingZ;
    }

    private bool IsNonRepeat => MoveData.MoveRepetition == MoveRepetition.None || MoveData.MoveRepetition == MoveRepetition.ReturnOnBlock;
    private bool IsDelayReturn => MoveData.MoveRepetition == MoveRepetition.DelayReturn;
    private bool IsInitCrush => MoveData.Crush != null && m_direction == MoveData.StartDirection && !m_crushing;

    public override bool Equals(object? obj)
    {
        if (obj is not SectorMoveSpecial moveSpecial)
            return false;

        bool? crushDataEquals = moveSpecial.MoveData.Crush?.Equals(MoveData.Crush);
        if (crushDataEquals == null)
            crushDataEquals = true;

        bool? damageSpecialEquals = moveSpecial.MoveData.DamageSpecial?.Equals(MoveData.DamageSpecial);
        if (damageSpecialEquals == null)
            damageSpecialEquals = true;

        return crushDataEquals.Value &&
            damageSpecialEquals.Value &&
            moveSpecial.Sector.Id == Sector.Id &&
            moveSpecial.MoveData.SectorMoveType == MoveData.SectorMoveType &&
            moveSpecial.MoveData.MoveRepetition == MoveData.MoveRepetition &&
            moveSpecial.MoveData.Speed == MoveData.Speed &&
            moveSpecial.MoveData.ReturnSpeed == MoveData.ReturnSpeed &&
            moveSpecial.MoveData.Delay == MoveData.Delay &&
            moveSpecial.MoveData.FloorChangeTextureHandle == MoveData.FloorChangeTextureHandle &&
            moveSpecial.MoveData.CeilingChangeTextureHandle == MoveData.CeilingChangeTextureHandle &&
            moveSpecial.MoveData.StartDirection == MoveData.StartDirection &&
            moveSpecial.MoveData.Flags == MoveData.Flags &&
            moveSpecial.SoundData.Equals(SoundData) &&
            moveSpecial.SectorPlane.Facing == SectorPlane.Facing &&
            moveSpecial.IsPaused == IsPaused &&
            moveSpecial.MoveDirection == MoveDirection &&
            moveSpecial.DelayTics == DelayTics &&
            moveSpecial.MoveSpeed == MoveSpeed &&
            moveSpecial.IsCrushing == IsCrushing &&
            moveSpecial.StartClipped == moveSpecial.StartClipped;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
