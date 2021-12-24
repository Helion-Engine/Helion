using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Helion.Layer.Consoles;
using Helion.Layer.EndGame;
using Helion.Layer.Worlds;
using Helion.Maps;
using Helion.Models;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.Util.Configs.Components;
using Helion.Util.Configs.Values;
using Helion.Util.Consoles;
using Helion.Util.Consoles.Commands;
using Helion.Util.Extensions;
using Helion.Util.Parser;
using Helion.World;
using Helion.World.Cheats;
using Helion.World.Entities.Players;
using Helion.World.Save;
using Helion.World.Util;

namespace Helion.Client;

public partial class Client
{
    private const string StatFile = "levelstat.txt";
    private static readonly IList<Player> NoPlayers = Array.Empty<Player>();

    private GlobalData m_globalData = new();

    [ConsoleCommand(Constants.ConsoleCommands.Commands, "Lists all available commands.")]
    private void CommandListCommands(ConsoleCommandEventArgs args)
    {
        List<string> commands = new();
        foreach ((string command, _) in m_consoleCommands.OrderBy(x => x.command))
            commands.Add(command);

        foreach (ICheat cheat in CheatManager.Instance.OrderBy(x => x.ConsoleCommand))
            if (cheat.ConsoleCommand != null)
                commands.Add(cheat.ConsoleCommand);

        foreach ((string path, _) in m_config.OrderBy(x => x.path))
            commands.Add(path);

        for (int i = 0; i < commands.Count; i++)
            Log.Info(commands[i]);
    }

    [ConsoleCommand("audioDevice", "Sets a new audio device; can list devices with 'audioDevices'")]
    [ConsoleCommandArg("deviceIndex", "The device number from 'audioDevices' command")]
    private void CommandSetAudioDevice(ConsoleCommandEventArgs args)
    {
        if (!int.TryParse(args.Args[0], out int deviceIndex))
        {
            Log.Warn($"Unable to read audio device number from {args.Args[0]}");
            return;
        }

        // The user provides something in the range of [1, n] when we want
        // it in [0, n).
        deviceIndex--;

        List<string> deviceNames = m_audioSystem.GetDeviceNames().ToList();
        if (deviceIndex < 0 || deviceIndex >= deviceNames.Count)
        {
            Log.Warn($"Audio device index out of range, must be between 1 and {deviceNames.Count} inclusive");
            return;
        }

        string deviceName = deviceNames[deviceIndex];
        Log.Info($"Setting audio device to {deviceName}");

        // TODO: We should poll the device after setting it, and if SetDevice == true, set the config value.
        m_config.Audio.Device.Set(deviceName);
        m_audioSystem.SetDevice(deviceName);
        m_audioSystem.SetVolume(m_config.Audio.Volume);
    }

    [ConsoleCommand("audioDevices", "Prints all available audio devices")]
    private void CommandPrintAudioDevices(ConsoleCommandEventArgs args)
    {
        int num = 1;
        foreach (string device in m_audioSystem.GetDeviceNames())
            Log.Info($"{num++}. {device}");
    }

    [ConsoleCommand("exit", "Exits Helion")]
    private void CommandExit(ConsoleCommandEventArgs args)
    {
        m_window.Close();
    }

    [ConsoleCommand("load", "Loads a save game file into a new world")]
    [ConsoleCommandArg("fileName", "The name of the file")]
    private void CommandLoadGame(ConsoleCommandEventArgs args)
    {
        string fileName = args.Args[0];
        SaveGame saveGame = new(fileName);

        if (saveGame.Model == null)
        {
            LogError("Corrupt save game.");
            return;
        }

        WorldModel? worldModel = saveGame.ReadWorldModel();
        if (worldModel == null)
        {
            LogError("Corrupt world.");
            return;
        }

        if (!ModelVerification.VerifyModelFiles(worldModel.Files, m_archiveCollection, Log))
        {
            ShowConsole();
            return;
        }

        LoadMap(GetMapInfo(worldModel.MapName), worldModel, NoPlayers);
    }

