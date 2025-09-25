using Helion.Graphics;
using Helion.Models;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.Util.Container;
using Helion.Util.SerializationContexts;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Helion.World.Save;

public class SaveGame
{
    public const string QuickPrefix = "quicksave";
    public const string AutoPrefix = "autosave";
    public const string DefaultPrefix = "savegame";

    private const string SaveDataFile = "save.json";
    private const string WorldDataFile = "world.json";
    private const string ImageFile = "image.png";

    public readonly SaveGameModel? Model;

    public readonly string SaveDir;

    public readonly string FileName;

    public string FilePath => Path.Combine(SaveDir, FileName);

    public readonly SaveGameType Type;

    public SaveGame(string saveDir, string filename, SaveGameModel model)
    {
        SaveDir = saveDir;
        FileName = filename;
        Model = model;
        Type = GetFileType(filename);
    }

    public SaveGame(string saveDir, string filename)
    {
        SaveDir = saveDir;
        FileName = filename;
        Type = GetFileType(filename);

        try
        {
            using ZipArchive zipArchive = ZipFile.Open(FilePath, ZipArchiveMode.Read);
            ZipArchiveEntry? saveDataEntry = zipArchive.Entries.FirstOrDefault(x => x.Name.Equals(SaveDataFile, StringComparison.Ordinal));
            if (saveDataEntry == null)
                return;

            Model = (SaveGameModel?)JsonSerializer.Deserialize(saveDataEntry.ReadDataAsString(), typeof(SaveGameModel), SaveGameModelSerializationContext.Default);
        }
        catch
        {
            // Corrupt zip or bad serialize
        }
    }

    public Image? GetSaveGameImage()
    {
        try
        {
            using ZipArchive zipArchive = ZipFile.Open(FilePath, ZipArchiveMode.Read);
            ZipArchiveEntry? imageFileEntry = zipArchive.Entries.FirstOrDefault(x => x.Name.Equals(ImageFile, StringComparison.Ordinal));

            if (imageFileEntry == null)
            {
                return null;
            }

            using Stream dataStream = imageFileEntry.Open();
            using SixLabors.ImageSharp.Image<Rgba32> pngImage = SixLabors.ImageSharp.Image.Load<Rgba32>(dataStream);
            return Image.FromImageSharp(pngImage);
        }
        catch
        {
            // Bad ZIP file?
        }

        return null;
    }

    private static SaveGameType GetFileType(string fileName)
    {
        var file = Path.GetFileName(fileName);
        if (file.StartsWith(AutoPrefix, StringComparison.Ordinal))
            return SaveGameType.Auto;
        else if (file.StartsWith(QuickPrefix, StringComparison.Ordinal))
            return SaveGameType.Quick;
        return SaveGameType.Default;
    }

    public WorldModel? ReadWorldModel()
    {
        if (Model == null)
            return null;

        try
        {
            using ZipArchive zipArchive = ZipFile.Open(FilePath, ZipArchiveMode.Read);
            ZipArchiveEntry? entry = zipArchive.Entries.FirstOrDefault(x => x.Name.Equals(Model.WorldFile, StringComparison.Ordinal));
            if (entry == null)
                return null;

            return (WorldModel?)JsonSerializer.Deserialize(entry.ReadDataAsString(), typeof(WorldModel), WorldModelSerializationContext.Default);
        }
        catch
        {
            return null;
        }
    }

    public static SaveGameEvent WriteSaveGame(IWorld world, WorldModel worldModel,
        string title, string saveDir, string filename, IScreenshotGenerator screenshotGenerator, Image? image)
    {
        SaveGameModel saveGameModel = new()
        {
            Text = title,
            MapName = world.MapInfo.GetMapNameWithPrefix(world.ArchiveCollection.Language),
            Date = DateTime.Now,
            WorldFile = WorldDataFile,
            ImageFile = image == null ? "" : ImageFile,
            Files = worldModel.Files,

            SaveGameStats = new SaveGameStats()
            {
                KillCount = worldModel.KillCount,
                TotalMonsters = worldModel.TotalMonsters,
                SecretCount = worldModel.SecretCount,
                TotalSecrets = worldModel.TotalSecrets,
                LevelTime = worldModel.LevelTime,
            }
        };

        string saveTempFile = TempFileManager.GetFile();

        try
        {
            File.Delete(saveTempFile);
            using (ZipArchive zipArchive = ZipFile.Open(saveTempFile, ZipArchiveMode.Create))
            {
                ZipArchiveEntry entry = zipArchive.CreateEntry(SaveDataFile);
                using (Stream stream = entry.Open())
                    stream.Write(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(saveGameModel, typeof(SaveGameModel), SaveGameModelSerializationContext.Default)));

                entry = zipArchive.CreateEntry(WorldDataFile);
                using (Stream stream = entry.Open())
                    stream.Write(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(worldModel, typeof(WorldModel), WorldModelSerializationContext.Default)));

                if (image != null)
                {
                    entry = zipArchive.CreateEntry(ImageFile);
                    using var stream = entry.Open();
                    screenshotGenerator.GeneratePngImage(image, stream);
                }
            }

            SaveGame saveGame = new(saveDir, filename, saveGameModel);

            if (File.Exists(saveGame.FilePath))
                File.Delete(saveGame.FilePath);

            File.Copy(saveTempFile, saveGame.FilePath);

            return new SaveGameEvent(saveGame, worldModel, filename, true);
        }
        catch (Exception ex)
        {
            return new SaveGameEvent(new SaveGame(saveDir, filename, saveGameModel), worldModel, filename, false, ex);
        }


    }
}
