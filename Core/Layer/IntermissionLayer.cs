using System;
using Helion.Audio;
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

        private readonly IWorld m_world;
        private readonly ArchiveCollection m_archiveCollection;
        private readonly IAudioSystem m_audioSystem;
        private readonly Player m_player;
        private readonly MapInfoDef m_mapInfo;
        private readonly ClusterDef? m_endGameCluster;
        private readonly Action m_nextMapFunc;
        private readonly IntermissionDrawer m_drawer;
        private bool m_invokedNextMapFunc;
        private IntermissionState m_intermissionState = IntermissionState.Started;
        public double KillPercent { get; private set; }
        public double ItemPercent { get; private set; }
        public double SecretPercent{ get; private set; }

        protected override double Priority => 0.65;

        public IntermissionLayer(IWorld world, IAudioSystem audioSystem, Player player, MapInfoDef mapInfo, 
            ClusterDef? endGameCluster, Action nextMapFunc)
        {
            m_world = world;
            m_archiveCollection = world.ArchiveCollection;
            m_audioSystem = audioSystem;
            m_player = player;
            m_mapInfo = mapInfo;
            m_endGameCluster = endGameCluster;
            m_nextMapFunc = nextMapFunc;
            m_drawer = new IntermissionDrawer(world.ArchiveCollection, mapInfo);

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
            
            foreach (Entity entity in m_world.Entities)
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
            foreach (Sector sector in m_world.Sectors)
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
            string musicName = m_archiveCollection.Definitions.MapInfoDefinition.GameDefinition.IntermissionMusic;
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
            
            m_audioSystem.Music.Play(midiData);
        }

        private void AdvanceToNextState()
        {
            m_intermissionState = m_intermissionState switch
            {
                IntermissionState.Started => IntermissionState.TallyingKills,
                IntermissionState.TallyingKills => IntermissionState.TallyingItems,
                IntermissionState.TallyingItems => IntermissionState.TallyingSecrets,
                IntermissionState.TallyingSecrets => IntermissionState.ShowingPar,
                IntermissionState.ShowingPar => IntermissionState.ShowingPar,
                IntermissionState.NextMap => IntermissionState.NextMap,
                _ => throw new Exception($"Unexpected intermission: {m_intermissionState}")
            };
        }

        private void PlayPressedKeySound()
        {
            if (m_invokedNextMapFunc)
                return;

            switch (m_intermissionState)
            {
            case IntermissionState.TallyingKills:
            case IntermissionState.TallyingItems:
            case IntermissionState.TallyingSecrets:
            case IntermissionState.ShowingPar:
                // TODO: Play boom sound.
                break;
            case IntermissionState.NextMap:
                // TODO: Play shotgun sound.
                break;
            }
        }

        public override void HandleInput(InputEvent input)
        {
            bool pressedKey = input.HasAnyKeyPressed();
            input.ConsumeAll();
            
            if (pressedKey)
            {
                if (!m_invokedNextMapFunc && m_intermissionState == IntermissionState.NextMap)
                {
                    m_invokedNextMapFunc = true;
                    m_nextMapFunc();
                }
                else
                {
                    AdvanceToNextState();
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

    internal enum IntermissionState
    {
        Started,
        TallyingKills,
        TallyingItems,
        TallyingSecrets,
        ShowingPar,
        NextMap
    }
}
