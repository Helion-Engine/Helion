using Helion.Demo;
using Helion.Layer.Worlds;
using Helion.Models;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.Util.Configs.Components;
using Helion.World.Cheats;
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
    private readonly List<DemoCheat> m_demoCheats = new();

    private void InitializeDemoRecorderFromCommandArgs()
    {
        if (m_commandLineArgs.Record == null || m_layerManager.WorldLayer == null)
            return;

        string fileName = m_commandLineArgs.Record;
        m_commandLineArgs.Record = null;

        if (!TryCreateDemoRecorder(fileName, out m_demoRecorder))
            return;

        var worldLayer = m_layerManager.WorldLayer;
        AddDemoMap(m_demoRecorder, worldLayer.CurrentMap.MapName, 0, null);
        m_demoRecorder.Start();
        worldLayer.StartRecording(m_demoRecorder);
        worldLayer.World.CheatManager.CheatActivationChanged += CheatManager_CheatActivationChanged;
        worldLayer.World.DisplayMessage(worldLayer.World.Player, null, "Recording has started.");
    }

    private void CheatManager_CheatActivationChanged(object? sender, CheatEventArgs e)
    {
        if (m_demoRecorder == null)
            return;

        int levelNumber = 0;
        if (e.Cheat is LevelCheat levelCheat)
            levelNumber = levelCheat.LevelNumber;

        m_demoCheats.Add(new DemoCheat()
        {
            CommandIndex = m_demoRecorder.CommandIndex,
            CheatType = (int)e.Cheat.CheatType,
            PlayerNumber = e.Player.PlayerNumber,
            LevelNumber = levelNumber
        });
    }

    private bool TryCreateDemoRecorder(string file, [NotNullWhen(true)] out IDemoRecorder? recorder)
    {
        m_demoPackageFile = file;

        try
        {
            recorder = new DemoRecorder(TempFileManager.GetFile());
            m_demoMaps.Clear();
            m_demoCheats.Clear();
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

        IList<DemoCheat> cheats = Array.Empty<DemoCheat>();
        if (m_demoModel != null)
            cheats = m_demoModel.Cheats;

        try
        {
            player = new DemoPlayer(binaryDemoFile, cheats);
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

            if (m_userConfigValues.Any(x => x.Key.Equals(component.Path, StringComparison.OrdinalIgnoreCase)))
                continue;

            m_userConfigValues.Add(new ConfigValueModel(component.Path, component.Value.ObjectValue));
        }

        m_config.ApplyConfiguration(m_demoModel.ConfigValues);
    }

    private void SetDefaultDemoValues(Dictionary<string, ConfigComponent> components)
    {
        foreach (var (_, component) in components)
        {
            if (!component.Attribute.Demo)
                continue;

            if (Equals(component.Value.ObjectValue, component.Value.ObjectDefaultValue))
                continue;

            component.Value.Set(component.Value.ObjectDefaultValue);
            m_userConfigValues.Add(new ConfigValueModel(component.Path, component.Value.ObjectValue));
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

        DemoArchive.Create(m_demoRecorder, m_layerManager.WorldLayer.World, m_demoMaps, m_demoCheats, m_demoConfigValues, m_demoPackageFile);
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

    private void SetWorldLayerToDemo(IDemoPlayer demoPlayer, MapInfoDef mapInfoDef, WorldLayer newLayer)
    {
        var demoMap = GetDemoMap(mapInfoDef.MapName);
        if (demoMap == null)
        {
            newLayer.World.DisplayMessage(newLayer.World.Player, null, 
                $"Demo does not contain map {mapInfoDef.MapName}. Playback stopped.");

            if (m_demoModel != null)
            {
                newLayer.World.DisplayMessage(newLayer.World.Player, null,
                    $"Available maps are: {string.Join(", ", m_demoModel.Maps.Select(x => x.Map))}");
            }

            demoPlayer.Stop();
            return;
        }

        demoPlayer.SetCommandIndex(demoMap.CommandIndex);

        var playerDef = newLayer.World.EntityManager.DefinitionComposer.GetByName(newLayer.World.Player.Definition.Name);
        if (demoMap.PlayerModel != null && playerDef != null)
        {
            var copyPlayer = new Player(demoMap.PlayerModel, new(), playerDef, newLayer.World);
            newLayer.World.Player.Inventory.Clear();
            newLayer.World.Player.Inventory.ClearKeys();
            newLayer.World.Player.CopyProperties(copyPlayer);          
        }

        demoPlayer.Start();
        newLayer.StartPlaying(demoPlayer);
        newLayer.World.CheatManager.CheatActivationChanged += CheatManager_CheatActivationChanged;
        newLayer.World.DisplayMessage(newLayer.World.Player, null, "Demo playback has started.");
    }

    private DemoMap? GetDemoMap(string mapName)
    {
        if (m_demoModel == null)
            return null;

        return m_demoModel.Maps.FirstOrDefault(x => x.Map == mapName);
    }

    private void AdvanceDemo(int advanceAmount)
    {
        if (m_demoPlayer == null || m_demoModel == null || m_demoModel.Maps.Count == 0 || m_layerManager.WorldLayer == null)
            return;

        var loadMap = m_demoModel.Maps[0];
        int commandIndex = Math.Clamp(m_demoPlayer.CommandIndex + advanceAmount, 0, int.MaxValue);

        foreach (var demoMap in m_demoModel.Maps.Reverse())
        {
            if (commandIndex < demoMap.CommandIndex)
                continue;

            loadMap = demoMap;
            break;
        }

        bool isPaused = m_layerManager.WorldLayer.World.Paused;
        bool consoleShowing = m_layerManager.ConsoleLayer != null;

        // Rewind is accomplished by loading the closest map and advancing to the desired tick.
        if (!loadMap.Map.Equals(m_layerManager.WorldLayer.CurrentMap.MapName, StringComparison.OrdinalIgnoreCase) || advanceAmount < 0)
            LoadMap(GetMapInfo(loadMap.Map), null, null);

        m_layerManager.WorldLayer.World.Resume();
        m_layerManager.WorldLayer.RunTicks(commandIndex - m_demoPlayer.CommandIndex);

        if (isPaused)
            m_layerManager.WorldLayer.World.Pause();
        if (consoleShowing)
            ShowConsole();
    }
}
