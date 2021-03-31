using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Layer;
using Helion.Layer.WorldLayers;
using Helion.Maps;
using Helion.Models;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util.Consoles;
using Helion.Util.Extensions;
using Helion.World;
using Helion.World.Cheats;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Save;
using Helion.World.Util;

namespace Helion.Client
{
    public partial class Client
    {
        private static readonly IList<Player> NoPlayers = Array.Empty<Player>();

        private void Console_OnCommand(object? sender, ConsoleCommandEventArgs ccmdArgs)
        {
            switch (ccmdArgs.Command.ToUpper())
            {
                case "EXIT":
                    m_window.Close();
                    break;
                
                case "LOADGAME":
                    HandleLoadGame(ccmdArgs.Args);
                    break;

                case "MAP":
                    HandleMap(ccmdArgs.Args);
                    break;
                
                case "STARTGAME":
                    StartNewGame();
                    break;

                case "VOLUME":
                    SetVolume(ccmdArgs.Args);
                    break;

                case "AUDIODEVICE":
                    HandleAudioDevice(ccmdArgs.Args);
                    break;

                case "AUDIODEVICES":
                    PrintAudioDevices();
                    break;

                default:
                    HandleDefault(ccmdArgs);
                    break;
            }
        }

        private void HandleDefault(ConsoleCommandEventArgs ccmdArgs)
        {
            if (!m_layerManager.TryGetLayer(out SinglePlayerWorldLayer? layer) || layer.World.EntityManager.Players.Count == 0)
                return;
            
            if (!CheatManager.Instance.HandleCommand(layer.World.EntityManager.Players[0], ccmdArgs.Command))
                Log.Info($"Unknown command: {ccmdArgs.Command}");
        }   

        private void HandleLoadGame(IReadOnlyList<string> args)
        {
            if (args.Empty())
            {
                Log.Info("Usage: loadfile <filename>");
                Log.Info("Example: loadfile savegame2");
                return;
            }
            
            string fileName = args[0];
            SaveGame saveGame = new SaveGame(fileName);

            if (saveGame.Model == null)
            {
                LogError("Corrupt save game.");
                return;
            }

            WorldModel? worldModel = saveGame.ReadWorldModel();
            if (worldModel == null)
            {
                LogError("Corrupt world.");
                return;
            }

            if (!ModelVerification.VerifyModelFiles(worldModel.Files, m_archiveCollection, Log))
            {
                ShowConsole();
                return;
            }

            LoadMap(GetMapInfo(worldModel.MapName), worldModel, NoPlayers);
        }

        private void StartNewGame()
        {
            MapInfoDef? mapInfoDef = GetDefaultMap();
            if (mapInfoDef == null)
            {
                LogError("Unable to find default map for game to start on");
                return;
            }
            
            LoadMap(mapInfoDef.MapName);
        }

        private void PrintAudioDevices()
        {
            int num = 1;
            foreach (string device in m_audioSystem.GetDeviceNames())
                Log.Info($"{num++}. {device}");
        }

        private void HandleAudioDevice(IList<string> args)
        {
            if (args.Empty())
            {
                Log.Info(m_audioSystem.GetDeviceName());
                return;
            }

            if (!int.TryParse(args[0], out int deviceIndex))
                return;

            deviceIndex--;
            List<string> deviceNames = m_audioSystem.GetDeviceNames().ToList();
            if (deviceIndex < 0 || deviceIndex >= deviceNames.Count)
                return;

            SetAudioDevice(deviceNames[deviceIndex]);
        }

        private void SetAudioDevice(string deviceName)
        {
            m_config.Audio.Device.Set(deviceName);
            m_audioSystem.SetDevice(deviceName);
            m_audioSystem.SetVolume(m_config.Audio.Volume);
        }

        private void SetVolume(IList<string> args)
        {
            if (args.Empty() || !float.TryParse(args[0], out float volume))
            {
                Log.Info("Usage: volume <volume>");
                return;
            }

            m_config.Audio.Volume.Set(volume);
            m_audioSystem.SetVolume(volume);
        }

        private void HandleMap(IList<string> args)
        {
            if (args.Empty())
            {
                Log.Info("Usage: map <mapName>");
                return;
            }

            LoadMap(GetMapInfo(args[0]), null, NoPlayers);    
        }

        private MapInfoDef GetMapInfo(string mapName) =>
            m_archiveCollection.Definitions.MapInfoDefinition.MapInfo.GetMapInfoOrDefault(mapName);