    [ConsoleCommand("map", "Starts a new world with the map provided")]
    [ConsoleCommandArg("mapName", "The name of the map")]
    private void CommandHandleMap(ConsoleCommandEventArgs args)
    {
        MapInfoDef mapInfo = GetMapInfo(args.Args[0]);
        NewGame(mapInfo);
    }

    [ConsoleCommand("startGame", "Starts a new game")]
    private void CommandStartNewGame(ConsoleCommandEventArgs args)
    {
        MapInfoDef? mapInfoDef = GetDefaultMap();
        if (mapInfoDef == null)
        {
            LogError("Unable to find default map for game to start on");
            return;
        }

        NewGame(mapInfoDef);
    }

    [ConsoleCommand("soundVolume", "Sets the sound volume")]
    [ConsoleCommandArg("value", "A decimal value between 0.0 and 1.0")]
    private void CommandSetSoundVolume(ConsoleCommandEventArgs args)
    {
        if (!SimpleParser.TryParseFloat(args.Args[0], out float volume))
        {
            Log.Warn($"Unable to parse sound volume for input: {args.Args[0]}");
            return;
        }

        // TODO: The audio system should be listening to the config.
        m_config.Audio.SoundVolume.Set(volume);
        m_audioSystem.SetVolume(volume);
    }

    [ConsoleCommand("centerView", "Centers the players view")]
    private void CommandCenterView(ConsoleCommandEventArgs args)
    {
        if (m_layerManager.WorldLayer != null)
            m_layerManager.WorldLayer.World.Player.TickCommand.Add(TickCommands.CenterView);
    }

    private void Console_OnCommand(object? sender, ConsoleCommandEventArgs args)
    {
        if (TryHandleConsoleCommand(args))
            return;

        if (TryHandleCheatCommand(args))
            return;

        if (TryHandleConfigVariableCommand(args))
            return;

        Log.Warn($"No such command or config variable: {args.Command}");
    }

    private bool TryHandleCheatCommand(ConsoleCommandEventArgs args)
    {
        List<Player>? players = m_layerManager.WorldLayer?.World.EntityManager.Players;
        if (players == null || players.Empty())
            return false;

        return CheatManager.Instance.HandleCommand(players[0], args.Command);
    }

    private bool TryHandleConsoleCommand(ConsoleCommandEventArgs args)
    {
        if (m_consoleCommands.Invoke(args))
            return true;

        if (!m_consoleCommands.TryGet(args.Command, out ConsoleCommandData? cmd))
            return false;

        string cmdArgs = cmd.Args.Select(arg => arg.Optional ? $"[{arg.Name}]" : $"<{arg.Name}>").Join(", ");
        Log.Warn($"Invalid number of arguments for command {cmd.Info.Command}");
        Log.Warn($"    Usage: {cmd.Info.Command} {cmdArgs}");
        return true;
    }

    private bool TryHandleConfigVariableCommand(ConsoleCommandEventArgs args)
    {
        if (!m_config.TryGetComponent(args.Command, out ConfigComponent? component))
            return false;

        if (args.Args.Empty())
        {
            Log.Info($"{component.Path} = {component.Value}");
            return true;
        }

        ConfigSetResult result = component.Value.Set(args.Args[0]);
        switch (result)
        {
            case ConfigSetResult.Set:
                Log.Info($"Set {args.Command} to {component.Value.ObjectValue}");
                break;
            case ConfigSetResult.Unchanged:
                Log.Info($"{args.Command} set to the same value as before");
                break;
            case ConfigSetResult.Queued:
                Log.Info($"{args.Command} has been queued up for change: {component.Value.SetFlags}");
                break;
            case ConfigSetResult.NotSetByBadConversion:
                Log.Warn($"{args.Command} could not be set, incompatible argument");
                break;
            case ConfigSetResult.NotSetByFilter:
                Log.Warn($"{args.Command} could not be set, out of range or invalid argument");
                break;
            default:
                Log.Error($"{args.Command} unexpected setting result, report to a developer!");
                break;
        }

        return true;
    }

