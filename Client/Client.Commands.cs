using System.Collections.Generic;
using System.Linq;
using Helion.Layer;
using Helion.Layer.WorldLayers;
using Helion.Maps;
using Helion.Models;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util.Consoles;
using Helion.Util.Extensions;
using Helion.World.Cheats;
using Helion.World.Save;
using Helion.World.Util;

namespace Helion.Client
{
    public partial class Client
    {
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
                Log.Error("Corrupt save game.");
                ShowConsole();
                return;
            }

            WorldModel? worldModel = saveGame.ReadWorldModel();
            if (worldModel == null)
            {
                Log.Error("Corrupt world.");
                ShowConsole();
                return;
            }

            if (!ModelVerification.VerifyModelFiles(worldModel.Files, m_archiveCollection, Log))
            {
                ShowConsole();
                return;
            }

            LoadMapByName(worldModel.MapName, worldModel);
        }

        private void StartNewGame()
        {
            MapInfoDef? mapInfoDef = GetDefaultMap();
            if (mapInfoDef == null)
            {
                Log.Error("Unable to find default map for game to start on");
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

            LoadMapByName(args[0], null);    
        }

        private void LoadMapByName(string mapName, WorldModel? worldModel)
        {
            IMap? map = m_archiveCollection.FindMap(mapName);
            if (map == null)
            {
                Log.Warn("Cannot load map '{0}', it cannot be found or is corrupt", mapName);
                return;
            }

            SkillDef? skillDef = m_archiveCollection.Definitions.MapInfoDefinition.MapInfo.GetSkill(m_config.Game.Skill);
            if (skillDef == null)
            {
                Log.Warn($"Could not find skill definition for {m_config.Game.Skill}");
                return;
            }

            MapInfoDef mapInfoDef = m_archiveCollection.Definitions.MapInfoDefinition.MapInfo.GetMapInfoOrDefault(map.Name);

            m_layerManager.Remove<SinglePlayerWorldLayer>();
            m_layerManager.PruneDisposed();

            SinglePlayerWorldLayer? newLayer = SinglePlayerWorldLayer.Create(m_layerManager, m_config, m_console,
                m_audioSystem, m_archiveCollection, mapInfoDef, skillDef, map, worldModel);
            if (newLayer == null)
                return;

            m_layerManager.Add(newLayer);
            newLayer.World.Start();

            m_layerManager.RemoveAllBut<WorldLayer>();
        }

        private void ShowConsole()
        {
            if (m_layerManager.Get<ConsoleLayer>() == null)
                m_layerManager.Add(new ConsoleLayer(m_archiveCollection, m_console));
        }
    }
}
