using Helion.Geometry.Vectors;
using Helion.Resources.Definitions.Decorate.States;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.World.Entities;
using Helion.World.Entities.Definition.States;
using System;
using System.Diagnostics;

namespace Helion.Layer.EndGame;

public partial class EndGameLayer
{
    private readonly TimeSpan m_scrollTimespan = TimeSpan.FromMilliseconds(40);
    private readonly Stopwatch m_scroller = new();
    private readonly Stopwatch m_stopwatch = new();

    public void RunLogic()
    {
        UpdateScroller();

        if (!m_forceState && (m_drawState == EndGameDrawState.Complete || !m_stopwatch.IsRunning || m_stopwatch.Elapsed < m_timespan))
            return;

        if (m_drawState == EndGameDrawState.TextComplete && NextMapInfo != null)
            return;

        if (m_drawState == EndGameDrawState.TextComplete)
        {
            HandleTextComplete();
        }
        else if (m_drawState == EndGameDrawState.ImageScroll)
        {
            if (m_forceState)
            {
                m_scroller.Stop();
                m_xOffset = m_xOffsetStop;
                m_drawState = EndGameDrawState.TheEnd;
                m_theEndImageIndex = TheEndImages.Count - 1;
                return;
            }

            if (m_scroller.IsRunning)
                return;

            m_drawState++;
            m_timespan = GetPageTime();
        }
        else if (m_drawState == EndGameDrawState.TheEnd)
        {
            m_timespan = TimeSpan.FromMilliseconds(150);
            if (m_theEndImageIndex < TheEndImages.Count - 1)
            {
                m_theEndImageIndex++;
                m_soundManager.PlayStaticSound("weapons/pistol");
            }
        }
        else if (m_drawState < EndGameDrawState.Complete)
        {
            // This is cluster text and does not proceed further
            if (NextMapInfo != null)
                m_drawState = EndGameDrawState.TextComplete;
            else
                m_drawState++;
        }

        if (m_drawState == EndGameDrawState.ImageScroll)
            m_scroller.Start();

        m_forceState = false;
        m_stopwatch.Restart();
    }

    public void OnTick()
    {
        if (m_castEntity == null)
            return;

        m_castEntityFrameTicks--;
        if (m_castEntityFrameTicks > 0)
            return;

        if (m_castEntity.Frame.Ticks == -1 || m_castEntity.Frame.IsNullFrame || m_castEntity.Frame.NextFrame.IsNullFrame)
        {
            SetNextCastEntity();
            return;
        }

        m_castFrameCount++;
        bool setNewState = false;
        switch (m_castEntityState)
        {
            case CastEntityState.See:
                if (m_castFrameCount == 12)
                {
                    m_castFrameCount = 0;
                    SetCastEntityState(CastEntityState.Attack, true);
                    setNewState = true;
                }
                break;

            case CastEntityState.Attack:
                if (m_castFrameCount == 24 || m_castEntity.FrameState.IsState(Constants.FrameStates.See))
                {
                    m_castFrameCount = 0;
                    SetCastEntityState(CastEntityState.See, false);
                    setNewState = true;
                }
                break;
        }

        string sound = GetCastEntitySound();
        if (!string.IsNullOrEmpty(sound))
            m_soundManager.PlayStaticSound(sound);

        if (!setNewState)
        {
            if (m_castEntity.Frame.BranchType == ActorStateBranch.Stop)
            {
                SetNextCastEntity();
                return;
            }

            m_castEntity.FrameState.SetFrameIndex(m_castEntity.Frame.NextFrameIndex);
        }

        m_castEntityFrameTicks = m_castEntity.Frame.Ticks;
        if (m_castEntityFrameTicks == -1)
            m_castEntityFrameTicks = 15;
    }

    private void HandleTextComplete()
    {
        if (m_endGameType == EndGameType.Cast)
        {
            SetNextCastEntity();
            m_drawState = EndGameDrawState.Cast;
            PlayMusic("D_EVIL");
            return;
        }

        if (m_shouldScroll)
        {
            m_drawState++;
            m_timespan += TimeSpan.FromSeconds(2);
            PlayMusic("D_BUNNY");
        }
        else
        {
            m_drawState = EndGameDrawState.Complete;
        }
    }

    private void SetNextCastEntity()
    {
        m_castFrameCount = 0;
        m_castIndex++;
        m_castIndex %= Cast.Count;
        m_castEntity = World.EntityManager.Create(Cast[m_castIndex].DefitionName, Vec3D.Zero);        
        SetCastEntityState(CastEntityState.See, true);
    }

    private void SetCastEntityState(CastEntityState state, bool playSound)
    {
        if (m_castEntity == null)
            return;

        m_castEntityState = state;
        string sound = string.Empty;
        switch (state)
        {
            case CastEntityState.Attack:
                SetCastEntityAttackState();
                break;
            case CastEntityState.Death:
                m_castEntity.FrameState.SetFrameIndexByLabel(Constants.FrameStates.Death);
                sound = m_castEntity.Definition.Properties.DeathSound;
                if (m_castEntity.Definition.Name.EqualsIgnoreCase("DoomPlayer"))
                    sound = "player/male/death1";
                break;
            default:
                m_castEntity.FrameState.SetFrameIndexByLabel(Constants.FrameStates.See);
                sound = m_castEntity.Definition.Properties.SeeSound;
                break;
        }

        m_castEntityFrameTicks = m_castEntity.Frame.Ticks;

        if (!string.IsNullOrEmpty(sound) && playSound)
            m_soundManager.PlayStaticSound(sound);
    }

