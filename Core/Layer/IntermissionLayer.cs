using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Helion.Audio;
using Helion.Audio.Sounds;
using Helion.Input;
using Helion.Render.Commands;
using Helion.Render.Shared.Drawers;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Intermission;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.Util.Parser;
using Helion.Util.Sounds.Mus;
using Helion.World;
using Helion.World.Stats;
using MoreLinq;
using NLog;

namespace Helion.Layer
{
    public class IntermissionLayer : GameLayer
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public readonly IWorld World;
        public double KillPercent => m_levelPercents.KillCount;
        public double ItemPercent => m_levelPercents.ItemCount;
        public double SecretPercent => m_levelPercents.SecretCount;
        public int LevelTimeSeconds { get; private set; }
        public int ParTimeSeconds { get; private set; }
        public MapInfoDef CurrentMapInfo { get; private set; }
        public MapInfoDef? NextMapInfo { get; private set; }
        public string IntermissionPic { get; private set; }
        public IntermissionDef? IntermissionDef { get; private set; }
        public IntermissionState IntermissionState { get; private set; } = IntermissionState.Started;
        public event EventHandler? Exited;

        private const int StatAddAmount = 2;
        private const int TimeAddAmount = 3;

        private readonly ArchiveCollection m_archiveCollection;
        private readonly SoundManager m_soundManager;
        private readonly IMusicPlayer m_musicPlayer;
        private readonly IntermissionDrawer m_drawer;
        private readonly Stopwatch m_stopwatch = new Stopwatch();
        private readonly LevelStats m_levelPercents = new LevelStats();
        private readonly int m_totalLevelTime;

        private IntermissionState m_delayState;
        private int m_tics;
        private int m_delayStateTics;

        protected override double Priority => 0.65;

        public IntermissionLayer(IWorld world, SoundManager soundManager, IMusicPlayer musicPlayer, 
            MapInfoDef currentMapInfo, MapInfoDef? nextMapInfo)
        {
            World = world;
            CurrentMapInfo = currentMapInfo;
            NextMapInfo = nextMapInfo;
            m_archiveCollection = world.ArchiveCollection;
            m_soundManager = soundManager;
            m_musicPlayer = musicPlayer;
            m_stopwatch.Start();
            m_totalLevelTime = World.LevelTime / (int)Constants.TicksPerSecond;

            IntermissionPic = "INTERPIC";
            CalculatePercentages();
            CheckEpisodeIntermission();
            Tick();

            m_delayStateTics = (int)Constants.TicksPerSecond;
            m_delayState = IntermissionState.TallyingKills;

            m_drawer = new IntermissionDrawer(world, currentMapInfo, nextMapInfo, this);

            PlayIntermissionMusic();
        }

        private void CheckEpisodeIntermission()
        {
            if (!Regex.IsMatch(CurrentMapInfo.MapName, @"E\dM\d"))
                return;

            string fileName = $"in_epi{CurrentMapInfo.MapName[1]}";
            var entry = m_archiveCollection.Entries.FindByName(fileName);
            if (entry == null)
                return;

            try
            {
                IntermissionDef = IntermissionParser.Parse(entry.ReadDataAsString());
                IntermissionPic = IntermissionDef.Background;

                IntermissionDef.Animations.ForEach(x => x.ShouldDraw = x.Type == IntermissionAnimationType.Normal);
            }
            catch (ParserException e)
            {
                Log.Error(e);
            }
        }

        private void CalculatePercentages()
        {
            if (World.LevelStats.TotalMonsters != 0)
                m_levelPercents.TotalMonsters = (World.LevelStats.KillCount * 100) / World.LevelStats.TotalMonsters;
            if (World.LevelStats.TotalItems != 0)
                m_levelPercents.TotalItems = (World.LevelStats.ItemCount * 100) / World.LevelStats.TotalItems;
            if (World.LevelStats.TotalSecrets != 0)
                m_levelPercents.TotalSecrets = (World.LevelStats.SecretCount * 100) / World.LevelStats.TotalSecrets;
        }

