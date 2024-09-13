using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Helion.Layer.Consoles;
using Helion.Layer.Images;
using Helion.Layer.IwadSelection;
using Helion.Resources.Definitions.MapInfo;
using Helion.Resources.IWad;
using Helion.Util;
using Helion.Util.CommandLine;
using Helion.Util.Consoles;
using Helion.World.Util;

namespace Helion.Client;

public partial class Client
{
    private readonly List<IWadPath> m_iwads = [];

    private async Task Initialize(string? iwad = null)
    {
        try
        {
            m_archiveCollection.Load(Array.Empty<string>());
            m_layerManager.Remove(m_layerManager.LoadingLayer);
            LoadingLayer loadingLayer = new(m_archiveCollection, m_config, "Loading files...");
            m_layerManager.Add(loadingLayer);

            if (iwad == null && m_iwads.Count == 0)
                await Task.Run(FindInstalledIWads);

            if (iwad == null && GetIwad(m_iwads) == null)
            {
                IwadSelectionLayer selectionlayer = new(m_archiveCollection, m_config, m_iwads);
                selectionlayer.OnIwadSelected += IwadSelection_OnIwadSelected;
                m_layerManager.Add(selectionlayer);
                m_layerManager.Remove(m_layerManager.LoadingLayer);
                return;
            }

            bool success = await Task.Run(() => LoadFiles(iwad));
            m_layerManager.Remove(m_layerManager.LoadingLayer);
            if (!success)
            {
                ShowConsole();
                return;
            }

            if (m_commandLineArgs.Skill.HasValue)
                SetSkill(m_commandLineArgs.Skill.Value);

            m_config.Game.NoMonsters.Set(m_commandLineArgs.NoMonsters);
            m_config.Game.LevelStat.Set(m_commandLineArgs.LevelStat);
            m_config.Game.FastMonsters.Set(m_commandLineArgs.SV_FastMonsters);
            m_config.Game.PistolStart.Set(m_commandLineArgs.PistolStart);

            m_archiveCollection.Definitions.CompLevelDefinition.Apply(m_config);

            if (m_commandLineArgs.LevelStat)
                ClearStatsFile();

            if (m_commandLineArgs.LoadGame != null)
            {
                ConsoleCommandEventArgs args = new($"load \"{m_commandLineArgs.LoadGame}\"");
                CommandLoadGame(args);
            }
            else
            {
                var loadedMap = CheckLoadMap();
                if (!loadedMap)
                {
                    m_layerManager.Remove(m_layerManager.IwadSelectionLayer);
                    m_layerManager.Add(new TitlepicLayer(m_layerManager, m_archiveCollection, m_audioSystem));
                }
            }
        }
        catch (Exception e)
        {
            HandleFatalException(e);
        }
    }

    private void FindInstalledIWads()
    {
        var iwadLocator = IWadLocator.CreateDefault(m_config.Files.Directories.Value);
        m_iwads.AddRange(iwadLocator.Locate());
    }

    private async void IwadSelection_OnIwadSelected(object? sender, string iwad)
    {
        m_layerManager.Remove(m_layerManager.IwadSelectionLayer);
        await Initialize(iwad);
    }

    private bool LoadFiles(string? iwad = null)
    {
        if (!m_archiveCollection.Load(m_commandLineArgs.Files, iwad ?? GetIwad(m_iwads),
            dehackedPatch: m_commandLineArgs.DehackedPatch))
        {
            if (m_archiveCollection.Assets == null)
                ShowFatalError($"Failed to load {Constants.AssetsFileName}.");
            else if (m_archiveCollection.IWad == null)
                ShowFatalError("Failed to load IWAD.");
            else
                ShowFatalError("Failed to load files.");
            return false;
        }

        m_filesLoaded = true;
        return true;
    }

    private bool CheckLoadMap()
    {
        bool loadedMap = false;
        bool tryLoadMap = m_commandLineArgs.Map != null || m_commandLineArgs.Warp != null;

        if (m_commandLineArgs.PlayDemo != null &&
            TryCreateDemoPlayer(m_commandLineArgs.PlayDemo, out m_demoPlayer))
        {
            // Check if a specific map was loaded. If not load the first map in the demo file.
            if (!tryLoadMap && m_demoModel != null && m_demoModel.Maps.Count > 0)
            {
                loadedMap = true;
                LoadMap(m_demoModel.Maps[0].Map);
            }
        }

        if (m_commandLineArgs.Map != null)
        {
            loadedMap = true;
            LoadMap(m_commandLineArgs.Map, m_commandLineArgs);
        }
        else if (m_commandLineArgs.Warp != null &&
            MapWarp.GetMap(m_commandLineArgs.Warp, m_archiveCollection, out MapInfoDef? mapInfoDef))
        {
            loadedMap = true;
            LoadMap(mapInfoDef.MapName, m_commandLineArgs);
        }

        InitializeDemoRecorderFromCommandArgs();
        return loadedMap;
    }

    private string? GetIwad(List<IWadPath> iwads)
    {
        if (m_commandLineArgs.Iwad != null)
            return m_commandLineArgs.Iwad;

        // Only return if one is found, otherwise show the iwad selector
        if (iwads.Count == 1)
            return iwads[0].Path;

        return null;
    }

    private MapInfoDef? GetDefaultMap()
    {
        if (m_archiveCollection.Definitions.MapInfoDefinition.MapInfo.Episodes.Count == 0)
        {
            Log.Error("No episodes defined.");
            return null;
        }

        var mapInfo = m_archiveCollection.Definitions.MapInfoDefinition.MapInfo;
        string startMapName = mapInfo.Episodes[0].StartMap;
        return mapInfo.GetStartMapOrDefault(m_archiveCollection, startMapName);
    }

    private void SetSkill(int value)
    {
        if (value > 0 && value < 6)
            m_config.Game.Skill.Set((Maps.Shared.SkillLevel)value);
        else
            Log.Info($"Invalid skill level: {value}");
    }

    private void LoadMap(string mapName, CommandLineArgs? args = null)
    {
        QueueLoadMap(GetMapInfo(mapName), null, null, OnLoadMapCommandComplete, args);
    }

    private void OnLoadMapCommandComplete(object? value)
    {
        CommandLineArgs? args = value as CommandLineArgs;
        if (m_layerManager.WorldLayer == null && m_layerManager.ConsoleLayer != null)
        {
            ConsoleLayer layer = new(m_archiveCollection.GameInfo.TitlePage, m_config, m_console, m_consoleCommands);
            m_layerManager.Add(layer);
        }

        if (args == null || m_layerManager.WorldLayer == null)
            return;

        var world = m_layerManager.WorldLayer.World;
        if (args.Cheats != null)
        {
            var player = m_layerManager.WorldLayer.World.Player;
            foreach (string cheatCmd in args.Cheats)
                world.CheatManager.HandleCommand(player, cheatCmd);
        }

        if (args.SetPosition != null)
            world.SetEntityPosition(world.Player, args.SetPosition.Value);

        if (args.SetAngle != null)
        {
            world.Player.AngleRadians = MathHelper.ToRadians(args.SetAngle.Value);
            world.Player.ResetInterpolation();
        }

        if (args.SetPitch != null)
        {
            world.Player.PitchRadians = MathHelper.ToRadians(args.SetPitch.Value);
            world.Player.ResetInterpolation();
        }
    }
}
