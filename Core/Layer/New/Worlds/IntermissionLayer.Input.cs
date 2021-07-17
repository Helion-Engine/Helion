using System;
using Helion.Input;
using Helion.Util;

namespace Helion.Layer.New.Worlds
{
    public partial class IntermissionLayerNew
    {
        public void HandleInput(InputEvent input)
        {
            if (IntermissionState == IntermissionState.Complete)
                return;

            bool pressedKey = input.HasAnyKeyPressed();
            input.ConsumeAll();
            
            if (pressedKey)
            {
                AdvanceToNextStateForcefully();
                PlayPressedKeySound();
            }
        }
        
        private void SetMaxStats()
        {
            m_levelPercents.KillCount = m_levelPercents.TotalMonsters;
            m_levelPercents.ItemCount = m_levelPercents.TotalItems;
            m_levelPercents.SecretCount = m_levelPercents.TotalSecrets;
            LevelTimeSeconds = m_totalLevelTime;
            ParTimeSeconds = CurrentMapInfo.ParTime;
        }
        
        private void AdvanceToNextStateForcefully()
        {
            if (IntermissionState == IntermissionState.ShowAllStats && NextMapInfo == null)
            {
                IntermissionState = IntermissionState.Complete;
                return;
            }

            if (m_delayStateTics != 0)
            {
                m_delayStateTics = 0;
                IntermissionState = m_delayState;
                m_delayState = IntermissionState.None;
            }

            if (IntermissionState < IntermissionState.ShowAllStats)
                SetMaxStats();

            IntermissionState = IntermissionState switch
            {
                IntermissionState.Started => IntermissionState.ShowAllStats,
                IntermissionState.TallyingKills => IntermissionState.ShowAllStats,
                IntermissionState.TallyingItems => IntermissionState.ShowAllStats,
                IntermissionState.TallyingSecrets => IntermissionState.ShowAllStats,
                IntermissionState.TallyingTime => IntermissionState.ShowAllStats,
                IntermissionState.ShowAllStats => IntermissionState.NextMap,
                IntermissionState.NextMap => IntermissionState.NextMap,
                IntermissionState.Complete => IntermissionState.Complete,
                _ => throw new Exception($"Unexpected intermission state: {IntermissionState}")
            };

            if (IntermissionState == IntermissionState.NextMap)
            {
                m_delayStateTics = (int)Constants.TicksPerSecond * 4;
                m_delayState = IntermissionState.Complete;
            }
        }
        
        private void PlayPressedKeySound()
        {
            switch (IntermissionState)
            {
            case IntermissionState.Started:
                break;
            case IntermissionState.TallyingKills:
            case IntermissionState.TallyingItems:
            case IntermissionState.TallyingSecrets:
            case IntermissionState.TallyingTime:
            case IntermissionState.ShowAllStats:
                m_soundManager.PlayStaticSound("intermission/nextstage");
                break;
            case IntermissionState.NextMap:
                m_soundManager.PlayStaticSound("intermission/paststats");
                break;
            case IntermissionState.Complete:
                break;
            default:
                throw new HelionException($"Unknown intermission state: {IntermissionState}");
            }
        }
    }
}
