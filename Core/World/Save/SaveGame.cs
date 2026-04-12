using Helion.Graphics;
using Helion.Models;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.Util.SerializationContexts;
using Helion.Util.Streams;
using Helion.World.Util;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

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

    /// <summary>
    /// Whether the save is compatible with the currently loaded WADs.
    /// </summary>
    public SaveVerificationResult VerificationResult;

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

            using var stream = saveDataEntry.Open();
            Model = (SaveGameModel?)JsonSerializer.Deserialize(stream, typeof(SaveGameModel), SaveGameModelSerializationContext.Default);
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

            using var stream = entry.Open();
            var model = (WorldModel?)JsonSerializer.Deserialize(stream, typeof(WorldModel), WorldModelSerializationContext.Default);
            if (model != null)
                ApplyVersionFix(model);
            return model;
        }
        catch
        {
            return null;
        }
    }

    private void ApplyVersionFix(WorldModel worldModel)
    {
        // Checks for versions < 0.9.8.0 as it was the first version to serialize solonet
        // This change removed a frame from the table and requires shifting the index down after that point:
        // https://github.com/Helion-Engine/Helion/commit/5217c33c74690fde574be70da6953c0db57c5ad2
        if (Model != null && Model.AppVersion.Length == 0 && !worldModel.ConfigValues.Any(x => x.Key.EqualsIgnoreCase("game.solonet")))
        {
            const int StartIndex = 731;
            for (int i = 0; i < worldModel.Entities.Count; i++)
            {
                var entity = worldModel.Entities[i];
                if (entity.Frame.FrameIndex > StartIndex)
                    entity.Frame.FrameIndex--;
            }

            for (int i = 0; i < worldModel.Players.Count; i++)
            {
                var player = worldModel.Players[i];
                if (player.Frame.FrameIndex > StartIndex)
                    player.Frame.FrameIndex--;

                if (player.AnimationWeaponFrame.HasValue && player.AnimationWeaponFrame.Value.FrameIndex > StartIndex)
                {
                    var frame = player.AnimationWeaponFrame.Value;
                    frame.FrameIndex--;
                    player.AnimationWeaponFrame = frame;
                }

                if (player.WeaponFlashFrame.HasValue && player.WeaponFlashFrame.Value.FrameIndex > StartIndex)
                {
                    var frame = player.WeaponFlashFrame.Value;
                    frame.FrameIndex--;
                    player.WeaponFlashFrame = frame;
                }
            }
        }
    }

    public static void AllocateJsonMapping()
    {
        JsonSerializer.Serialize(new SaveGameModel(), SaveGameModelSerializationContext.Default.SaveGameModel);
        JsonSerializer.Serialize(new WorldModel(), WorldModelSerializationContext.Default.WorldModel);
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
            AppVersion = AppVersion.Current.VersionString,

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
            using (var zipArchive = ZipFile.Open(saveTempFile, ZipArchiveMode.Create))
            {
                WriteZipEntry(zipArchive, SaveDataFile, saveGameModel, SaveGameModelSerializationContext.Default.SaveGameModel);
                WriteZipEntry(zipArchive, WorldDataFile, worldModel, WorldModelSerializationContext.Default.WorldModel);

                if (image != null)
                {
                    var entry = zipArchive.CreateEntry(ImageFile);
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

    private static void WriteZipEntry<T>(ZipArchive zipArchive, string entryName, T value, JsonTypeInfo<T> typeInfo)
    {
        var entry = zipArchive.CreateEntry(entryName);
        using var zipStream = entry.Open();
        using var bufferStream = new PoolBufferedStream(zipStream);
        JsonSerializer.Serialize(bufferStream, value, typeInfo);
    }
}
