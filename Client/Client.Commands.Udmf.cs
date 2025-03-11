using Helion.Util.Consoles.Commands;
using Helion.Util.Consoles;
using Helion.Maps.Udmf;

namespace Helion.Client;

public partial class Client
{
    [ConsoleCommand("writetextmap", "Converts a doom map to a textmap file")]
    private void CommandWriteTextMap(ConsoleCommandEventArgs args)
    {
        if (args.Args.Count < 2)
            Log.Error("Output file argument required");

        if (!UdmfMapWriter.WriteMap(m_archiveCollection, args.Args[0], args.Args[1]))
            Log.Error("Failed to write textmap");
        else
            Log.Info($"Successfully wrote {args.Args[1]}");
    }
}
