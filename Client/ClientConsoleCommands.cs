using System.Collections.Generic;
using Helion.Layer.WorldLayers;
using Helion.Util;
using Helion.Util.Extensions;

namespace Helion.Client
{
    public partial class Client
    {
        private void Console_OnCommand(object? sender, ConsoleCommandEventArgs ccmdArgs)
        {
            switch (ccmdArgs.Command)
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

            string map = args[0];
            
            if (m_layerManager.TryGetLayer(out SinglePlayerWorldLayer layer))
            {
                layer.LoadMap(map);
                return;
            }
            
            // For now, we will only have one world layer present. If someone
            // wants to `map mapXX` offline then it will kill their connection
            // and go offline to some world.
            m_layerManager.RemoveByType(typeof(WorldLayer));
            
            SinglePlayerWorldLayer newLayer = new SinglePlayerWorldLayer(m_config, m_console, m_archiveCollection);
            if (newLayer.LoadMap(map))
                m_layerManager.Add(newLayer);
            else
                newLayer.Dispose();
        }
    }
}