using System;
using System.Linq;
using Helion.Resources.Definitions.Intermission;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.Util.Timing;

namespace Helion.Layer.Worlds;

public partial class IntermissionLayer
{
    private bool m_exited;

    public void RunLogic(TickerInfo tickerInfo)
    {
        if (m_stopwatch.ElapsedMilliseconds > 1000 / Constants.TicksPerSecond)
        {
            m_stopwatch.Restart();
            Tick();
        }
    }

    private void Tick()
    {
        if (m_exited || m_gameLayerManager.HasMenuOrConsole())
            return;

        m_tics++;
        AnimationTick();

        if (m_delayStateTics > 0)
        {
            m_delayStateTics--;
            return;
        }

        if (m_delayState != IntermissionState.None)
        {
            IntermissionState = m_delayState;
            m_delayState = IntermissionState.None;
        }

        TallyTick();

        if (IntermissionState == IntermissionState.Complete)
        {
            m_exited = true;
            Exited?.Invoke(this, EventArgs.Empty);
        }
    }

    private static bool CompareMapName(string mapName, MapInfoDef? mapInfo)
    {
        return mapInfo != null && mapName.EqualsIgnoreCase(mapInfo.MapName);
    }

    private bool VisitedMap(string mapName)
    {
        return World.GlobalData.VisitedMaps.Any(x => CompareMapName(mapName, x));
    }

    private static bool ShouldAnimate(IntermissionAnimation animation)
    {
        return !animation.Once || (animation.ItemIndex < animation.Items.Count - 1);
    }

    private void AnimationTick()
    {
        if (IntermissionDef == null)
            return;

        bool draw = true;
        foreach (var animation in IntermissionDef.Animations)
        {
            animation.Tic++;
            if (animation.Tic >= animation.Tics)
            {
                switch (animation.Type)
                {
                case IntermissionAnimationType.IfEntering:
                    draw = IsNextMap && NextMapInfo != null && CompareMapName(animation.MapName, NextMapInfo);
                    break;

                case IntermissionAnimationType.IfLeaving:
                    draw = !IsNextMap && CompareMapName(animation.MapName, CurrentMapInfo);
                    break;

                case IntermissionAnimationType.IfVisited:
                    draw = VisitedMap(animation.MapName);
                    break;
                }

                if (!draw)
                    continue;

                animation.ShouldDraw = true;
                animation.Tic = 0;
                if (ShouldAnimate(animation))
                    animation.ItemIndex = (animation.ItemIndex + 1) % animation.Items.Count;
            }
        }
    }

    private void AdvanceTally()
    {
        m_delayState = IntermissionState switch
        {
            IntermissionState.Started => IntermissionState.TallyingKills,
            IntermissionState.TallyingKills => IntermissionState.TallyingItems,
            IntermissionState.TallyingItems => IntermissionState.TallyingSecrets,
            IntermissionState.TallyingSecrets => IntermissionState.TallyingTime,
            IntermissionState.TallyingTime => IntermissionState.ShowAllStats,
            _ => IntermissionState
        };

        if (m_delayState != IntermissionState.ShowAllStats)
            m_delayStateTics = (int)Constants.TicksPerSecond;
        m_soundManager.PlayStaticSound("intermission/nextstage");
    }

    private void TallyTick()
    {
        if (IntermissionState != IntermissionState.TallyingKills && IntermissionState != IntermissionState.TallyingItems &&
            IntermissionState != IntermissionState.TallyingSecrets && IntermissionState != IntermissionState.TallyingTime)
            return;

        if ((m_tics & 3) == 0)
            m_soundManager.PlayStaticSound("intermission/tick");

        switch (IntermissionState)
        {
        case IntermissionState.TallyingKills:
            m_levelPercents.KillCount = Math.Clamp(m_levelPercents.KillCount + StatAddAmount, 0, m_levelPercents.TotalMonsters);
            if (SkipTallyCount || m_levelPercents.KillCount >= m_levelPercents.TotalMonsters)
                AdvanceTally();
            break;

        case IntermissionState.TallyingItems:
            m_levelPercents.ItemCount = Math.Clamp(m_levelPercents.ItemCount + StatAddAmount, 0, m_levelPercents.TotalItems);
            if (SkipTallyCount || m_levelPercents.ItemCount >= m_levelPercents.TotalItems)
                AdvanceTally();
            break;

        case IntermissionState.TallyingSecrets:
            m_levelPercents.SecretCount = Math.Clamp(m_levelPercents.SecretCount + StatAddAmount, 0, m_levelPercents.TotalSecrets);
            if (SkipTallyCount || m_levelPercents.SecretCount >= m_levelPercents.TotalSecrets)
                AdvanceTally();
            break;

        case IntermissionState.TallyingTime:
            LevelTimeSeconds = Math.Clamp(LevelTimeSeconds + TimeAddAmount, 0, m_totalLevelTime);
            ParTimeSeconds = Math.Clamp(ParTimeSeconds + TimeAddAmount, 0, CurrentMapInfo.ParTime);
            if (SkipTallyCount || LevelTimeSeconds >= m_totalLevelTime && ParTimeSeconds >= CurrentMapInfo.ParTime)
                AdvanceTally();
            break;
        }
    }

    private bool SkipTallyCount => !m_archiveCollection.GameInfo.IntermissionCounter;
}
