using Helion.Models;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.SerializationContexts;
using Helion.World;
using Helion.World.Util;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Helion.Demo;

public static class DemoArchive
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    const string DemoInfoFile = "info.json";
    const string DemoDataFile = "demodata.lmp";

    public static bool Create(IDemoRecorder recorder, IWorld world, IList<DemoMap> demoMaps, IList<DemoCheat> cheats,
        IList<ConfigValueModel> configValues, string demoArchiveName)
    {
        recorder.Dispose();

        DemoModel demoModel = new()
        {
            AppVersion = GetAppVersionString(),
            Version = DemoVersion.Alpha,
            GameFiles = world.GetGameFilesModel(),
            Maps = demoMaps,
            Cheats = cheats,
            ConfigValues = configValues,
        };

        try
        {
            if (File.Exists(demoArchiveName))
                File.Delete(demoArchiveName);

            using ZipArchive zipArchive = ZipFile.Open(demoArchiveName, ZipArchiveMode.Create);
            ZipArchiveEntry entry = zipArchive.CreateEntry(DemoInfoFile);
            using (Stream stream = entry.Open())
                stream.Write(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(demoModel, typeof(DemoModel), DemoModelSerializationContext.Default)));

            entry = zipArchive.CreateEntry(DemoDataFile);
            using (Stream stream = entry.Open())
                stream.Write(File.ReadAllBytes(recorder.DemoFile));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error packaging demo file.");
            return false;
        }

        return true;
    }

    private static string GetAppVersionString()
    {
        var assemblyName = Assembly.GetExecutingAssembly().GetName();
        if (assemblyName.Version == null)
            return string.Empty;

        return assemblyName.Version.ToString();
    }

    public static bool Read(ArchiveCollection archiveCollection, string demoArchiveFile, out DemoModel? demoModel, out string binaryDemoFile)
    {
        binaryDemoFile = string.Empty;
        demoModel = null;
        try
        {
            using ZipArchive zipArchive = ZipFile.OpenRead(demoArchiveFile);
            var info = zipArchive.GetEntry(DemoInfoFile);
            var data = zipArchive.GetEntry(DemoDataFile);

            if (info == null || data == null)
                return false;

            demoModel = (DemoModel?)JsonSerializer.Deserialize(info.ReadDataAsString(), typeof(DemoModel), DemoModelSerializationContext.Default);
            if (demoModel == null)
                return false;

            // TODO the error log messages are sort of specific to save games
            if (!ModelVerification.VerifyModelFiles(demoModel.GameFiles, archiveCollection, Log))
                return false;

            binaryDemoFile = TempFileManager.GetFile();
            data.ExtractToFile(binaryDemoFile, true);

            return true;
        }
        catch
        {
            return false;
        }
    }
}
