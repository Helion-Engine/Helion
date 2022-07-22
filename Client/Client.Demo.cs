using Helion.Demo;
using Helion.Layer.Worlds;
using Helion.Models;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.Util.RandomGenerators;
using Helion.World;
using Helion.World.Entities.Players;
using Helion.World.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Helion.Client;

public partial class Client
{
    private IDemoRecorder? m_demoRecorder;
    private IDemoPlayer? m_demoPlayer;
    private DemoModel? m_demoModel;
    private string m_demoPackageFile = string.Empty;
    private readonly List<DemoMap> m_demoMaps = new();

    const string DemoInfoFile = "info.json";
    const string DemoDataFile = "demodata.lmp";

    private bool TryCreateDemoRecorder(string file, string mapName, [NotNullWhen(true)] out IDemoRecorder? recorder)
    {
        m_demoPackageFile = file;

        try
        {
            recorder = new DemoRecorder(TempFileManager.GetFile());
            m_demoMaps.Clear();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to record demo file.");
            recorder = null;
        }

        return false;
    }

    private bool TryCreateDemoPlayer(string file, [NotNullWhen(true)] out IDemoPlayer? player)
    {
        player = null;
        if (!GetBinaryDemoFileFromPackage(file, out string binaryDemoFile))
            return false;

        try
        {
            player = new DemoPlayer(binaryDemoFile);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to read demo file.");
            player = null;
        }

        return false;
    }

    private bool GetBinaryDemoFileFromPackage(string file, out string binaryDemoFile)
    {
        binaryDemoFile = string.Empty;
        try
        {
            using ZipArchive zipArchive = ZipFile.OpenRead(file);
            var info = zipArchive.GetEntry(DemoInfoFile);
            var data = zipArchive.GetEntry(DemoDataFile);

            if (info == null || data == null)
                return false;

            m_demoModel = JsonConvert.DeserializeObject<DemoModel>(info.ReadDataAsString());
            if (m_demoModel == null)
                return false;

            // TODO the error log messages are sort of specific to save games
            if (!ModelVerification.VerifyModelFiles(m_demoModel.GameFiles, m_archiveCollection, Log))
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

    private void PackageDemo()
    {
        if (m_demoRecorder == null || m_layerManager.WorldLayer == null || !File.Exists(m_demoRecorder.DemoFile))
            return;

        m_demoRecorder.Dispose();

        DemoModel demoModel = new()
        {
            Version = DemoVersion.Alpha,
            GameFiles = m_layerManager.WorldLayer.World.GetGameFilesModel(),
            Maps = m_demoMaps
        };

        try
        {
            if (File.Exists(m_demoPackageFile))
                File.Delete(m_demoPackageFile);

            using ZipArchive zipArchive = ZipFile.Open(m_demoPackageFile, ZipArchiveMode.Create);
            ZipArchiveEntry entry = zipArchive.CreateEntry(DemoInfoFile);
            using (Stream stream = entry.Open())
                stream.Write(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(demoModel)));

            entry = zipArchive.CreateEntry(DemoDataFile);
            using (Stream stream = entry.Open())
                stream.Write(File.ReadAllBytes(m_demoRecorder.DemoFile));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error packaging demo file.");
        }
    }

    private void AddDemoMap(IDemoRecorder recorder, string mapName, int randomIndex, Player? player)
    {
        m_demoMaps.Add(new DemoMap()
        {
            Map = mapName,
            CommandIndex = recorder.CommandIndex,
            RandomIndex = randomIndex,
            PlayerModel = player?.ToPlayerModel()
        });
    }

    private void SetWorldLayerToDemo(IDemoPlayer player, MapInfoDef mapInfoDef, WorldLayer newLayer)
    {
        var demoMap = GetDemoMap(mapInfoDef.MapName);
        var playerDef = newLayer.World.EntityManager.DefinitionComposer.GetByName(newLayer.World.Player.Definition.Name);
        if (demoMap != null)
        {
            player.SetCommandIndex(demoMap.CommandIndex);

            if (demoMap.PlayerModel != null && playerDef != null)
            {
                var copyPlayer = new Player(demoMap.PlayerModel, new(), playerDef, newLayer.World);
                newLayer.World.Player.CopyProperties(copyPlayer);

                player.Start();
                newLayer.StartPlaying(player);
                newLayer.World.DisplayMessage(newLayer.World.Player, null, "Demo playback has started.");
            }
        }

        player.Start();
        newLayer.StartPlaying(player);
    }

    private DemoMap? GetDemoMap(string mapName)
    {
        if (m_demoModel == null)
            return null;

        return m_demoModel.Maps.FirstOrDefault(x => x.Map == mapName);
    }
}
