using Helion.Bsp.External;
using Helion.Maps;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Locator;
using System;
using System.Diagnostics;
using System.IO;

namespace Helion.Client
{
    public partial class Client
    {
        private string m_lastMapName = string.Empty;
        private ArchiveCollection? m_lastBspCollection;
        private readonly Stopwatch m_stopwatch = new Stopwatch();

        private bool RunZdbsp(IMap map, string mapName, out IMap? outputMap)
        {
            string outputFile = Path.GetFullPath("output.wad");
            outputMap = null;

            try
            {
                if (m_lastBspCollection != null && File.Exists(outputFile) && m_lastMapName.Equals(mapName, StringComparison.OrdinalIgnoreCase))
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

                m_lastMapName = mapName;

                Log.Info("Loading compiled map...");
                m_stopwatch.Restart();
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

            if (File.Exists(outputFile))
                File.Delete(outputFile);
        }
    }
}
