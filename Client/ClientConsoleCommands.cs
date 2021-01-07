using System.Collections.Generic;
using Helion.Layer.WorldLayers;
using Helion.Maps;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util.Extensions;
using Helion.Util.Terminals;
using Helion.World.Cheats;

namespace Helion.Client
{
    /// <summary>
    /// The client that runs the engine.
    /// </summary>
    public partial class Client
    {
        private void Console_OnCommand(object? sender, ConsoleCommandEventArgs ccmdArgs)
        {
            switch (ccmdArgs.Command.ToUpper())
            {
                case "EXIT":
                    m_window.Close();
                    break;

                case "MAP":
                    HandleMap(ccmdArgs.Args);
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
                    if (!CheatManager.Instance.HandleCommand(ccmdArgs.Command))
                        Log.Info($"Unknown command: {ccmdArgs.Command}");
                    break;
            }
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
            IList<string> deviceNames = m_audioSystem.GetDeviceNames();
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

            // For now, we will only have one world layer present. If someone
            // wants to `map mapXX` offline then it will kill their connection
            // and go offline to some world.
            m_layerManager.RemoveByType(typeof(WorldLayer));

            string mapName = args[0];
            IMap? map = m_archiveCollection.FindMap(mapName);
            if (map == null)
            {
                Log.Warn("Cannot load map '{0}', it cannot be found or is corrupt", mapName);
                return;
            }

            MapInfoDef mapInfoDef = m_archiveCollection.Definitions.MapInfoDefinition.MapInfo.GetMapInfoOrDefault(map.Name);
            SinglePlayerWorldLayer? newLayer = SinglePlayerWorldLayer.Create(m_config, m_console, m_audioSystem,
                m_archiveCollection, mapInfoDef, map);
            if (newLayer == null)
                return;

            m_layerManager.Add(newLayer);
            newLayer.World.Start();
        }
    }
}