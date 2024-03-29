using Helion.Util.Configs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Helion.Util.Extensions;
using Helion.Resources.Archives.Collection;
using Helion.World.Util;
using Helion.Models;

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

    public event EventHandler<SaveGameEvent>? GameSaved;

    public SaveGameManager(IConfig config)
    {
        m_config = config;
    }

    public SaveGameEvent WriteNewSaveGame(IWorld world, string title, bool autoSave = false) =>
        WriteSaveGame(world, title, null, autoSave);

    public SaveGameEvent WriteSaveGame(IWorld world, string title, SaveGame? existingSave, bool autoSave = false)
    {
        string filename = existingSave?.FileName ?? GetNewSaveName(autoSave);
        var saveEvent = SaveGame.WriteSaveGame(world, title, filename);

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
        return Directory.GetFiles(Directory.GetCurrentDirectory(), "*.hsg")
            .Select(f => new SaveGame(f))
            .OrderByDescending(f => f.Model?.Date)
            .ToList();
    }

    public bool DeleteSaveGame(SaveGame saveGame)
    {
        try
        {
            if (File.Exists(saveGame.FileName))
                File.Delete(saveGame.FileName);
        }
        catch
        {
            return false;
        }

        return true;
    }

    private string GetNewSaveName(bool autoSave)
    {
        List<string> files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.hsg")
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
