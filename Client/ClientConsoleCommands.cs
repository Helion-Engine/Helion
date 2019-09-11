using System.Collections.Generic;
using Helion.Layer.WorldLayers;
using Helion.Maps;
using Helion.Util;
using Helion.Util.Extensions;

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

            case "MAP":
                HandleMap(ccmdArgs.Args);
                break;
        
            default:
                Log.Info($"Unknown command: {ccmdArgs.Command}");
                break;
            }
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
            
            SinglePlayerWorldLayer? newLayer = SinglePlayerWorldLayer.Create(m_config, m_console, m_audioSystem, m_archiveCollection, map);
            if (newLayer == null)
                return;
            
            m_layerManager.Add(newLayer);
        }
    }
}