    private string GetCastEntitySound()
    {
        if (m_castEntity == null)
            return string.Empty;

        EntityFrame? frame = GetCastEntityAttackFrame();
        if (frame == null)
            return string.Empty;

        string name = m_castEntity.Definition.Name;
        int frameIndex = m_castEntity.Frame.MasterFrameIndex;
        int frameDiff = frameIndex - frame.MasterFrameIndex;
        if (name.EqualsIgnoreCase("ZombieMan") && frameDiff == 1)
            return "grunt/attack";
        if (name.EqualsIgnoreCase("ShotgunGuy") && frameDiff == 1)
            return "shotguy/attack";
        if (name.EqualsIgnoreCase("ChaingunGuy") && (frameDiff == 1 || frameDiff == 3))
            return "chainguy/attack";
        else if (name.EqualsIgnoreCase("DoomImp") && frameDiff == 2)
            return "imp/melee";
        else if (name.EqualsIgnoreCase("Demon") && frameDiff == 1)
            return "demon/melee";
        else if (name.EqualsIgnoreCase("LostSoul") && frameDiff == 1)
            return "skull/melee";
        else if (name.EqualsIgnoreCase("Cacodemon") && frameDiff == 1)
            return "caco/attack";
        else if ((name.EqualsIgnoreCase("HellKnight") || name.EqualsIgnoreCase("BaronOfHell")) && frameDiff == 1)
            return "baron/attack";
        else if (name.EqualsIgnoreCase("Arachnotron") && frameDiff == 1)
            return "baby/attack";
        else if (name.EqualsIgnoreCase("PainElemental") && frameDiff == 1)
            return "skull/melee";
        else if (name.EqualsIgnoreCase("FatSo") && (frameDiff == 1 || frameDiff == 4 || frameDiff == 7))
            return "fatso/attack";
        else if (name.EqualsIgnoreCase("Archvile") && frameDiff == 1)
            return "vile/firecrkl";
        else if (name.EqualsIgnoreCase("SpiderMastermind") && (frameDiff == 1 || frameDiff == 2))
            return "spider/attack";
        else if (name.EqualsIgnoreCase("Cyberdemon") && (frameDiff == 1 || frameDiff == 3 || frameDiff == 5))
            return "weapons/rocklf";
        else if (name.EqualsIgnoreCase("DoomPlayer") && frameDiff == 1)
            return "weapons/shotgf";
        else if (name.EqualsIgnoreCase("Revenant"))
        {
            bool melee = ShouldUseMeleeState(m_castEntity, m_castIsMelee);
            if (melee && frameDiff == 1)
                return "skeleton/swing";
            else if (melee && frameDiff == 3)
                return "skeleton/melee";
            else if (!melee && frameDiff == 1)
                return "skeleton/attack";
        }

        //  case S_SKEL_FIST2: sfx = sfx_skeswg; break;
        //case S_SKEL_FIST4: sfx = sfx_skepch; break;
        //case S_SKEL_MISS2: sfx = sfx_skeatk; break;

        return string.Empty;
    }

    private EntityFrame? GetCastEntityAttackFrame()
    {
        if (m_castEntity == null)
            return null;

        EntityFrame? frame;
        if (ShouldUseMeleeState(m_castEntity, m_castIsMelee))
            frame = m_castEntity.FrameState.GetStateFrame(Constants.FrameStates.Melee);
        else
            frame = m_castEntity.FrameState.GetStateFrame(Constants.FrameStates.Missile);

        return frame;
    }

    private void SetCastEntityAttackState()
    {
        if (m_castEntity == null)
            return;

        m_castIsMelee = m_castMelee;

        if (ShouldUseMeleeState(m_castEntity, m_castIsMelee))
            m_castEntity.FrameState.SetFrameIndexByLabel(Constants.FrameStates.Melee);
        else
            m_castEntity.FrameState.SetFrameIndexByLabel(Constants.FrameStates.Missile);

        m_castMelee = !m_castMelee;
    }

    private static bool ShouldUseMeleeState(Entity entity, bool melee)
    {
        if (melee && entity.Definition.Name.Equals("Revenant"))
            return entity.HasMeleeState();

        if (!entity.HasMissileState())
            return true;

        return false;
    }

    private void UpdateScroller()
    {
        if (m_scroller.IsRunning && m_scroller.Elapsed >= m_scrollTimespan)
        {
            m_scroller.Restart();
            m_xOffset += 1;

            if (m_xOffset >= m_xOffsetStop)
            {
                m_xOffset = m_xOffsetStop;
                m_scroller.Stop();
            }
        }
    }
}
