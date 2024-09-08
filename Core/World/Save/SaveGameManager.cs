using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Helion.Models;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using Helion.Util.Extensions;
using Helion.World.Util;
using NLog;

namespace Helion.World.Save;

public readonly struct SaveGameEvent
{
    public readonly SaveGame SaveGame;
    public readonly WorldModel WorldModel;
    public readonly string FileName;
    public readonly bool Success;
    public readonly Exception? Exception;

    public SaveGameEvent(SaveGame saveGame, WorldModel worldModel, string filename, bool success, Exception? ex = null)
    {
        SaveGame = saveGame;
        FileName = filename;
        WorldModel = worldModel;
        Success = success;
        Exception = ex;
    }
}

public class SaveGameManager
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly IConfig m_config;
    private readonly ArchiveCollection m_archiveCollection;
    private readonly string? m_saveDirCommandLineArg;

    public event EventHandler<SaveGameEvent>? GameSaved;

    public SaveGameManager(IConfig config, ArchiveCollection archiveCollection, string? saveDirCommandLineArg)
    {
        m_config = config;
        m_archiveCollection = archiveCollection;
        m_saveDirCommandLineArg = saveDirCommandLineArg;
    }

    private string GetSaveDir()
    {
        if (string.IsNullOrEmpty(m_saveDirCommandLineArg))
            return Directory.GetCurrentDirectory();

        if (!EnsureDirectoryExists(m_saveDirCommandLineArg))
            return Directory.GetCurrentDirectory();

        return m_saveDirCommandLineArg;
    }

    private static bool EnsureDirectoryExists(string path)
    {
        if (Directory.Exists(path))
            return true;

        try
        {
            Directory.CreateDirectory(path);
            return true;
        }
        catch
        {
            Log.Error("Failed to create directory {dir}", path);
            return false;
        }
    }

    public bool SaveFileExists(string filename)
    {
        string filePath = Path.Combine(GetSaveDir(), filename);
        return File.Exists(filePath);
    }

    public SaveGame ReadSaveGame(string filename) => new SaveGame(GetSaveDir(), filename);

    public SaveGameEvent WriteNewSaveGame(IWorld world, string title, bool autoSave = false) =>
        WriteSaveGame(world, title, null, autoSave);

    public SaveGameEvent WriteSaveGame(IWorld world, string title, SaveGame? existingSave, bool autoSave = false)
    {
        if (existingSave == null && autoSave && m_config.Game.RotateAutoSaves.Value)
        {
            var autoSaves = GetSaveGames().Where(x => x.IsAutoSave);
            var matchingSaves = GetMatchingSaveGames(autoSaves).OrderBy(x => x.Model?.Date);
            if (matchingSaves.Any() && matchingSaves.Count() >= m_config.Game.MaxAutoSaves.Value)
                existingSave = matchingSaves.First();
        }
        string filename = existingSave?.FileName ?? GetNewSaveName(autoSave);
        var saveEvent = SaveGame.WriteSaveGame(world, title, GetSaveDir(), filename);

        GameSaved?.Invoke(this, saveEvent);
        return saveEvent;
    }

    public List<SaveGame> GetSortedSaveGames()
    {
        var saveGames = GetSaveGames();
        var matchingGames = GetMatchingSaveGames(saveGames);
        var nonMatchingGames = saveGames.Except(matchingGames);
        return matchingGames.Union(nonMatchingGames).ToList();
    }

    public IEnumerable<SaveGame> GetMatchingSaveGames(IEnumerable<SaveGame> saveGames)
    {
        return saveGames.Where(x => x.Model != null &&
            ModelVerification.VerifyModelFiles(x.Model.Files, m_archiveCollection, null));
    }

    public List<SaveGame> GetSaveGames()
    {
        return Directory.GetFiles(GetSaveDir(), "*.hsg")
            .Select(f => new SaveGame(GetSaveDir(), Path.GetFileName(f)))
            .OrderByDescending(f => f.Model?.Date)
            .ToList();
    }

    public bool DeleteSaveGame(SaveGame saveGame)
    {
        try
        {
            if (File.Exists(saveGame.FilePath))
                File.Delete(saveGame.FilePath);
        }
        catch
        {
            return false;
        }

        return true;
    }

    private string GetNewSaveName(bool autoSave)
    {
        List<string> files = Directory.GetFiles(GetSaveDir(), "*.hsg")
            .Select(Path.GetFileName)
            .WhereNotNull()
            .ToList();

        int number = 0;
        while (true)
        {
            string name = GetSaveName(number, autoSave);
            if (files.Any(x => x.Equals(name, StringComparison.OrdinalIgnoreCase)))
                number++;
            else
                return name;
        }
    }

    private static string GetSaveName(int number, bool autoSave)
    {
        if (autoSave)
            return $"autosave{number}.hsg";
        return $"savegame{number}.hsg";
    }
}
