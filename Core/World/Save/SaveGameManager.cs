using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Helion.Graphics;
using Helion.Models;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configs;
using Helion.World.Util;
using NLog;

namespace Helion.World.Save;

public readonly struct SaveGameEvent(SaveGame saveGame, WorldModel worldModel, string filename, bool success, Exception? ex = null, string errorMessage = "")
{
    public readonly SaveGame SaveGame = saveGame;
    public readonly WorldModel WorldModel = worldModel;
    public readonly string FileName = filename;
    public readonly bool Success = success;
    public readonly Exception? Exception = ex;
    public readonly string ErrorMessage = errorMessage;
}

public class SaveGameManager
{
    struct WriteSaveGameArgs(IWorld world, WorldModel worldModel, string title, string saveDir, string fileName, IScreenshotGenerator screenshotGenerator, Image? image)
    {
        public IWorld World = world;
        public WorldModel WorldModel = worldModel;
        public string Title = title;
        public string SaveDir = saveDir;
        public string FileName = fileName;
        public IScreenshotGenerator ScreenshotGenerator = screenshotGenerator;
        public Image? Image = image;
    }

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly SaveGameEvent ActiveSaveError = new(null!, null!, "file", false, null, "(Save in progress)");
    private readonly IConfig m_config;
    private readonly PathsManager m_pathsManager;
    private readonly ArchiveCollection m_archiveCollection;
    private readonly string? m_saveDirCommandLineArg;
    private readonly List<SaveGame> m_currentSaves = [];
    private readonly List<SaveGame> m_matchingSaves = [];
    private readonly Comparison<SaveGame> m_saveComparison = new(CompareSaveCompatibilityAndDates);
    private readonly Func<SaveGameEvent> m_saveFunc;
    private bool m_currentSavesLoaded;
    private bool m_saving;
    private WriteSaveGameArgs m_saveArgs;

    public event EventHandler<SaveGameEvent>? GameSaved;

    public SaveGameManager(IConfig config, PathsManager pathsManager, ArchiveCollection archiveCollection, string? saveDirCommandLineArg)
    {
        m_config = config;
        m_pathsManager = pathsManager;
        m_archiveCollection = archiveCollection;
        m_saveDirCommandLineArg = saveDirCommandLineArg;
        m_saveFunc = new Func<SaveGameEvent>(WriteSaveGameForTask);
        SaveGame.AllocateJsonMapping();
    }

