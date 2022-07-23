using Helion.Demo;
using Helion.Layer.Worlds;
using Helion.Models;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.Util.Configs.Components;
using Helion.World.Entities.Players;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Helion.Client;

public partial class Client
{
    private IDemoRecorder? m_demoRecorder;
    private IDemoPlayer? m_demoPlayer;
    private DemoModel? m_demoModel;
    private string m_demoPackageFile = string.Empty;
    private readonly List<DemoMap> m_demoMaps = new();
    private readonly List<ConfigValueModel> m_demoConfigValues = new();
    private readonly List<ConfigValueModel> m_userConfigValues = new();

    private bool TryCreateDemoRecorder(string file, [NotNullWhen(true)] out IDemoRecorder? recorder)
    {
        m_demoPackageFile = file;

        try
        {
            recorder = new DemoRecorder(TempFileManager.GetFile());
            m_demoMaps.Clear();
            SaveDemoConfigValues();
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
            player.PlaybackEnded += Player_PlaybackEnded;
            SetConfigValuesFromDemo();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to read demo file.");
            player = null;
        }

        return false;
    }

    private void Player_PlaybackEnded(object? sender, EventArgs e)
    {
        m_config.ApplyConfiguration(m_userConfigValues);
        m_userConfigValues.Clear();
    }

    private void SaveDemoConfigValues()
    {
        m_demoConfigValues.Clear();
        foreach (var (path, component) in m_config.GetComponents())
        {
            if (!component.Attribute.Demo)
                continue;

            m_demoConfigValues.Add(new ConfigValueModel(path, component.Value.ObjectValue));
        }
    }

    private void SetConfigValuesFromDemo()
    {
        if (m_demoModel == null)
            return;

        var components = m_config.GetComponents();
        SetDefaultDemoValues(components);

        foreach (var configValue in m_demoModel.ConfigValues)
        {
            if (!components.TryGetValue(configValue.Key, out ConfigComponent? component))
                continue;

            m_userConfigValues.Add(new ConfigValueModel(component.Path, component.Value.ObjectValue));
        }

        m_config.ApplyConfiguration(m_demoModel.ConfigValues);
    }

    private static void SetDefaultDemoValues(Dictionary<string, ConfigComponent> components)
    {
        foreach (var component in components)
        {
            if (!component.Value.Attribute.Demo)
                continue;

            component.Value.Value.Set(component.Value.Value.ObjectDefaultValue);
        }
    }

    private bool GetBinaryDemoFileFromPackage(string file, out string binaryDemoFile)
    {
        return DemoArchive.Read(m_archiveCollection, file, out m_demoModel, out binaryDemoFile);
    }

    private void PackageDemo()
    {
        if (m_demoRecorder == null || m_layerManager.WorldLayer == null || !File.Exists(m_demoRecorder.DemoFile))
            return;

        DemoArchive.Create(m_demoRecorder, m_layerManager.WorldLayer.World, m_demoMaps, m_demoConfigValues, m_demoPackageFile);
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
                newLayer.World.Player.Inventory.Clear();
                newLayer.World.Player.Inventory.ClearKeys();
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
