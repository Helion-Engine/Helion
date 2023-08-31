using Helion.Models;
using Helion.Resources.Archives;
using Helion.Resources.Archives.Collection;
using NLog;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Helion.World.Util;

public static class ModelVerification
{
    public static bool VerifyModelFiles(GameFilesModel filesModel, ArchiveCollection archiveCollection, Logger? log)
    {
        if (!VerifyFileModel(archiveCollection, filesModel.IWad, log))
            return false;

        var fileArchives = archiveCollection.Archives.Where(x => x.ExtractedFrom == null).ToList();
        if (fileArchives.Count != filesModel.Files.Count)
        {
            if (log == null)
                return false;

            LogExtraLoadedArchives(filesModel, log, fileArchives);
            LogMissingFiles(filesModel, log, fileArchives);
            return false;
        }

        if (filesModel.Files.Any(x => !VerifyFileModel(archiveCollection, x, log)))
            return false;

        return true;
    }

    private static void LogExtraLoadedArchives(GameFilesModel filesModel, ILogger log, IList<Archive> fileArchives)
    {
        foreach (var archive in fileArchives)
        {
            if (filesModel.Files.Any(x => archive.MD5.Equals(x.MD5)))
                continue;
            log.Error($"Loaded '{Path.GetFileName(archive.OriginalFilePath)}' that is not part of this save.");
        }
    }

    private static void LogMissingFiles(GameFilesModel filesModel, ILogger log, IList<Archive> fileArchives)
    {
        foreach (var file in filesModel.Files)
        {
            if (fileArchives.Any(x => x.MD5.Equals(file.MD5)))
                continue;
            log.Error($"Required archive '{file.FileName}' for this save is not loaded.");
        }
    }

    private static bool VerifyFileModel(ArchiveCollection archiveCollection, FileModel fileModel, ILogger? log)
    {
        if (fileModel.FileName == null)
        {
            log?.Warn("File in save game was null.");
            return true;
        }

        var archive = archiveCollection.GetArchiveByFileName(fileModel.FileName);
        if (archive == null)
        {
            log?.Error($"Required archive '{fileModel.FileName}' for this save is not loaded.");
            return false;
        }

        if (fileModel.MD5 == null)
        {
            log?.Warn("MD5 for file in save game was null.");
            return true;
        }

        if (!fileModel.MD5.Equals(archive.MD5))
        {
            log?.Error($"Required archive {fileModel.FileName} did not match MD5 for save game.");
            log?.Error($"Save MD5: {fileModel.MD5} - Loaded MD5: {archive.MD5}");
            return false;
        }

        return true;
    }
}
