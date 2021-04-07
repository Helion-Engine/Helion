using System;
using System.Diagnostics;
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
using MoreLinq;
using NLog;

namespace Helion.Layer
{
    public class IntermissionLayer : GameLayer
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public readonly IWorld World;
        public double KillPercent { get; private set; }
        public double ItemPercent { get; private set; }
        public double SecretPercent{ get; private set; }
        public MapInfoDef CurrentMapInfo { get; private set; }
        public MapInfoDef? NextMapInfo { get; private set; }
        public string IntermissionPic { get; private set; }
        public IntermissionDef? IntermissionDef { get; private set; }
        public IntermissionState IntermissionState { get; private set; } = IntermissionState.Started;
        public event EventHandler? Exited;

        private readonly ArchiveCollection m_archiveCollection;
        private readonly SoundManager m_soundManager;
        private readonly IMusicPlayer m_musicPlayer;
        private readonly IntermissionDrawer m_drawer;
        private readonly Stopwatch m_stopwatch = new Stopwatch();

        private int m_tics;

        protected override double Priority => 0.65;

        public IntermissionLayer(IWorld world, SoundManager soundManager, IMusicPlayer musicPlayer, MapInfoDef currentMapInfo,
            MapInfoDef? nextMapInfo)
        {
            World = world;
            CurrentMapInfo = currentMapInfo;
            NextMapInfo = nextMapInfo;
            m_archiveCollection = world.ArchiveCollection;
            m_soundManager = soundManager;
            m_musicPlayer = musicPlayer;
            m_stopwatch.Start();

            IntermissionPic = "INTERPIC";
            CheckEpisodeIntermission();
            Tick();

            m_drawer = new IntermissionDrawer(world.ArchiveCollection, currentMapInfo, nextMapInfo, this);

            CalculatePercentages();
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
                KillPercent = (double)World.LevelStats.KillCount / World.LevelStats.TotalMonsters;
            if (World.LevelStats.TotalItems != 0)
                ItemPercent = (double)World.LevelStats.ItemCount / World.LevelStats.TotalItems;
            if (World.LevelStats.TotalSecrets != 0)
                SecretPercent = (double)World.LevelStats.SecretCount / World.LevelStats.TotalSecrets;
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

            IntermissionState = IntermissionState switch
            {
                IntermissionState.Started => IntermissionState.ShowAllStats,
                IntermissionState.TallyingKills => IntermissionState.ShowAllStats,
                IntermissionState.TallyingItems => IntermissionState.ShowAllStats,
                IntermissionState.TallyingSecrets => IntermissionState.ShowAllStats,
                IntermissionState.ShowingPar => IntermissionState.ShowAllStats,
                IntermissionState.ShowAllStats => IntermissionState.NextMap,
                IntermissionState.NextMap => IntermissionState.Complete,
                IntermissionState.Complete => IntermissionState.Complete,
                _ => throw new Exception($"Unexpected intermission state: {IntermissionState}")
            };
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
                case IntermissionState.ShowingPar:
                case IntermissionState.ShowAllStats:
                    // Playing it twice to simulate a louder sound as if multiple
                    // of the tallies are completed at once.
                    m_soundManager.PlayStaticSound("intermission/nextstage");
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

            if (IntermissionState == IntermissionState.Complete)
                Exited?.Invoke(this, EventArgs.Empty);
            
            base.HandleInput(input);
        }

        public override void RunLogic()
        {
            // TODO: Play gunshot sounds when advancing the percentages.       
            base.RunLogic();

            if (m_stopwatch.ElapsedMilliseconds > 1000 / Constants.TicksPerSecond)
            {
                m_stopwatch.Restart();
                Tick();
            }
        }

        private bool IsLeaving => IntermissionState == IntermissionState.ShowAllStats;

        private static bool CompareMapName(IntermissionAnimation animation, MapInfoDef? mapInfo)
        {
            if (mapInfo == null)
                return false;

            return animation.MapName.Equals(mapInfo.MapName, StringComparison.OrdinalIgnoreCase);
        }

        private void Tick()
        {
            m_tics++;
            if (IntermissionDef == null)
                return;

            foreach (var animation in IntermissionDef.Animations)
            {
                animation.Tic++;
                if (animation.Tic >= animation.Tics)
                {
                    switch (animation.Type)
                    {
                        case IntermissionAnimationType.IfEntering:
                            if (IsLeaving || NextMapInfo == null || !CompareMapName(animation, NextMapInfo))
                                continue;
                            break;

                        case IntermissionAnimationType.IfLeaving:
                            if (!IsLeaving || !CompareMapName(animation, CurrentMapInfo))
                                continue;
                            break;

                        case IntermissionAnimationType.IfVisited:
                            // TODO waiting for this to be implemented
                            continue;

                        default:
                            break;
                    }

                    animation.ShouldDraw = true;
                    animation.Tic = 0;
                    if (!animation.Once)
                        animation.ItemIndex = (animation.ItemIndex + 1) % animation.Items.Count;
                }
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
        Started,
        TallyingKills,
        TallyingItems,
        TallyingSecrets,
        ShowingPar,
        ShowAllStats,
        NextMap,
        Complete
    }
}
