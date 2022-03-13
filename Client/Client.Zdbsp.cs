using Helion.Bsp.External;
using Helion.Maps;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Locator;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using System;
using System.Diagnostics;
using System.IO;

namespace Helion.Client;

public partial class Client
{
    private string m_lastMapName = string.Empty;

    private ArchiveCollection? m_lastBspCollection;
    private readonly Stopwatch m_stopwatch = new();

    private bool RunZdbsp(IMap map, string mapName, MapInfoDef mapInfoDef, out IMap? outputMap)
    {
        string outputFile = TempFileManager.GetFile();
        outputMap = null;

        try
        {
            if (m_lastBspCollection != null && File.Exists(outputFile) && m_lastMapName.Equals(mapInfoDef.MapName, StringComparison.OrdinalIgnoreCase))
            {
                outputMap = m_lastBspCollection.FindMap(mapName);
                return outputMap != null;
            }

            CleanZdbspData(outputFile);

            Log.Info($"Building nodes [{map.Archive.OriginalFilePath}]...");
            m_stopwatch.Restart();
            Zdbsp zdbsp = new(map.Archive.OriginalFilePath, outputFile);
            zdbsp.Run(mapName, out string output);

            m_stopwatch.Stop();
            Log.Info($"Completed nodes {m_stopwatch.Elapsed}");
            Log.Debug("Zdbsp output:");
            foreach (string line in output.Split(Environment.NewLine))
                Log.Debug($"    {line}");

            m_lastMapName = mapInfoDef.MapName;

            Log.Info("Loading compiled map...");
            m_stopwatch.Restart();

            if (m_lastBspCollection != null)
                m_lastBspCollection.Dispose();

            m_lastBspCollection = new ArchiveCollection(new FilesystemArchiveLocator(), new());
            if (!m_lastBspCollection.Load(new string[] { outputFile }, loadDefaultAssets: false))
                return false;

            outputMap = m_lastBspCollection.FindMap(mapName);
            m_stopwatch.Stop();
            Log.Info($"Completed map load {m_stopwatch.Elapsed}");
            return outputMap != null;
        }
        catch (Exception e)
        {
            Log.Error($"Zdbsp critical failure: {e.Message}");
        }

        return false;
    }

    private void CleanZdbspData(string outputFile)
    {
        if (m_lastBspCollection != null)
            m_lastBspCollection.Dispose();

        TempFileManager.DeleteFile(outputFile);
    }
}
