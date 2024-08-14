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
    private readonly string? m_saveDirCommandLineArg;

    public event EventHandler<SaveGameEvent>? GameSaved;

    public SaveGameManager(IConfig config, string? saveDirCommandLineArg)
    {
        m_config = config;
        m_saveDirCommandLineArg = saveDirCommandLineArg;
    }

    public string GetSaveDir()
    {
        if (string.IsNullOrEmpty(m_saveDirCommandLineArg))
            return Directory.GetCurrentDirectory();

        if (!EnsureDirectoryExists(m_saveDirCommandLineArg))
            return Directory.GetCurrentDirectory();

        return m_saveDirCommandLineArg;
    }

    public static bool EnsureDirectoryExists(string path)
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

    public bool SaveFileExists(string? baseDir, string filename)
    {
        string filePath = Path.Combine(baseDir ?? GetSaveDir(), filename);
        return File.Exists(filePath);
    }

    public SaveGame ReadSaveGame(string dir, string filename) => new SaveGame(dir, filename);

    public SaveGameEvent WriteNewSaveGame(IWorld world, string title, bool autoSave = false) =>
        WriteSaveGame(world, title, null, autoSave);

    public SaveGameEvent WriteSaveGame(IWorld world, string title, SaveGame? existingSave, bool autoSave = false)
    {
        SaveGameEvent saveEvent = existingSave != null
            ? SaveGame.WriteSaveGame(world, title, existingSave.SaveDir, existingSave.FileName)
            : SaveGame.WriteSaveGame(
                world,
                title,
                m_config.Game.UseSavedGameOrganizer ? GetOrganizedSaveDir(world.ArchiveCollection) : GetSaveDir(),
                GetNewSaveName(world, autoSave));

        GameSaved?.Invoke(this, saveEvent);
        return saveEvent;
    }

    public IEnumerable<SaveGame> GetMatchingSaveGames(IEnumerable<SaveGame> saveGames,
        ArchiveCollection archiveCollection)
    {
        return saveGames.Where(x => x.Model != null &&
            ModelVerification.VerifyModelFiles(x.Model.Files, archiveCollection, null));
    }

    public List<SaveGame> GetSaveGames(string saveDir)
    {
        return Directory.GetFiles(saveDir, "*.hsg")
            .Select(f => new SaveGame(saveDir, f))
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

    private string GetNewSaveName(IWorld world, bool autoSave)
    {
        string saveDir = m_config.Game.UseSavedGameOrganizer
            ? GetOrganizedSaveDir(world.ArchiveCollection)
            : GetSaveDir();

        List<string> files = Directory.GetFiles(saveDir, "*.hsg")
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

    public string GetOrganizedSaveDir(ArchiveCollection archiveCollection)
    {
        string path = Path.Join(GetSaveDir(),
            Path.Join(archiveCollection.GetIdentifiers().ToArray()));

        return EnsureDirectoryExists(path)
            ? path
            : Directory.GetCurrentDirectory();
    }

    private static string GetSaveName(int number, bool autoSave)
    {
        if (autoSave)
            return $"autosave{number}.hsg";
        return $"savegame{number}.hsg";
    }
}