    private void NewGame(MapInfoDef mapInfo)
    {
        m_globalData = new();
        LoadMap(mapInfo, null, NoPlayers);
    }

    private MapInfoDef GetMapInfo(string mapName) =>
        m_archiveCollection.Definitions.MapInfoDefinition.MapInfo.GetMapInfoOrDefault(mapName);

    private void LoadMap(MapInfoDef mapInfoDef, WorldModel? worldModel, IList<Player> players)
    {
        IMap? map = m_archiveCollection.FindMap(mapInfoDef.MapName);
        if (map == null)
        {
            LogError($"Cannot load map '{mapInfoDef.MapName}', it cannot be found or is corrupt");
            return;
        }

        if (!RunZdbsp(map, mapInfoDef.MapName, out map))
        {
            Log.Error("Failed to run zdbsp.");
            return;
        }

        m_config.ApplyQueuedChanges(ConfigSetFlags.OnNewWorld);
        var skill = m_config.Game.Skill.Value;
        if (worldModel != null)
            skill = worldModel.Skill;

        SkillDef? skillDef = m_archiveCollection.Definitions.MapInfoDefinition.MapInfo.GetSkill(skill);
        if (skillDef == null)
        {
            LogError($"Could not find skill definition for {m_config.Game.Skill}");
            return;
        }

        if (m_layerManager.WorldLayer != null)
            m_layerManager.WorldLayer.World.LevelExit -= World_LevelExit;
        m_layerManager.Remove(m_layerManager.WorldLayer);

        if (map == null)
        {
            LogError($"Cannot load map '{mapInfoDef.MapName}', it cannot be found or is corrupt");
            return;
        }

        WorldLayer? newLayer = WorldLayer.Create(m_layerManager, m_globalData, m_config, m_console,
            m_audioSystem, m_archiveCollection, m_fpsTracker, m_profiler, mapInfoDef, skillDef, map,
            players.FirstOrDefault(), worldModel);
        if (newLayer == null)
            return;

        if (!m_globalData.VisitedMaps.Contains(mapInfoDef))
            m_globalData.VisitedMaps.Add(mapInfoDef);
        newLayer.World.LevelExit += World_LevelExit;

        m_layerManager.Add(newLayer);
        m_layerManager.ClearAllExcept(newLayer);

        newLayer.World.Start(worldModel);
    }

    private void World_LevelExit(object? sender, LevelChangeEvent e)
    {
        if (sender is not IWorld world)
            return;

        if (m_config.Game.LevelStat)
            WriteStatsFile(world);

        switch (e.ChangeType)
        {
            case LevelChangeType.Next:
                Intermission(world, GetNextLevel(world.MapInfo));
                break;

            case LevelChangeType.SecretNext:
                Intermission(world, GetNextSecretLevel(world.MapInfo));
                break;

            case LevelChangeType.SpecificLevel:
                ChangeLevel(e);
                break;

            case LevelChangeType.Reset:
                LoadMap(world.MapInfo, null, NoPlayers);
                break;
        }
    }

    private static void ClearStatsFile()
    {
        try
        {
            File.WriteAllText(StatFile, string.Empty);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to clear {StatFile} - {e}");
        }
    }

