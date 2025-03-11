using Helion.Util.Consoles.Commands;
using Helion.Util.Consoles;
using Helion.Maps.Udmf;
using System;
using System.IO;

namespace Helion.Client;

public partial class Client
{
    [ConsoleCommand("writetextmap", "Converts a doom map to a textmap file")]
    private void CommandWriteTextMap(ConsoleCommandEventArgs args)
    {
        if (args.Args.Count < 2)
        {
            Log.Error("Map and output file argument required");
            Log.Error("writetextmap [MapName] [OutputFile]");
            return;
        }

        var mapName = args.Args[0];
        var outputFile = args.Args[1];
        try
        {
            var map = m_archiveCollection.FindMap(mapName);
            if (map == null)
            {
                Log.Error($"Failed to find map {mapName}");
                return;
            }

            using var textWriter = new StreamWriter(outputFile);
            UdmfMapWriter.WriteMap(map, textWriter);
            Log.Info($"Successfully wrote {outputFile}");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to write textmap {outputFile}");
            Log.Error(ex.Message);
        }
    }
}
