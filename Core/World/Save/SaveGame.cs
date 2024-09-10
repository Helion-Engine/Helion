using Helion.Models;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Helion.World.Save;

public class SaveGame
{
    private static readonly string SaveDataFile = "save.json";
    private static readonly string WorldDataFile = "world.json";

    private static readonly JsonSerializerSettings DefaultSerializerSettings = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore,
        MissingMemberHandling = MissingMemberHandling.Ignore,
        TypeNameHandling = TypeNameHandling.Auto,
        DefaultValueHandling = DefaultValueHandling.Ignore
    };

    public readonly SaveGameModel? Model;

    public readonly string SaveDir;

    public readonly string FileName;

    public string FilePath => Path.Combine(SaveDir, FileName);

    public bool IsAutoSave => Path.GetFileName(FileName).StartsWith("autosave");

    public bool IsQuickSave => Path.GetFileName(FileName).StartsWith("quicksave");

    public SaveGame(string saveDir, string filename, SaveGameModel model)
    {
        SaveDir = saveDir;
        FileName = filename;
        Model = model;
    }

    public SaveGame(string saveDir, string filename)
    {
        SaveDir = saveDir;
        FileName = filename;

        try
        {
            using ZipArchive zipArchive = ZipFile.Open(FilePath, ZipArchiveMode.Read);
            ZipArchiveEntry? saveDataEntry = zipArchive.Entries.FirstOrDefault(x => x.Name.Equals(SaveDataFile));
            if (saveDataEntry == null)
                return;

            Model = JsonConvert.DeserializeObject<SaveGameModel>(saveDataEntry.ReadDataAsString(), DefaultSerializerSettings);
        }
        catch
        {
            // Corrupt zip or bad serialize
        }
    }

    public WorldModel? ReadWorldModel()
    {
        if (Model == null)
            return null;

        try
        {
            using ZipArchive zipArchive = ZipFile.Open(FilePath, ZipArchiveMode.Read);
            ZipArchiveEntry? entry = zipArchive.Entries.FirstOrDefault(x => x.Name.Equals(Model.WorldFile));
            if (entry == null)
                return null;

            return JsonConvert.DeserializeObject<WorldModel>(entry.ReadDataAsString(), DefaultSerializerSettings);
        }
        catch
        {
            return null;
        }
    }

    public static SaveGameEvent WriteSaveGame(IWorld world, string title, string saveDir, string filename)
    {
        SaveGameModel saveGameModel = new()
        {
            Text = title,
            MapName = world.MapInfo.GetMapNameWithPrefix(world.ArchiveCollection),
            Date = DateTime.Now,
            WorldFile = "world.json",
            Files = world.GetGameFilesModel()
        };

        string saveTempFile = TempFileManager.GetFile();
        WorldModel worldModel = world.ToWorldModel();

        try
        {
            File.Delete(saveTempFile);
            using ZipArchive zipArchive = ZipFile.Open(saveTempFile, ZipArchiveMode.Create);
            ZipArchiveEntry entry = zipArchive.CreateEntry(SaveDataFile);
            using (Stream stream = entry.Open())
                stream.Write(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(saveGameModel, DefaultSerializerSettings)));

            entry = zipArchive.CreateEntry(WorldDataFile);
            using (Stream stream = entry.Open())
                stream.Write(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(worldModel, DefaultSerializerSettings)));
        }
        catch (Exception ex)
        {
            return new SaveGameEvent(new SaveGame(saveDir, filename, saveGameModel), worldModel, filename, false, ex);
        }

        SaveGame saveGame = new(saveDir, filename, saveGameModel);

        if (File.Exists(saveGame.FilePath))
            File.Delete(saveGame.FilePath);

        File.Copy(saveTempFile, saveGame.FilePath);

        return new SaveGameEvent(saveGame, worldModel, filename, true);
    }
}
