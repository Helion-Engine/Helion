using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Helion.Geometry.Boxes;
using Helion.Layer.Consoles;
using Helion.Layer.EndGame;
using Helion.Layer.IwadSelection;
using Helion.Layer.Worlds;
using Helion.Maps;
using Helion.Maps.Bsp.Zdbsp;
using Helion.Models;
using Helion.Render.OpenGL.Shared;
using Helion.Resources.Definitions;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Configs.Components;
using Helion.Util.Configs.Impl;
using Helion.Util.Configs.Values;
using Helion.Util.Consoles;
using Helion.Util.Consoles.Commands;
using Helion.Util.Extensions;
using Helion.Util.Loggers;
using Helion.Util.Parser;
using Helion.Util.RandomGenerators;
using Helion.World;
using Helion.World.Cheats;
using Helion.World.Entities;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Players;
using Helion.World.Save;
using Helion.World.Util;

namespace Helion.Client;

public partial class Client
{
    private const string StatFile = "levelstat.txt";

    private GlobalData m_globalData = new();
    private readonly Zdbsp m_zdbsp = new();
    private WorldModel? m_lastWorldModel;
    private bool m_isSecretExit;

    [ConsoleCommand("setpos", "Sets the player's position (x y z). Ex setpos 100 100 0")]
    private void SetPosition(ConsoleCommandEventArgs args)
    {
        if (m_layerManager.WorldLayer == null)
            return;

        if (args.Args.Count < 3)
            return;

        if (!Parsing.TryParseDouble(args.Args[0], out var x) || !Parsing.TryParseDouble(args.Args[1], out var y) || !Parsing.TryParseDouble(args.Args[2], out var z))
            return;

        m_layerManager.WorldLayer.World.SetEntityPosition(m_layerManager.WorldLayer.World.Player, (x, y, z));
        m_layerManager.Remove(m_layerManager.ConsoleLayer);
    }

    [ConsoleCommand("restart", "Restarts the application.")]
    private void Restart(ConsoleCommandEventArgs args)
    {
        // Note:  We might also want to use the current working directory when restarting, in case it
        // is not the same directory the executable is in.  That seems like a less likely case because
        // the single-file published version of this application doesn't really like being run from
        // a different working directory.
        string executablePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;

        if (!File.Exists(executablePath))
        {
            Log.Error("Application not found.");
            return;
        }

        if (m_config is FileConfig fileConfig)
            fileConfig.Write();

        Process.Start(executablePath, m_commandLineArgs.OriginalArgs);
        Environment.Exit(0);
    }

    [ConsoleCommand(Constants.ConsoleCommands.Commands, "Lists all available commands.")]
    private void CommandListCommands(ConsoleCommandEventArgs args)
    {
        var commands = GetAllCommands();
        foreach (var command in commands)
            HelionLog.Info(command);
    }

    [ConsoleCommand("search", "Lists all available commands.")]
    private void SearchCommands(ConsoleCommandEventArgs args)
    {
        if (args.Args.Count == 0)
            return;

        var commands = GetAllCommands().Where(x => x.Contains(args.Args[0], StringComparison.OrdinalIgnoreCase));
        if (!commands.Any())
        {
            Log.Warn("No commands found");
            return;
        }

        foreach (var command in commands)
            HelionLog.Info(command);
    }

    private IEnumerable<string> GetAllCommands()
    {
        foreach ((string command, _) in m_consoleCommands.OrderBy(x => x.command))
            yield return command;

        foreach (ICheat cheat in CheatManager.Cheats.OrderBy(x => x.ConsoleCommand))
            if (cheat.ConsoleCommand != null)
                yield return cheat.ConsoleCommand;

        foreach (string path in m_config.GetComponents().Keys.OrderBy(x => x))
            yield return path;
    }

    [ConsoleCommand("demo.stop", "Stops the current demo.")]
    private void DemoStop(ConsoleCommandEventArgs args)
    {
        if (m_demoPlayer == null)
            return;

        m_demoPlayer.Stop();

        if (m_layerManager.WorldLayer == null)
            return;

        m_layerManager.WorldLayer.StopPlaying();
        m_demoPlayer.Dispose();
        m_demoPlayer = null;
    }