        private void LoadMap(MapInfoDef mapInfoDef, WorldModel? worldModel, IList<Player> players)
        {
            IMap? map = m_archiveCollection.FindMap(mapInfoDef.MapName);
            if (map == null)
            {
                LogError($"Cannot load map '{mapInfoDef.MapName}', it cannot be found or is corrupt");
                return;
            }

            if (worldModel != null)
                m_config.Game.Skill.Set(worldModel.Skill);

            SkillDef? skillDef = m_archiveCollection.Definitions.MapInfoDefinition.MapInfo.GetSkill(m_config.Game.Skill);
            if (skillDef == null)
            {
                LogError($"Could not find skill definition for {m_config.Game.Skill}");
                return;
            }

            m_layerManager.Remove<SinglePlayerWorldLayer>();
            m_layerManager.PruneDisposed();

            SinglePlayerWorldLayer? newLayer = SinglePlayerWorldLayer.Create(m_layerManager, m_config, m_console,
                m_audioSystem, m_archiveCollection, mapInfoDef, skillDef, map, players.FirstOrDefault(), worldModel);
            if (newLayer == null)
                return;

            newLayer.World.LevelExit += World_LevelExit;
            m_layerManager.Add(newLayer);
            newLayer.World.Start();

            m_layerManager.RemoveAllBut<WorldLayer>();
        }

        private void World_LevelExit(object? sender, LevelChangeEvent e)
        {
            if (sender is not IWorld world)
                return;

            if (world is not SinglePlayerWorld singlePlayerWorld)
            {
                Log.Error("Currently only support single player worlds");
                return;
            }

            switch (e.ChangeType)
            {
                case LevelChangeType.Next:
                    TransitionToLevel(GetNextLevel(world.MapInfo));
                    break;
                    
                case LevelChangeType.SecretNext:
                    TransitionToLevel(GetNextSecretLevel(world.MapInfo));
                    break;
                
                case LevelChangeType.SpecificLevel:
                    ChangeLevel(e);
                    break;
                    
                case LevelChangeType.Reset:
                    LoadMap(world.MapInfo, null, NoPlayers);
                    break;
            }
            
            void TransitionToLevel(MapInfoDef? nextMapInfo)
            {
                if (nextMapInfo == null)
                {
                    Log.Error("Unable to find next map to go to");
                    return;
                }
                
                bool isChangingClusters = world.MapInfo.Cluster != nextMapInfo.Cluster;
                ClusterDef? cluster = m_archiveCollection.Definitions.MapInfoDefinition.MapInfo.GetCluster(world.MapInfo.Cluster);

                if (isChangingClusters && cluster != null && !cluster.AllowIntermission)
                {
                    EndGameLayer endGameLayer = new(m_archiveCollection, m_audioSystem.Music, cluster, GoToNextLevel);
                    m_layerManager.Add(endGameLayer);
                }
                else
                {
                    bool hasEndGame = isChangingClusters && cluster != null && cluster.AllowIntermission;
                    ClusterDef? clusterToUse = hasEndGame ? cluster : null;
                    IntermissionLayer intermissionLayer = new(world, m_soundManager, m_audioSystem.Music, singlePlayerWorld.Player, 
                        singlePlayerWorld.MapInfo, nextMapInfo, clusterToUse, GoToNextLevel);
                    m_layerManager.Add(intermissionLayer);
                }
                
                // TODO: Refactor this later so we're not passing around lambdas.
                void GoToNextLevel() => LoadMap(nextMapInfo, null, world.EntityManager.Players);
            }
        }

        private void ChangeLevel(LevelChangeEvent e)
        {
            if (MapWarp.GetMap(e.LevelNumber, m_archiveCollection.Definitions.MapInfoDefinition.MapInfo,
                out MapInfoDef? mapInfoDef) && mapInfoDef != null)
                LoadMap(mapInfoDef, null, NoPlayers);
        }

        private MapInfoDef? GetNextLevel(MapInfoDef mapDef) => 
            m_archiveCollection.Definitions.MapInfoDefinition.MapInfo.GetNextMap(mapDef);

        private MapInfoDef? GetNextSecretLevel(MapInfoDef mapDef) => 
            m_archiveCollection.Definitions.MapInfoDefinition.MapInfo.GetNextSecretMap(mapDef);

        private void ShowConsole()
        {
            if (m_layerManager.Get<ConsoleLayer>() == null)
                m_layerManager.Add(new ConsoleLayer(m_archiveCollection, m_console));
        }

        private void LogError(string error)
        {
            Log.Error(error);
            ShowConsole();
        }
    }
}