    private string GetSaveDir()
    {
        if (!string.IsNullOrEmpty(m_saveDirCommandLineArg) && EnsureDirectoryExists(m_saveDirCommandLineArg))
            return m_saveDirCommandLineArg;

        return m_pathsManager.UserDataFolder;
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

    public void LoadCurrentSaveFiles()
    {
        if (m_currentSavesLoaded)
            return;

        var saves = ReadSaveGameFiles();
        foreach (var save in saves)
        {
            save.VerificationResult = CheckIfSaveModelCompatible(save.Model);
            m_currentSaves.Add(save);
            if (save.VerificationResult == SaveVerificationResult.Success)
                m_matchingSaves.Add(save);
        }
        m_currentSavesLoaded = true;
    }

    public bool SaveFileExists(string filename)
    {
        string filePath = Path.Combine(GetSaveDir(), filename);
        return File.Exists(filePath);
    }

    public SaveGame ReadSaveGame(string filename) => new(GetSaveDir(), filename);

    public Task<SaveGameEvent> WriteNewSaveGameAsync(IWorld world, string title, IScreenshotGenerator screenshotGenerator, SaveGameType type = SaveGameType.Default) =>
        WriteSaveGameAsync(world, title, screenshotGenerator, null, type);

    public async Task<SaveGameEvent> WriteSaveGameAsync(IWorld world, string title, IScreenshotGenerator screenshotGenerator, SaveGame? existingSave, SaveGameType type = SaveGameType.Default)
    {
        // Unlikely to happen but check if actively saving. Only one can be processed at a time.
        if (m_saving)
            return ActiveSaveError;

        m_saving = true;
        existingSave = GetExistingSave(existingSave, type);
        var filename = existingSave?.FileName ?? GetNewSaveName(type);
        var worldModel = world.ToWorldModel();
        var image = screenshotGenerator.GetImage();
        m_saveArgs = new(world, worldModel, title, GetSaveDir(), filename, screenshotGenerator, image);
        var saveEvent = await Task.Run(m_saveFunc);

        saveEvent.SaveGame.VerificationResult = SaveVerificationResult.Success;
        AddOrUpdateSaveGame(saveEvent.SaveGame);
        GameSaved?.Invoke(this, saveEvent);
        m_saving = false;
        return saveEvent;
    }

    private SaveGameEvent WriteSaveGameForTask() =>
        SaveGame.WriteSaveGame(m_saveArgs.World, m_saveArgs.WorldModel, m_saveArgs.Title, m_saveArgs.SaveDir, m_saveArgs.FileName, m_saveArgs.ScreenshotGenerator, m_saveArgs.Image);

    private void AddOrUpdateSaveGame(SaveGame newSaveGame)
    {
        AddOrUpdateSaveGame(newSaveGame, m_currentSaves);
        if (newSaveGame.VerificationResult == SaveVerificationResult.Success)
            AddOrUpdateSaveGame(newSaveGame, m_matchingSaves);
    }

    private static void AddOrUpdateSaveGame(SaveGame newSaveGame, List<SaveGame> saveList)
    {
        for (int i = 0; i < saveList.Count; i++)
        {
            var save = saveList[i];
            if (save.Type == newSaveGame.Type && save.FileName == newSaveGame.FileName)
            {
                saveList[i] = newSaveGame;
                return;
            }
        }

        saveList.Add(newSaveGame);
    }

    private SaveGame? GetExistingSave(SaveGame? existingSave, SaveGameType type)
    {
        var saveGames = GetSaveGames(compatibleOnly: true);

        if (existingSave == null && type == SaveGameType.Auto && m_config.Game.RotatingAutoSaves > 0)
        {
            var autoSaves = saveGames.Where(x => x.Type == SaveGameType.Auto);
            if (autoSaves.Any() && autoSaves.Count() >= m_config.Game.RotatingAutoSaves)
                existingSave = autoSaves.Last();
        }

        if (existingSave == null && type == SaveGameType.Quick && m_config.Game.RotatingQuickSaves > 0)
        {
            var quickSaves = saveGames.Where(x => x.Type == SaveGameType.Quick);
            if (quickSaves.Any() && quickSaves.Count() >= m_config.Game.RotatingQuickSaves)
                existingSave = quickSaves.Last();
        }

        return existingSave;
    }

    private SaveVerificationResult CheckIfSaveModelCompatible(SaveGameModel? model)
    {
        if (model == null)
            return SaveVerificationResult.Unknown;

        return ModelVerification.VerifyModelFiles(model.Files, m_archiveCollection, null);
    }

    /// <summary>
    /// Gets save games, ordered by compatible first, then newest first.
    /// </summary>
    public List<SaveGame> GetSaveGames(bool compatibleOnly = false)
    {
        LoadCurrentSaveFiles();
        var saves = compatibleOnly ? m_matchingSaves : m_currentSaves;
        saves.Sort(m_saveComparison);
        return saves;
    }

    private List<SaveGame> ReadSaveGameFiles()
    {
        return [.. Directory.GetFiles(GetSaveDir(), "*.hsg")
            .Select(f => new SaveGame(GetSaveDir(), Path.GetFileName(f)))
            .OrderByDescending(f => f.Model?.Date)];
    }

    public bool DeleteSaveGame(SaveGame saveGame)
    {
        try
        {
            if (File.Exists(saveGame.FilePath))
                File.Delete(saveGame.FilePath);

            m_currentSaves.Remove(saveGame);
        }
        catch
        {
            return false;
        }

        return true;
    }

    private string GetNewSaveName(SaveGameType type)
    {
        int number = 0;
        var searchSaves = m_currentSaves.Where(x => x.Type == type);
        while (true)
        {
            string name = GetSaveName(number, type);
            if (searchSaves.Any(x => x.FileName.Equals(name, StringComparison.OrdinalIgnoreCase)))
                number++;
            else
                return name;
        }
    }

    private static string GetSaveName(int number, SaveGameType type)
    {
        return type switch
        {
            SaveGameType.Auto => $"{SaveGame.AutoPrefix}{number}.hsg",
            SaveGameType.Quick => $"{SaveGame.QuickPrefix}{number}.hsg",
            _ => $"{SaveGame.DefaultPrefix}{number}.hsg",
        };
    }

    private static int CompareSaveCompatibilityAndDates(SaveGame x, SaveGame y)
    {
        if (x.Model == null)
            return 1;

        if (y.Model == null)
            return -1;

        // sort compatible saves first
        if (x.VerificationResult != y.VerificationResult)
        {
            if (x.VerificationResult == SaveVerificationResult.Success)
                return -1;
            if (y.VerificationResult == SaveVerificationResult.Success)
                return 1;
            return 0;
        }

        // then by most recent first
        return y.Model.Date.CompareTo(x.Model.Date);

    }
}