        private void PlayIntermissionMusic()
        {
            m_musicPlayer.Stop();

            string musicName = m_archiveCollection.Definitions.MapInfoDefinition.GameDefinition.IntermissionMusic;
            musicName = m_archiveCollection.Definitions.Language.GetDefaultMessage(musicName);
            
            Entry? entry = m_archiveCollection.Entries.FindByName(musicName);
            if (entry == null)
            {
                Log.Error($"Cannot find intermission music: {musicName}");
                return;
            }

            byte[] data = entry.ReadData();
            byte[]? midiData = MusToMidi.Convert(data);
            if (midiData == null)
            {
                Log.Error($"Unable to convert {musicName} from MUS to MIDI, data is corrupt");
                return; 
            }
            
            m_musicPlayer.Play(midiData);
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

        private void SetMaxStats()
        {
            m_levelPercents.KillCount = m_levelPercents.TotalMonsters;
            m_levelPercents.ItemCount = m_levelPercents.TotalItems;
            m_levelPercents.SecretCount = m_levelPercents.TotalSecrets;
            LevelTimeSeconds = m_totalLevelTime;
            ParTimeSeconds = CurrentMapInfo.ParTime;
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

        public override void HandleInput(InputEvent input)
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
            
            base.HandleInput(input);
        }

        public override void RunLogic()
        {    
            base.RunLogic();

            if (m_stopwatch.ElapsedMilliseconds > 1000 / Constants.TicksPerSecond)
            {
                m_stopwatch.Restart();
                Tick();
            }
        }

        private bool IsNextMap => IntermissionState == IntermissionState.NextMap;

        private static bool CompareMapName(string mapName, MapInfoDef? mapInfo)
        {
            if (mapInfo == null)
                return false;

            return mapName.Equals(mapInfo.MapName, StringComparison.OrdinalIgnoreCase);
        }

        private bool VisitedMap(string mapName) =>
            World.GlobalData.VisitedMaps.Any(x => CompareMapName(mapName, x));

        private static bool ShouldAnimate(IntermissionAnimation animation) =>
            !animation.Once || (animation.ItemIndex < animation.Items.Count - 1);

        private void Tick()
        {
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
                Exited?.Invoke(this, EventArgs.Empty);
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

                        default:
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
                    if (m_levelPercents.KillCount >= m_levelPercents.TotalMonsters)
                        AdvanceTally();
                    break;

                case IntermissionState.TallyingItems:
                    m_levelPercents.ItemCount = Math.Clamp(m_levelPercents.ItemCount + StatAddAmount, 0, m_levelPercents.TotalItems);
                    if (m_levelPercents.ItemCount >= m_levelPercents.TotalItems)
                        AdvanceTally();
                    break;

                case IntermissionState.TallyingSecrets:
                    m_levelPercents.SecretCount = Math.Clamp(m_levelPercents.SecretCount + StatAddAmount, 0, m_levelPercents.TotalSecrets);
                    if (m_levelPercents.SecretCount >= m_levelPercents.TotalSecrets)
                        AdvanceTally();
                    break;

                case IntermissionState.TallyingTime:
                    LevelTimeSeconds = Math.Clamp(LevelTimeSeconds + TimeAddAmount, 0, m_totalLevelTime);
                    ParTimeSeconds = Math.Clamp(ParTimeSeconds + TimeAddAmount, 0, CurrentMapInfo.ParTime);
                    if (LevelTimeSeconds >= m_totalLevelTime && ParTimeSeconds >= CurrentMapInfo.ParTime)
                        AdvanceTally();
                    break;
            }
        }

        public override void Render(RenderCommands renderCommands)
        {
            m_drawer.Draw(this, renderCommands, m_tics);
            
            base.Render(renderCommands);
        }
    }

    public enum IntermissionState
    {
        None,
        Started,
        TallyingKills,
        TallyingItems,
        TallyingSecrets,
        TallyingTime,
        ShowAllStats,
        NextMap,
        Complete
    }
}