    [ConsoleCommand("demo.advanceticks", "Advances the demo forward.")]
    [ConsoleCommandArg("ticks", "The number of seconds to advance the demo.")]
    private void DemoAdvanceTicks(ConsoleCommandEventArgs args)
    {
        if (args.Args.Count == 0 || !int.TryParse(args.Args[0], out int advanceAmount))
            return;

        AdvanceDemo(advanceAmount);
    }

    [ConsoleCommand("demo.advance", "Advances the demo forward.")]
    [ConsoleCommandArg("seconds", "The number of seconds to advance the demo.")]
    private void DemoAdvance(ConsoleCommandEventArgs args)
    {
        if (args.Args.Count == 0 || !int.TryParse(args.Args[0], out int advanceAmount))
            return;

        AdvanceDemo(advanceAmount * (int)Constants.TicksPerSecond);
    }


    [ConsoleCommand("mark.add", "Mark current spot in automap.")]
    private void CommandMark(ConsoleCommandEventArgs args)
    {
        if (m_layerManager.WorldLayer == null)
            return;

        var world = m_layerManager.WorldLayer.World;
        world.EntityManager.Create("MapMarker", m_layerManager.WorldLayer.World.Player.Position + RenderInfo.LastAutomapOffset.Double.To3D(0));
    }

    [ConsoleCommand("mark.remove", "Removes map markers within a 128 radius.")]
    private void CommandRemoveMark(ConsoleCommandEventArgs args)
    {
        if (m_layerManager.WorldLayer == null)
            return;

        var world = m_layerManager.WorldLayer.World;
        var box = new Box2D(world.Player.Position.XY + RenderInfo.LastAutomapOffset.Double, 128);
        for (var entity = world.EntityManager.Head; entity != null; entity = entity.Next)
        {
            if (entity.Definition.EditorId == (int)EditorId.MapMarker && entity.Overlaps2D(box))
                world.EntityManager.Destroy(entity);
        }
    }

    [ConsoleCommand("mark.clear", "Removes all map markers.")]
    private void CommandClearMarks(ConsoleCommandEventArgs args)
    {
        if (m_layerManager.WorldLayer == null)
            return;

        var world = m_layerManager.WorldLayer.World;
        for (var entity = world.EntityManager.Head; entity != null; entity = entity.Next)
        {
            if (entity.Definition.EditorId == (int)EditorId.MapMarker)
                world.EntityManager.Destroy(entity);
        }
    }

    [ConsoleCommand("findkeys", "Finds the next key in the map.")]
    private void CommandFindKey(ConsoleCommandEventArgs args)
    {
        if (m_layerManager.WorldLayer == null)
            return;

        m_layerManager.WorldLayer.World.FindKeys();
    }

    [ConsoleCommand("findkeylines", "Finds the next locked key line in the map.")]
    private void CommandFindKeyLine(ConsoleCommandEventArgs args)
    {
        if (m_layerManager.WorldLayer == null)
            return;

        m_layerManager.WorldLayer.World.FindKeyLines();
    }

    [ConsoleCommand("findexits", "Finds the next exit line/sector in the map.")]
    private void CommandFindExit(ConsoleCommandEventArgs args)
    {
        if (m_layerManager.WorldLayer == null)
            return;

        m_layerManager.WorldLayer.World.FindExits();
    }

    [ConsoleCommand("listdisplays", "Lists all available displays.")]
    private void CommandListDisplays(ConsoleCommandEventArgs args)
    {
        var monitors = m_window.GetMonitors(out var currentMonitor);
        foreach (var monitor in monitors)
        {
            string current = (currentMonitor != null && monitor.Index == currentMonitor.Index ? "[Current]" : string.Empty);
            HelionLog.Info($"{monitor.Index + 1}: {monitor.HorizontalResolution}, {monitor.VerticalResolution}{current}");
        }
    }

    [ConsoleCommand("audio.device", "The current audio device")]
    private void CommandAudioDevice(ConsoleCommandEventArgs args)
    {
        HelionLog.Info(m_config.Audio.Device.Value);
    }

