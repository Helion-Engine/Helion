using System;
using Helion.Audio;
using Helion.Audio.Sounds;
using Helion.Input;
using Helion.Maps.Specials.ZDoom;
using Helion.Render.Commands;
using Helion.Render.Shared.Drawers;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util.Sounds.Mus;
using Helion.World;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Sectors;
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
        public IntermissionState IntermissionState { get; private set; } = IntermissionState.Started;
        private readonly ArchiveCollection m_archiveCollection;
        private readonly SoundManager m_soundManager;
        private readonly IMusicPlayer m_musicPlayer;
        private readonly Player m_player;
        private readonly MapInfoDef m_currentMapInfo;
        private readonly MapInfoDef m_nextMapInfo;
        private readonly ClusterDef? m_endGameCluster;
        private readonly Action m_nextMapFunc;
        private readonly IntermissionDrawer m_drawer;
        private bool m_invokedNextMapFunc;

        protected override double Priority => 0.65;

        public IntermissionLayer(IWorld world, SoundManager soundManager, IMusicPlayer musicPlayer, Player player, MapInfoDef currentMapInfo,
            MapInfoDef nextMapInfo, ClusterDef? endGameCluster, Action nextMapFunc)
        {
            World = world;
            m_archiveCollection = world.ArchiveCollection;
            m_soundManager = soundManager;
            m_musicPlayer = musicPlayer;
            m_currentMapInfo = currentMapInfo;
            m_nextMapInfo = nextMapInfo;
            m_player = player;
            m_endGameCluster = endGameCluster;
            m_nextMapFunc = nextMapFunc;
            m_drawer = new IntermissionDrawer(world.ArchiveCollection, currentMapInfo, nextMapInfo);

            CalculatePercentages();
            PlayIntermissionMusic();
        }

        private void CalculatePercentages()
        {
            int totalMonsters = 0;
            int monstersDead = 0;
            int totalItems = 0; // TODO: Get from the world; have the world cache at beginning.
            int itemsPickedUp = 0;
            int totalSecrets = m_player.SecretsFound;
            int secretsVisited = m_player.SecretsFound;
            
            foreach (Entity entity in World.Entities)
            {
                if (entity.Definition.Flags.IsMonster)
                {
                    totalMonsters++;
                    if (entity.IsDead)
                        monstersDead++;
                }

                if (entity.Flags.Pickup)
                    totalItems++;
            }
            
            // Since secrets have their sector type turned off, any secrets
            // encountered means the player has not touched them.
            foreach (Sector sector in World.Sectors)
                if (sector.SectorSpecialType == ZDoomSectorSpecialType.Secret)
                    totalSecrets++;

            if (totalMonsters != 0)
                KillPercent = (double)monstersDead / totalMonsters;
            if (totalItems != 0)
                ItemPercent = (double)itemsPickedUp / totalItems;
            if (totalSecrets != 0)
                SecretPercent = (double)secretsVisited / totalSecrets;
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
            IntermissionState = IntermissionState switch
            {
                IntermissionState.Started => IntermissionState.ShowAllStats,
                IntermissionState.TallyingKills => IntermissionState.ShowAllStats,
                IntermissionState.TallyingItems => IntermissionState.ShowAllStats,
                IntermissionState.TallyingSecrets => IntermissionState.ShowAllStats,
                IntermissionState.ShowingPar => IntermissionState.ShowAllStats,
                IntermissionState.ShowAllStats => IntermissionState.NextMap,
                IntermissionState.NextMap => IntermissionState.NextMap,
                _ => throw new Exception($"Unexpected intermission state: {IntermissionState}")
            };
        }

        private void PlayPressedKeySound()
        {
            if (m_invokedNextMapFunc)
                return;

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
                m_soundManager.PlayStaticSound("intermission/nextstage");
                break;
            default:
                throw new Exception($"Unknown intermission state: {IntermissionState}");
            }
        }

        public override void HandleInput(InputEvent input)
        {
            bool pressedKey = input.HasAnyKeyPressed();
            input.ConsumeAll();
            
            if (pressedKey)
            {
                if (!m_invokedNextMapFunc && IntermissionState == IntermissionState.NextMap)
                {
                    m_invokedNextMapFunc = true;
                    m_nextMapFunc();
                }
                else
                {
                    Console.WriteLine("Going to next state!");
                    AdvanceToNextStateForcefully();
                }

                PlayPressedKeySound();
            }
            
            base.HandleInput(input);
        }

        public override void RunLogic()
        {
            // TODO: Play gunshot sounds when advancing the percentages.
            
            base.RunLogic();
        }

        public override void Render(RenderCommands renderCommands)
        {
            m_drawer.Draw(this, renderCommands);
            
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
        NextMap
    }
}
