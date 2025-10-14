using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Locator;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using NLog;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using zdbspSharp;

namespace Helion.Maps.Bsp.Zdbsp;

public class Zdbsp
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly Stopwatch m_stopwatch = new();

    public bool RunZdbsp(string filePath, string mapName, [NotNullWhen(true)] out IMap? outputMap)
    {
        string outputFile = TempFileManager.GetFile();
        outputMap = null;

        try
        {
            Log.Info($"Building nodes [{filePath}]...");
            m_stopwatch.Restart();
            if (!RunZdbsp(filePath, mapName, outputFile))
                return false;

            m_stopwatch.Stop();
            Log.Info($"Completed nodes {m_stopwatch.Elapsed}");
            Log.Debug("Zdbsp output:");

            Log.Info("Loading compiled map...");
            m_stopwatch.Restart();

            var archiveCollection = new ArchiveCollection(new FilesystemArchiveLocator(), new(), ArchiveCollection.StaticDataCache);
            if (!archiveCollection.Load([outputFile], loadDefaultAssets: false))
            {
                TempFileManager.DeleteFile(outputFile);
                return false;
            }

            outputMap = archiveCollection.FindMap(mapName, FindMapOptions.LoadMapData);
            m_stopwatch.Stop();
            Log.Info($"Completed map load {m_stopwatch.Elapsed}");
            archiveCollection.Dispose();
            TempFileManager.DeleteFile(outputFile);
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
        using FWadWriter outwad = new(File.Open(outputFile, FileMode.OpenOrCreate), inwad.IsIWAD());

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
}
