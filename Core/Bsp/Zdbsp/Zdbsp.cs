using Helion.Maps;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Locator;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using NLog;
using System;
using System.Diagnostics;
using System.IO;
using zdbspSharp;

namespace Helion.Bsp.Zdbsp
{
    public class Zdbsp
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private string m_lastMapName = string.Empty;

        private ArchiveCollection? m_lastBspCollection;
        private readonly Stopwatch m_stopwatch = new();

        public bool RunZdbsp(IMap map, string mapName, MapInfoDef mapInfoDef, out IMap? outputMap)
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
                if (!RunZdbsp(map.Archive.OriginalFilePath, mapName, outputFile))
                    return false;

                m_stopwatch.Stop();
                Log.Info($"Completed nodes {m_stopwatch.Elapsed}");
                Log.Debug("Zdbsp output:");

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

        private static bool RunZdbsp(string file, string map, string outputFile)
        {
            using FWadReader inwad = new(File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            using FWadWriter outwad = new(File.Open(outputFile, FileMode.CreateNew), inwad.IsIWAD());

            ProcessorOptions options = new()
            {
                GLOnly = true,
                BuildGLNodes = true,
                ConformNodes = false
            };

            int lumpCount = inwad.NumLumps();
            for (int i = 0; i < lumpCount - 1; i++)
            {
                if (!inwad.IsMap(i) || !inwad.LumpName(i).EqualsIgnoreCase(map))
                    continue;

                FProcessor builder = new(inwad, i, options);
                builder.Write(outwad);
                return true;
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
}
