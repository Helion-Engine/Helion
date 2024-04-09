using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Helion.Models;
using Helion.Resources.Archives.Collection;
using Helion.Util.CommandLine;
using Helion.Util.Configs;
using Helion.Util.Extensions;
using Helion.World.Util;

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
    private readonly IConfig m_config;
    private readonly CommandLineArgs m_commandLineArgs;

    public event EventHandler<SaveGameEvent>? GameSaved;

    public SaveGameManager(IConfig config, CommandLineArgs commandLineArgs)
    {
        m_config = config;
        m_commandLineArgs = commandLineArgs;
    }

    private string GetSaveDir() => m_commandLineArgs.SaveDir ?? Directory.GetCurrentDirectory();

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
        string filename = existingSave?.FileName ?? GetNewSaveName(autoSave);
        var saveEvent = SaveGame.WriteSaveGame(world, title, GetSaveDir(), filename);

        GameSaved?.Invoke(this, saveEvent);
        return saveEvent;
    }

    public List<SaveGame> GetSortedSaveGames(ArchiveCollection archiveCollection)
    {
        var saveGames = GetSaveGames();
        var matchingGames = GetMatchingSaveGames(saveGames, archiveCollection);
        var nonMatchingGames = saveGames.Except(matchingGames);
        return matchingGames.Union(nonMatchingGames).ToList();
    }

    public IEnumerable<SaveGame> GetMatchingSaveGames(IEnumerable<SaveGame> saveGames,
        ArchiveCollection archiveCollection)
    {
        return saveGames.Where(x => x.Model != null &&
            ModelVerification.VerifyModelFiles(x.Model.Files, archiveCollection, null));
    }

    public List<SaveGame> GetSaveGames()
    {
        return Directory.GetFiles(GetSaveDir(), "*.hsg")
            .Select(f => new SaveGame(GetSaveDir(), f))
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