    [ConsoleCommand("audio.setdevice", "Sets a new audio device; can list devices with 'audioDevices'")]
    [ConsoleCommandArg("deviceIndex", "The device number from 'audio.devices' command")]
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
            HelionLog.Warn($"Audio device index out of range, must be between 1 and {deviceNames.Count} inclusive");
            return;
        }

        string deviceName = deviceNames[deviceIndex];
        HelionLog.Info($"Setting audio device to {deviceName}");

        // TODO: We should poll the device after setting it, and if SetDevice == true, set the config value.
        m_config.Audio.Device.Set(deviceName);
        m_audioSystem.SetDevice(deviceName);
    }

    [ConsoleCommand("audio.devices", "Prints all available audio devices")]
    private void CommandPrintAudioDevices(ConsoleCommandEventArgs args)
    {
        int num = 1;
        foreach (string device in m_audioSystem.GetDeviceNames())
            HelionLog.Info($"{num++}. {device}");
    }

    [ConsoleCommand("exit", "Exits Helion")]
    private void CommandExit(ConsoleCommandEventArgs args)
    {
        m_window.Close();
    }

    [ConsoleCommand("load", "Loads a save game file into a new world")]
    [ConsoleCommandArg("fileName", "The name of the file")]
    private async Task CommandLoadGame(ConsoleCommandEventArgs args)
    {
        string fileName = args.Args[0];
        HelionLog.Info($"Loading save file {fileName}");
        if (!m_saveGameManager.SaveFileExists(fileName))
        {
            LogError($"Save file {fileName} not found.");
            return;
        }

        SaveGame saveGame = m_saveGameManager.ReadSaveGame(fileName);
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

        var world = m_layerManager.WorldLayer?.World;
        m_layerManager.LastSave = new(saveGame, worldModel, string.Empty, true);
        PrepLoadMap();
        await LoadMapAsync(GetMapInfo(worldModel.MapName), worldModel, null,
            showLoadingTitlepic: world == null || !world.MapInfo.MapName.EqualsIgnoreCase(worldModel.MapName));
    }

    [ConsoleCommand("map", "Starts a new world with the map provided")]
    [ConsoleCommandArg("mapName", "The name of the map")]
    private async Task CommandHandleMap(ConsoleCommandEventArgs args)
    {
        try
        {
            var mapName = args.Args[0];
            if (mapName == "*" && m_layerManager.WorldLayer != null)
            {
                await NewGame(m_layerManager.WorldLayer.CurrentMap);
                return;
            }

            if (MapInfo.IsWarpTrans(mapName))
            {
                await NewGame(m_archiveCollection.Definitions.MapInfoDefinition.MapInfo.GetStartMapOrDefault(m_archiveCollection, mapName));
                return;
            }

            MapInfoDef mapInfo = GetMapInfo(mapName);
            await NewGame(mapInfo);
        }
        catch (Exception e)
        {
            HandleFatalException(e);
        }
    }

    [ConsoleCommand("startGame", "Starts a new game")]
    private async Task CommandStartNewGame(ConsoleCommandEventArgs args)
    {
        try
        {
            MapInfoDef? mapInfoDef = GetDefaultMap();
            if (mapInfoDef == null)
            {
                LogError("Unable to find default map for game to start on");
                return;
            }

            await NewGame(mapInfoDef);
        }
        catch (Exception e)
        {
            HandleFatalException(e);
        }
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
            m_layerManager.WorldLayer.AddCommand(TickCommands.CenterView);
    }

    [ConsoleCommand("inventory.clear", "Clears the players inventory")]
    private void CommandInventoryClear(ConsoleCommandEventArgs args) =>
        AddWorldResumeCommand(DoInventoryClear, args);

    [ConsoleCommand("inventory.remove", "Removes the item from the players inventory")]
    private void CommandInventoryRemove(ConsoleCommandEventArgs args) =>
        AddWorldResumeCommand(DoInventoryRemove, args);

    [ConsoleCommand("inventory.add", "Adds the item to the players inventory")]
    private void CommandInventoryAdd(ConsoleCommandEventArgs args) =>
        AddWorldResumeCommand(DoInventoryAdd, args);

    [ConsoleCommand("inventory.setamount", "Sets the item amount in the players inventory (player must own the item)")]
    private void CommandInventorySetAmount(ConsoleCommandEventArgs args) =>
        AddWorldResumeCommand(DoInventorySetAmount, args);

    [ConsoleCommand("nojump", "Disables player jumping")]
    private void NoJump(ConsoleCommandEventArgs args) =>
        ToggleMapOption(MapOptions.NoJump, args);

    [ConsoleCommand("NoCrouch", "Disables player crouching")]
    private void NoCrouch(ConsoleCommandEventArgs args) =>
        ToggleMapOption(MapOptions.NoCrouch, args);

    [ConsoleCommand("NoFreelook", "Disables player freelook")]
    private void NoFreelook(ConsoleCommandEventArgs args) =>
        ToggleMapOption(MapOptions.NoFreelook, args);

    [ConsoleCommand("AllowMonsterTelefrags", "Allows monster telefrags")]
    private void AllowMonsterTelefrags(ConsoleCommandEventArgs args) =>
        ToggleMapOption(MapOptions.AllowMonsterTelefrags, args);

    private readonly List<string> m_compLevelNames = [];

    [ConsoleCommand("CompLvl", "Sets the complvl (vanilla, boom, mbf, or mbf21)")]
    private void CompLvl(ConsoleCommandEventArgs args)
    {
        if (m_compLevelNames.Count == 0)
        {
            foreach (CompLevel comp in Enum.GetValues(typeof(CompLevel)))
                m_compLevelNames.Add(comp.ToString());
        }

        var compLevel = m_archiveCollection.Definitions.CompLevelDefinition;
        if (args.Args.Count == 0)
        {
            HelionLog.Info($"CompLvl is {compLevel.CompLevel}");
            return;
        }

        string arg = args.Args[0];
        if (arg.EqualsIgnoreCase("none"))
        {
            compLevel.CompLevel = CompLevel.Undefined;
            m_config.Compatibility.ResetToUserValues();
            return;
        }

        for (int i = 0; i < m_compLevelNames.Count; i++)
        {
            if (arg.EqualsIgnoreCase(m_compLevelNames[i]))
            {
                m_config.Compatibility.ResetToUserValues();
                compLevel.CompLevel = (CompLevel)i;
                compLevel.Apply(m_config);
                return;
            }
        }

        HelionLog.Error("Invalid complvl");
    }

    private void ToggleMapOption(MapOptions option, ConsoleCommandEventArgs args)
    {
        if (m_layerManager.WorldLayer == null)
            return;

        var mapInfo = m_layerManager.WorldLayer.World.MapInfo;
        if (args.Args.Count > 0 && int.TryParse(args.Args[0], out int set))
        {
            mapInfo.SetOption(option, set != 0);
            return;
        }

        HelionLog.Info($"{option} is {mapInfo.HasOption(option)}");
    }

    private void DoInventoryAdd(ConsoleCommandEventArgs args)
    {
        if (m_layerManager.WorldLayer != null && args.Args.Count > 0)
        {
            var world = m_layerManager.WorldLayer.World;
            var def = world.EntityManager.DefinitionComposer.GetByName(args.Args[0]);
            if (def == null)
                return;

            m_layerManager.WorldLayer.World.Player.GiveItem(def, null, pickupFlash: false);
        }
    }

    private void DoInventoryRemove(ConsoleCommandEventArgs args)
    {
        if (m_layerManager.WorldLayer != null && args.Args.Count > 0)
            m_layerManager.WorldLayer.World.Player.Inventory.Remove(args.Args[0], int.MaxValue);
    }

    private void DoInventoryClear(ConsoleCommandEventArgs args)
    {
        if (m_layerManager.WorldLayer != null)
            m_layerManager.WorldLayer.World.Player.Inventory.Clear();
    }

    private void DoInventorySetAmount(ConsoleCommandEventArgs args)
    {
        if (m_layerManager.WorldLayer != null && args.Args.Count > 1 && int.TryParse(args.Args[1], out int amount))
        {
            var world = m_layerManager.WorldLayer.World;
            var def = world.EntityManager.DefinitionComposer.GetByName(args.Args[0]);
            if (def == null)
                return;

            m_layerManager.WorldLayer.World.Player.Inventory.SetAmount(def, amount);
        }
    }

    private void AddWorldResumeCommand(Action<ConsoleCommandEventArgs> action, ConsoleCommandEventArgs args)
    {
        if (m_layerManager.WorldLayer == null)
            return;

        if (!m_layerManager.WorldLayer.World.Paused)
        {
            action(args);
            return;
        }

        if (m_layerManager.WorldLayer != null)
            m_resumeCommands.Add(new Tuple<Action<ConsoleCommandEventArgs>, ConsoleCommandEventArgs>(action, args));
    }

    private void Console_OnCommand(object? sender, ConsoleCommandEventArgs args)
    {
        if (TryHandleConsoleCommand(args))
            return;

        if (TryHandleCheatCommand(args))
            return;

        if (TryHandleConfigVariableCommand(args))
            return;

        if (!Constants.BaseCommands.Contains(args.Command) && !Constants.InGameCommands.Contains(args.Command))
            HelionLog.Warn($"No such command or config variable: {args.Command}");
    }

    private bool TryHandleCheatCommand(ConsoleCommandEventArgs args)
    {
        if (m_layerManager.WorldLayer == null)
            return false;

        List<Player>? players = m_layerManager.WorldLayer.World.EntityManager.Players;
        if (players == null || players.Empty())
            return false;

        return m_layerManager.WorldLayer.World.CheatManager.HandleCommand(players[0], args.Command);
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

        if (component.Attribute.Legacy)
            return false;

        if (args.Args.Empty())
        {
            HelionLog.Info($"{component.Path} is {component.Value}");
            return true;
        }

        if (m_layerManager.WorldLayer != null && component.Attribute.Demo)
        {
            if (m_layerManager.WorldLayer.World.PlayingDemo)
            {
                HelionLog.Warn($"{args.Command} cannot be changed during demo playback");
                return true;
            }

            if (m_demoRecorder != null)
            {
                Log.Warn($"{args.Command} cannot be changed during recording");
                return true;
            }
        }

        bool success = true;
        ConfigSetResult result = component.Value.Set(args.Args[0]);
        switch (result)
        {
            case ConfigSetResult.Set:
                HelionLog.Info($"Set {args.Command} to {component.Value.ObjectValue}");
                break;
            case ConfigSetResult.Unchanged:
                HelionLog.Info($"{args.Command} set to the same value as before");
                break;
            case ConfigSetResult.Queued:
                HelionLog.Info($"{args.Command} has been queued up for change: {component.Value.SetFlags}");
                break;
            case ConfigSetResult.NotSetByBadConversion:
                success = false;
                HelionLog.Warn($"{args.Command} could not be set, incompatible argument");
                break;
            case ConfigSetResult.NotSetByFilter:
                success = false;
                HelionLog.Warn($"{args.Command} could not be set, out of range or invalid argument");
                break;
            default:
                success = false;
                HelionLog.Error($"{args.Command} unexpected setting result, report to a developer!");
                break;
        }

        if (success && component.Attribute.GetSetWarningString(out var warning))
            HelionLog.Warn(warning);

        return true;
    }

    private async Task NewGame(MapInfoDef mapInfo)
    {
        m_globalData = new();
        PrepLoadMap();
        await LoadMapAsync(mapInfo, null, null);
        InitializeDemoRecorderFromCommandArgs();
    }

    private MapInfoDef GetMapInfo(string mapName) =>
        m_archiveCollection.Definitions.MapInfoDefinition.MapInfo.GetMapInfoOrDefault(mapName);

    private readonly List<Tuple<Action<ConsoleCommandEventArgs>, ConsoleCommandEventArgs>> m_resumeCommands = new();

    private IRandom GetLoadMapRandom(MapInfoDef mapInfoDef, WorldModel? worldModel, IWorld? previousWorld)
    {
        if (previousWorld != null)
            return previousWorld.Random;

        if (worldModel != null)
            return new DoomRandom(worldModel.RandomIndex);

        var demoMap = GetDemoMap(mapInfoDef.MapName);
        if (m_demoPlayer != null && demoMap != null)
            return new DoomRandom(demoMap.RandomIndex);

        return new DoomRandom();
    }

    private void PrepLoadMap()
    {
        m_layerManager.LockInput = true;
        m_layerManager.WorldLayer?.Stop();
    }

    private async Task LoadMapAsync(MapInfoDef mapInfoDef, WorldModel? worldModel, IWorld? previousWorld, LevelChangeEvent? eventContext = null,
        bool showLoadingTitlepic = true)
    {
        var loadingLayer = m_layerManager.LoadingLayer;
        if (loadingLayer == null)
        {
            loadingLayer = new(m_archiveCollection, m_config, string.Empty);
            m_layerManager.Add(loadingLayer);
        }

        loadingLayer.LoadingText = $"Loading {mapInfoDef.GetDisplayNameWithPrefix(m_archiveCollection)}...";
        loadingLayer.LoadingImage = m_archiveCollection.GameInfo.TitlePage;

        UnRegisterWorldEvents();

        m_layerManager.ClearAllExcept(loadingLayer);
        m_archiveCollection.DataCache.FlushReferences();

        await Task.Run(() => LoadMap(mapInfoDef, worldModel, previousWorld, eventContext));

        // Signal the client to finalizing loading on the main thread. OpenGL can't do things outside of the main thread.
        m_loadCompleteModel = worldModel;
        m_loadComplete = true;
    }

    private void LoadMap(MapInfoDef mapInfoDef, WorldModel? worldModel, IWorld? previousWorld, LevelChangeEvent? eventContext = null)
    {
        IList<Player> players = Array.Empty<Player>();
        IRandom random = GetLoadMapRandom(mapInfoDef, worldModel, previousWorld);
        int randomIndex = random.RandomIndex;

        if (previousWorld != null)
            players = previousWorld.EntityManager.Players;

        m_lastWorldModel = worldModel;
        IMap? map = m_archiveCollection.FindMap(mapInfoDef.MapName);
        if (map == null)
        {
            LogError($"Cannot load map '{mapInfoDef.MapName}', it cannot be found or is corrupt");
            return;
        }

        if (!m_zdbsp.RunZdbsp(map, map.Name, mapInfoDef, out map))
        {
            Log.Error("Failed to run zdbsp.");
            return;
        }

        m_config.ApplyQueuedChanges(ConfigSetFlags.OnNewWorld);
        SkillDef? skillDef = GetSkillDefinition(worldModel);
        if (skillDef == null)
        {
            LogError($"Could not find skill definition for {m_config.Game.Skill}");
            return;
        }

        UnRegisterWorldEvents();
        m_window.InputManager.Clear();
        m_resumeCommands.Clear();

        if (map == null)
        {
            LogError($"Cannot load map '{mapInfoDef.MapName}', it cannot be found or is corrupt");
            return;
        }

        m_layerManager.Remove(m_layerManager.WorldLayer);
        m_archiveCollection.DataCache.FlushReferences();

        // Don't show the spinner here. The final steps requires OpenGL calls that are required to be executed on the main thread for now so the spinner can't update.
        if (m_layerManager.LoadingLayer != null)
            m_layerManager.LoadingLayer.ShowSpinner = false;

        WorldLayer? newLayer = WorldLayer.Create(m_layerManager, m_globalData, m_config, m_console,
            m_audioSystem, m_archiveCollection, m_fpsTracker, m_profiler, mapInfoDef, skillDef, map,
            players.FirstOrDefault(), worldModel, random);
        if (newLayer == null)
            return;

        if (!m_globalData.VisitedMaps.Contains(mapInfoDef))
            m_globalData.VisitedMaps.Add(mapInfoDef);
        RegisterWorldEvents(newLayer);

        m_layerManager.Add(newLayer);
        m_layerManager.ClearAllExcept(newLayer, m_layerManager.LoadingLayer);

        if (players.Count > 0 && m_config.Game.AutoSave)
        {
            string title = $"Auto: {mapInfoDef.GetMapNameWithPrefix(newLayer.World.ArchiveCollection)}";
            SaveGameEvent saveGameEvent = m_saveGameManager.WriteNewSaveGame(newLayer.World, title, autoSave: true);
            if (saveGameEvent.Success)
                m_console.AddMessage($"Saved {saveGameEvent.FileName}");

            if (!saveGameEvent.Success)
            {
                m_console.AddMessage($"Failed to save {saveGameEvent.FileName}");
                if (saveGameEvent.Exception != null)
                    throw saveGameEvent.Exception;
            }
        }

        if (m_demoPlayer != null)
            SetWorldLayerToDemo(m_demoPlayer, mapInfoDef, newLayer);

        if (m_demoRecorder != null)
        {
            var worldPlayer = newLayer.World.Player;
            // Cheat events reset the player, do not serialize the player
            if (eventContext != null && eventContext.ChangeType == LevelChangeType.SpecificLevel)
                worldPlayer = null;

            AddDemoMap(m_demoRecorder, newLayer.CurrentMap.MapName, randomIndex, worldPlayer);
            newLayer.StartRecording(m_demoRecorder);
        }
    }

    private SkillDef? GetSkillDefinition(WorldModel? worldModel)
    {
        if (m_config.Game.SelectedSkillDefinition != null)
            return m_config.Game.SelectedSkillDefinition;

        var skill = m_config.Game.Skill.Value;
        if (worldModel != null)
            skill = worldModel.Skill;

        return m_archiveCollection.Definitions.MapInfoDefinition.MapInfo.GetSkill(skill);
    }

    private void RegisterWorldEvents(WorldLayer newLayer)
    {
        newLayer.World.LevelExit += World_LevelExit;
        newLayer.World.WorldResumed += World_WorldResumed;
        newLayer.World.ClearConsole += World_ClearConsole;
    }

    private void World_ClearConsole(object? sender, EventArgs e)
    {
        m_layerManager.Remove(m_layerManager.ConsoleLayer);
    }

    private void UnRegisterWorldEvents()
    {
        if (m_layerManager.WorldLayer == null)
            return;

        m_layerManager.WorldLayer.World.LevelExit -= World_LevelExit;
        m_layerManager.WorldLayer.World.WorldResumed -= World_WorldResumed;
        m_layerManager.WorldLayer.World.ClearConsole -= World_ClearConsole;
    }

    private void World_WorldResumed(object? sender, EventArgs e)
    {
        foreach (var cmd in m_resumeCommands)
            cmd.Item1(cmd.Item2);

        m_resumeCommands.Clear();
    }

    private async void World_LevelExit(object? sender, LevelChangeEvent e)
    {
        try
        {
            if (sender is not IWorld world || e.Cancel)
                return;

            if (m_config.Game.LevelStat && ShouldWriteStatsFile(e.ChangeType))
                WriteStatsFile(world);

            m_isSecretExit = false;
            switch (e.ChangeType)
            {
                case LevelChangeType.Next:
                    await Intermission(world, () => GetNextLevel(world.MapInfo));
                    break;

                case LevelChangeType.SecretNext:
                    m_isSecretExit = true;
                    await Intermission(world, () => GetNextSecretLevel(world.MapInfo));
                    break;

                case LevelChangeType.SpecificLevel:
                    await ChangeLevel(world, e);
                    break;

                case LevelChangeType.Reset:
                    PrepLoadMap();
                    await LoadMapAsync(world.MapInfo, null, null, e, showLoadingTitlepic: false);
                    break;

                case LevelChangeType.ResetOrLoadLast:
                    PrepLoadMap();
                    await LoadMapAsync(world.MapInfo, m_lastWorldModel, null, e, showLoadingTitlepic: false);
                    break;
            }
        }
        catch (Exception ex)
        {
            HandleFatalException(ex);
        }
    }

    private static bool ShouldWriteStatsFile(LevelChangeType type) =>
        type == LevelChangeType.Next || type == LevelChangeType.SecretNext;

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
            TimeSpan levelTime = TimeSpan.FromSeconds(world.LevelTime / Constants.TicksPerSecond);
            TimeSpan totalTime = TimeSpan.FromSeconds(world.GlobalData.TotalTime / Constants.TicksPerSecond);
            using StreamWriter sw = File.AppendText(StatFile);
            sw.WriteLine(string.Format("{0} - {1} ({2})  K: {3}/{4}  I: {5}/{6}  S: {7}/{8}", world.MapInfo.MapName,
                $"{levelTime.Minutes}:{levelTime.Seconds}.{levelTime.Milliseconds}", $"{totalTime.Minutes}:{totalTime.Seconds}",
                world.LevelStats.KillCount, world.LevelStats.TotalMonsters,
                world.LevelStats.ItemCount, world.LevelStats.TotalItems,
                world.LevelStats.SecretCount, world.LevelStats.TotalSecrets));
        }
        catch (Exception e)
        {
            Log.Error($"Failed to write {StatFile} - {e}");
        }
    }

    private async Task Intermission(IWorld world, Func<FindMapResult> getNextMapInfo)
    {
        if (world.MapInfo.HasOption(MapOptions.NoIntermission))
        {
            await EndGame(world, getNextMapInfo);
        }
        else
        {
            IntermissionLayer intermissionLayer = new(m_layerManager, world, m_soundManager, m_audioSystem.Music,
                world.MapInfo, getNextMapInfo);
            intermissionLayer.Exited += IntermissionLayer_Exited;
            m_layerManager.Add(intermissionLayer);
        }
    }

    private async void IntermissionLayer_Exited(object? sender, EventArgs e)
    {
        if (sender is not IntermissionLayer intermissionLayer)
            return;

        await EndGame(intermissionLayer.World, intermissionLayer.GetNextMapInfo);
        m_layerManager.Remove(m_layerManager.IntermissionLayer);
    }

    private async Task EndGame(IWorld world, Func<FindMapResult> getNextMapInfo)
    {
        try
        {
            var nextMapResult = getNextMapInfo();
            var nextMapInfo = nextMapResult.MapInfo;
            ClusterDef? cluster = m_archiveCollection.Definitions.MapInfoDefinition.MapInfo.GetCluster(world.MapInfo.Cluster);
            ClusterDef? nextCluster = null;
            if (nextMapInfo != null)
                nextCluster = m_archiveCollection.Definitions.MapInfoDefinition.MapInfo.GetCluster(nextMapInfo.Cluster);

            bool isChangingClusters = cluster != null && nextMapInfo != null && world.MapInfo.Cluster != nextMapInfo.Cluster;
            if (cluster != null && isChangingClusters)
            {
                bool hasExitText = m_isSecretExit ? cluster.SecretExitText.Count > 0 : cluster.ExitText.Count > 0;
                if (!hasExitText && nextCluster == null)
                    isChangingClusters = false;
                if (!hasExitText && nextCluster != null && nextCluster.EnterText.Count == 0)
                    isChangingClusters = false;
            }

            if (isChangingClusters || world.MapInfo.EndGame != null || nextMapResult.Options.HasFlag(FindMapResultOptions.EndGame))
            {
                HandleZDoomTransition(world, cluster, nextCluster, nextMapInfo);
            }
            else if (nextMapInfo != null)
            {
                PrepLoadMap();
                await LoadMapAsync(nextMapInfo, null, world, showLoadingTitlepic: false);
            }

            if (!string.IsNullOrEmpty(nextMapResult.Error))
            {
                m_layerManager.ClearAllExcept();
                Log.Error(nextMapResult.Error);
                ShowConsole();
            }
        }
        catch (Exception e)
        {
            HandleFatalException(e);
        }
    }

    private void HandleZDoomTransition(IWorld world, ClusterDef? cluster, ClusterDef? nextCluster, MapInfoDef? nextMapInfo)
    {
        if (cluster == null)
            return;

        EndGameLayer endGameLayer = new(m_archiveCollection, m_audioSystem.Music, m_soundManager, world, cluster, nextCluster, nextMapInfo, m_isSecretExit);
        endGameLayer.Exited += EndGameLayer_Exited;

        m_layerManager.Add(endGameLayer);
    }

    private async void EndGameLayer_Exited(object? sender, EventArgs e)
    {
        try
        {
            if (sender is not EndGameLayer endGameLayer)
                return;

            if (endGameLayer.NextMapInfo != null)
            {
                PrepLoadMap();
                await LoadMapAsync(endGameLayer.NextMapInfo, null, endGameLayer.World, showLoadingTitlepic: false);
            }
        }
        catch (Exception ex)
        {
            HandleFatalException(ex);
        }
    }

    private async Task ChangeLevel(IWorld world, LevelChangeEvent e)
    {
        if (!MapWarp.GetMap(e.LevelNumber, m_archiveCollection, out MapInfoDef? mapInfoDef))
        {
            Log.Error($"Could not find map for {e.LevelNumber}");
            return;
        }

        if (e.IsCheat)
            world.DisplayMessage("$STSTR_CLEV");

        PrepLoadMap();
        await LoadMapAsync(mapInfoDef, null, null, e);
    }

    private FindMapResult GetNextLevel(MapInfoDef mapDef) =>
        m_archiveCollection.Definitions.MapInfoDefinition.MapInfo.GetNextMap(mapDef);

    private FindMapResult GetNextSecretLevel(MapInfoDef mapDef) =>
        m_archiveCollection.Definitions.MapInfoDefinition.MapInfo.GetNextSecretMap(mapDef);

    private void ShowConsole()
    {
        m_layerManager.ShowConsole();
    }

    private void LogError(string error)
    {
        Log.Error(error);
        ShowConsole();
    }
}