    private static void WriteStatsFile(IWorld world)
    {
        try
        {
            TimeSpan ts = TimeSpan.FromSeconds(world.LevelTime / Constants.TicksPerSecond);
            using StreamWriter sw = File.AppendText(StatFile);
            sw.WriteLine(string.Format("{0} - {1} ({2})  K: {3}/{4}  I: {5}/{6}  S: {7}/{8}", world.MapInfo.MapName,
                $"{ts.Minutes}:{ts.Seconds}.{ts.Milliseconds}", $"{ts.Minutes}:{ts.Seconds}",
                world.LevelStats.KillCount, world.LevelStats.TotalMonsters,
                world.LevelStats.ItemCount, world.LevelStats.TotalItems,
                world.LevelStats.SecretCount, world.LevelStats.TotalSecrets));
        }
        catch (Exception e)
        {
            Log.Error($"Failed to write {StatFile} - {e}");
        }
    }

    private void Intermission(IWorld world, MapInfoDef? nextMapInfo)
    {
        if (world.MapInfo.HasOption(MapOptions.NoIntermission))
        {
            EndGame(world, nextMapInfo);
        }
        else
        {
            IntermissionLayer intermissionLayer = new(world, m_soundManager, m_audioSystem.Music,
                world.MapInfo, nextMapInfo);
            intermissionLayer.Exited += IntermissionLayer_Exited;
            m_layerManager.Add(intermissionLayer);
        }
    }

    private void IntermissionLayer_Exited(object? sender, EventArgs e)
    {
        if (sender is not IntermissionLayer intermissionLayer)
            return;

        m_layerManager.Remove(m_layerManager.IntermissionLayer);
        EndGame(intermissionLayer.World, intermissionLayer.NextMapInfo);
    }

    private void EndGame(IWorld world, MapInfoDef? nextMapInfo)
    {
        ClusterDef? cluster = m_archiveCollection.Definitions.MapInfoDefinition.MapInfo.GetCluster(world.MapInfo.Cluster);
        bool isChangingClusters = nextMapInfo != null && world.MapInfo.Cluster != nextMapInfo.Cluster;

        if (isChangingClusters || EndGameLayer.EndGameMaps.Contains(world.MapInfo.Next))
            HandleZDoomTransition(world, cluster, nextMapInfo);
        else if (nextMapInfo != null)
            LoadMap(nextMapInfo, null, world.EntityManager.Players);
    }

    private void HandleZDoomTransition(IWorld world, ClusterDef? cluster, MapInfoDef? nextMapInfo)
    {
        if (cluster == null)
            return;

        EndGameLayer endGameLayer = new(m_archiveCollection, m_audioSystem.Music, m_soundManager, world, cluster, nextMapInfo);
        endGameLayer.Exited += EndGameLayer_Exited;

        m_layerManager.Add(endGameLayer);
    }

    private void EndGameLayer_Exited(object? sender, EventArgs e)
    {
        if (sender is not EndGameLayer endGameLayer)
            return;

        if (endGameLayer.NextMapInfo != null)
            LoadMap(endGameLayer.NextMapInfo, null, endGameLayer.World.EntityManager.Players);
    }

    private void ChangeLevel(LevelChangeEvent e)
    {
        if (MapWarp.GetMap(e.LevelNumber, m_archiveCollection.Definitions.MapInfoDefinition.MapInfo,
            out MapInfoDef? mapInfoDef) && mapInfoDef != null)
            LoadMap(mapInfoDef, null, NoPlayers);
    }

    private MapInfoDef? GetNextLevel(MapInfoDef mapDef) =>
        m_archiveCollection.Definitions.MapInfoDefinition.MapInfo.GetNextMap(mapDef);

    private MapInfoDef? GetNextSecretLevel(MapInfoDef mapDef) =>
        m_archiveCollection.Definitions.MapInfoDefinition.MapInfo.GetNextSecretMap(mapDef);

    private void ShowConsole()
    {
        if (m_layerManager.ConsoleLayer == null)
            m_layerManager.Add(new ConsoleLayer(m_archiveCollection.GameInfo.TitlePage, m_config, m_console, m_consoleCommands));
    }

    private void LogError(string error)
    {
        Log.Error(error);
        ShowConsole();
    }
}
