using System.Collections.Generic;
using System.IO;
using Helion.Layer.Consoles;
using Helion.Layer.Images;
using Helion.Resources.Definitions.MapInfo;
using Helion.Resources.IWad;
using Helion.Util;
using Helion.Util.Consoles;
using Helion.World.Util;

namespace Helion.Client;

public partial class Client
{
    private void Initialize()
    {
        LoadFiles();

        if (m_commandLineArgs.Skill.HasValue)
            SetSkill(m_commandLineArgs.Skill.Value);

        m_config.Game.NoMonsters.Set(m_commandLineArgs.NoMonsters);
        m_config.Game.LevelStat.Set(m_commandLineArgs.LevelStat);
        m_config.Game.FastMonsters.Set(m_commandLineArgs.SV_FastMonsters);

        if (m_commandLineArgs.LevelStat)
            ClearStatsFile();

        if (m_commandLineArgs.LoadGame != null)
        {
            ConsoleCommandEventArgs args = new($"load {m_commandLineArgs.LoadGame}");
            CommandLoadGame(args);
        }
        else
        {
            CheckLoadMap();
            AddTitlepicIfNoMap();
        }
    }

    private void AddTitlepicIfNoMap()
    {
        if (m_layerManager.WorldLayer != null)
            return;

        TitlepicLayer layer = new(m_layerManager, m_config, m_console, m_soundManager, m_archiveCollection,
            m_saveGameManager, m_audioSystem);
        m_layerManager.Add(layer);
    }

    private void LoadFiles()
    {
        if (!m_archiveCollection.Load(m_commandLineArgs.Files, GetIwad(),
            dehackedPatch: m_commandLineArgs.DehackedPatch))
        {
            if (m_archiveCollection.Assets == null)
                ShowFatalError($"Failed to load {Constants.AssetsFileName}.");
            else if (m_archiveCollection.IWad == null)
                ShowFatalError("Failed to load IWAD.");
            else
                ShowFatalError("Failed to load files.");
        }
    }

    private void CheckLoadMap()
    {
        bool tryLoadMap = m_commandLineArgs.Map != null || m_commandLineArgs.Warp != null;

        if (m_commandLineArgs.PlayDemo != null &&
            TryCreateDemoPlayer(m_commandLineArgs.PlayDemo, out m_demoPlayer))
        {
            // Check if a specific map was loaded. If not load the first map in the demo file.
            if (!tryLoadMap && m_demoModel != null && m_demoModel.Maps.Count > 0)
                LoadMap(m_demoModel.Maps[0].Map);
        }

        if (m_commandLineArgs.Map != null)
        {
            LoadMap(m_commandLineArgs.Map);
        }
        else if (m_commandLineArgs.Warp != null &&
            MapWarp.GetMap(m_commandLineArgs.Warp, m_archiveCollection.Definitions.MapInfoDefinition.MapInfo,
                out MapInfoDef? mapInfoDef) && mapInfoDef != null)
        {
            LoadMap(mapInfoDef.MapName);
        }

        InitializeDemoRecorderFromCommandArgs();
    }

    private string? GetIwad()
    {
        if (m_commandLineArgs.Iwad != null)
            return m_commandLineArgs.Iwad;

        string? iwad = LocateIwad();
        if (iwad != null)
            return iwad;

        Log.Error("No IWAD found!");
        return null;
    }

    private static string? LocateIwad()
    {
        IWadLocator iwadLocator = new(new[] { Directory.GetCurrentDirectory() });
        List<(string, IWadInfo)> iwadData = iwadLocator.Locate();
        return iwadData.Count > 0 ? iwadData[0].Item1 : null;
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
        return mapInfo.GetMap(startMapName);
    }

    private void SetSkill(int value)
    {
        if (value > 0 && value < 6)
            m_config.Game.Skill.Set((Maps.Shared.SkillLevel)value);
        else
            Log.Info($"Invalid skill level: {value}");
    }

    private void LoadMap(string mapName)
    {
        m_console.ClearInputText();
        m_console.AddInput($"map {mapName}\n");

        if (m_layerManager.WorldLayer == null && m_layerManager.ConsoleLayer != null)
        {
            ConsoleLayer layer = new(m_archiveCollection.GameInfo.TitlePage, m_config, m_console, m_consoleCommands);
            m_layerManager.Add(layer);
        }
    }
}
