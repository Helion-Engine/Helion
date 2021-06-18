using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Helion.Layer;
using Helion.Layer.WorldLayers;
using Helion.Maps;
using Helion.Models;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.Util.Consoles;
using Helion.Util.Extensions;
using Helion.Util.Parser;
using Helion.World;
using Helion.World.Cheats;
using Helion.World.Entities.Players;
using Helion.World.Save;
using Helion.World.Util;

namespace Helion.Client
{
    public partial class Client
    {
        private static readonly IList<Player> NoPlayers = Array.Empty<Player>();
        private static readonly string StatFile = "levelstat.txt";

        private GlobalData m_globalData = new();

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

                case "SOUNDVOLUME":
                    SetSoundVolume(ccmdArgs.Args);
                    break;

                case "MUSICVOLUME":
                    SetMusicVolume(ccmdArgs.Args);
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

            NewGame(mapInfoDef);
        }

        private void NewGame(MapInfoDef mapInfo)
        {
            m_globalData = new();
            LoadMap(mapInfo, null, NoPlayers);
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

        private void SetSoundVolume(IList<string> args)
        {
            if (args.Empty() || !SimpleParser.TryParseFloat(args[0], out float volume))
            {
                Log.Info("Usage: soundvolume <volume>");
                return;
            }

            m_config.Audio.SoundVolume.Set(volume);
            m_audioSystem.SetVolume(volume);
        }

        private void SetMusicVolume(IList<string> args)
        {
            if (args.Empty() || !SimpleParser.TryParseFloat(args[0], out float volume))
            {
                Log.Info("Usage: musicvolume <volume>");
                return;
            }

            m_config.Audio.MusicVolume.Set(volume);
            m_audioSystem.Music.SetVolume(volume);
        }

        private void HandleMap(IList<string> args)
        {
            if (args.Empty())
            {
                Log.Info("Usage: map <mapName>");
                return;
            }

            NewGame(GetMapInfo(args[0]));    
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

            if (!m_config.Developer.InternalBSPBuilder && !RunZdbsp(map, mapInfoDef.MapName, out map))
            {
                Log.Error("Failed to run zdbsp.");
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

            if (map == null)
            {
                LogError($"Cannot load map '{mapInfoDef.MapName}', it cannot be found or is corrupt");
                return;
            }

            SinglePlayerWorldLayer? newLayer = SinglePlayerWorldLayer.Create(m_layerManager, m_globalData, m_config, m_console,
                m_audioSystem, m_archiveCollection, mapInfoDef, skillDef, map, players.FirstOrDefault(), worldModel);
            if (newLayer == null)
                return;

            if (!m_globalData.VisitedMaps.Contains(mapInfoDef))
                m_globalData.VisitedMaps.Add(mapInfoDef);
            newLayer.World.LevelExit += World_LevelExit;

            m_layerManager.Add(newLayer);
            m_layerManager.RemoveAllBut<WorldLayer>();

            newLayer.World.Start(worldModel);
        }     

        private void World_LevelExit(object? sender, LevelChangeEvent e)
        {
            if (sender is not IWorld world)
                return;

            if (m_config.Game.LevelStat)
                WriteStatsFile(world);

            switch (e.ChangeType)
            {
                case LevelChangeType.Next:
                    Intermission(world, GetNextLevel(world.MapInfo));
                    break;
                    
                case LevelChangeType.SecretNext:
                    Intermission(world, GetNextSecretLevel(world.MapInfo));
                    break;
                
                case LevelChangeType.SpecificLevel:
                    ChangeLevel(e);
                    break;
                    
                case LevelChangeType.Reset:
                    LoadMap(world.MapInfo, null, NoPlayers);
                    break;
            }
        }

        private static void ClearStatsFile()
        {
            try
            {
                File.WriteAllText(StatFile, string.Empty);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to clear {StatFile} - {e}");
            }
        }

        private static void WriteStatsFile(IWorld world)
        {
            try
            {
                TimeSpan ts = TimeSpan.FromSeconds(world.LevelTime / Constants.TicksPerSecond);
                using StreamWriter sw = File.AppendText(StatFile);
                sw.WriteLine(string.Format("{0} - {1} ({2})  K: {3}/{4}  I: {5}/{6}  S: {7}/{8}", world.MapInfo.MapName,
                    $"{ts.Minutes}:{ts.Seconds}.{ts.Milliseconds}", $"{ts.Minutes}:{ts.Seconds}",
                    world.LevelStats.KillCount, world.LevelStats.TotalMonsters,
                    world.LevelStats.ItemCount, world.LevelStats.TotalItems,
                    world.LevelStats.SecretCount, world.LevelStats.TotalSecrets));
            }
            catch (Exception e)
            {
                Log.Error($"Failed to write {StatFile} - {e}");
            }
        }

        private void Intermission(IWorld world, MapInfoDef? nextMapInfo)
        {
            if (world.MapInfo.HasOption(MapOptions.NoIntermission))
            {
                EndGame(world, nextMapInfo);
            }
            else
            {
                IntermissionLayer intermissionLayer = new(world, m_soundManager, m_audioSystem.Music,
                    world.MapInfo, nextMapInfo);
                intermissionLayer.Exited += IntermissionLayer_Exited;
                m_layerManager.Add(intermissionLayer);
            }
        }

        private void IntermissionLayer_Exited(object? sender, EventArgs e)
        {
            if (sender is not IntermissionLayer intermissionLayer)
                return;
            m_layerManager.Remove<IntermissionLayer>();
            EndGame(intermissionLayer.World, intermissionLayer.NextMapInfo);
        }

        private void EndGame(IWorld world, MapInfoDef? nextMapInfo)
        {
            ClusterDef? cluster = m_archiveCollection.Definitions.MapInfoDefinition.MapInfo.GetCluster(world.MapInfo.Cluster);
            bool isChangingClusters = nextMapInfo != null && world.MapInfo.Cluster != nextMapInfo.Cluster;

            if (isChangingClusters || EndGameLayer.EndGameMaps.Contains(world.MapInfo.Next))
                HandleZDoomTransition(world, cluster, nextMapInfo);
            else if (nextMapInfo != null)
                LoadMap(nextMapInfo, null, world.EntityManager.Players);
        }

        void HandleZDoomTransition(IWorld world, ClusterDef? cluster, MapInfoDef? nextMapInfo)
        {
            if (cluster == null)
                return;

            EndGameLayer endGameLayer = new(m_archiveCollection, m_audioSystem.Music, m_soundManager, world, cluster, nextMapInfo);
            endGameLayer.Exited += EndGameLayer_Exited;
            m_layerManager.Add(endGameLayer);
        }

        private void EndGameLayer_Exited(object? sender, EventArgs e)
        {
            if (sender is not EndGameLayer endGameLayer)
                return;

            if (endGameLayer.NextMapInfo != null)
                LoadMap(endGameLayer.NextMapInfo, null, endGameLayer.World.EntityManager.Players);
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
